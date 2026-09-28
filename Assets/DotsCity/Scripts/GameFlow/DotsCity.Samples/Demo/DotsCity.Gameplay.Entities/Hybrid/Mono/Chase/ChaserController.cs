using Spirit604.Attributes;
using Spirit604.DotsCity.Gameplay.Config.Common;
using Spirit604.DotsCity.Gameplay.Factory;
using Spirit604.DotsCity.Gameplay.Player;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.Extensions;
using Spirit604.Gameplay;
using Spirit604.Gameplay.Car;
using Spirit604.Gameplay.Factory.Npc;
using Spirit604.Gameplay.Npc;
using Spirit604.Gameplay.Weapons;
using Spirit604.StateMachine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Chaser
{
    public class ChaserController : MonoBehaviourBase
    {
        #region Consts

        private const int POOL_SIZE = 5;
        private const int MAX_ATTEMP_COUNT = 5;
        private const float ATTEMP_TIME = 4f;

        private readonly Vector3 spawnOffset = new Vector3(0, 1f);

        #endregion

        #region Serialized variables

        [SerializeField] private VehicleDataHolder vehicleDataHolder;
        [SerializeField] private GameObject carNavMeshSurface;
        [SerializeField] private int chasingCarPrefabIndex;
        [SerializeField] private string defaultNpcID;
        [SerializeField][Range(0, 10)] private int maxCars = 3;
        [SerializeField][Range(1, 5)] private int initialNpcInCarCount = 3;
        [SerializeField]
        private List<WeaponType> availableWeapons = new List<WeaponType>() { WeaponType.Revolver, WeaponType.TommyGun };

        [SerializeField] private List<ChasingCarBehaviour> prefabs = new List<ChasingCarBehaviour>();

        #endregion

        #region Variables

        private Dictionary<int, ObjectPool> carPools = new Dictionary<int, ObjectPool>();
        private List<RigidTransform> spawnedTrafficSpawnPoints = new List<RigidTransform>();
        private List<int> IndexInUse = new List<int>();
        private PlayerScaryTriggerSystem playerScaryTriggerSystem;
        private bool isChasing;
        private float spawnDelay = 1f;
        private float unlockTimestamp;
        private float nextSpawnTime;
        private bool chasingStarted;
        private List<ChasingCarBehaviour> currentChasingCars = new List<ChasingCarBehaviour>();
        private IAIShotTargetProvider shootTargetProvider;
        private AINpcInteractCarService aiNpcExitCarService;
        private Coroutine chasingCoroutine;

        #endregion

        #region Constructor

        private PoliceMonoNpcFactory policeNpcFactory;
        private PoliceNpcInCarFactory policeNpcInCarFactory;
        private Camera mainCamera;

        [InjectWrapper]
        public void Construct(
            PoliceMonoNpcFactory policeNpcFactory,
            PoliceNpcInCarFactory policeNpcInCarFactory,
            GeneralSettingData generalSettingData)
        {
            this.policeNpcFactory = policeNpcFactory;
            this.policeNpcInCarFactory = policeNpcInCarFactory;

            aiNpcExitCarService = new AINpcInteractCarService(policeNpcInCarFactory, policeNpcFactory);
            shootTargetProvider = new ChaserCarEntityTargetProvider();

            if (generalSettingData.ChasingCarsSupport)
            {
                ReadyForTrigger = true;
            }

            mainCamera = Camera.main;
        }

        #endregion

        #region Properties

        public bool IsChasing
        {
            get
            {
                return isChasing;
            }

            set
            {
                if (!isChasing)
                {
                    if (value)
                    {
                        if (chasingCoroutine == null)
                        {
                            chasingCoroutine = StartCoroutine(SpawnCoroutine());
                        }
                    }

                    isChasing = value;
                }
            }
        }

        public bool ReadyForTrigger { get; set; }

        private bool ShouldSpawn => IsChasing && currentChasingCars.Count < maxCars && chasingStarted && UnityEngine.Time.time > nextSpawnTime && UnityEngine.Time.time > unlockTimestamp;

        #endregion

        #region Unity lifecycle

        private void Start()
        {
            FillPool();
            playerScaryTriggerSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PlayerScaryTriggerSystem>();
        }

        private void OnEnable()
        {
            if (playerScaryTriggerSystem != null)
                playerScaryTriggerSystem.OnStartShooting += PlayerScaryTriggerSystem_OnStartShooting;
        }

        private void OnDisable()
        {
            IsChasing = false;

            if (chasingCoroutine != null)
            {
                StopCoroutine(chasingCoroutine);
                chasingCoroutine = null;
            }

            if (playerScaryTriggerSystem != null)
                playerScaryTriggerSystem.OnStartShooting -= PlayerScaryTriggerSystem_OnStartShooting;
        }

        #endregion

        #region Public methods

        [Button]
        public void StartChase()
        {
            if (!IsChasing)
            {
                IsChasing = true;
            }
        }

        public void StopChasing()
        {
            IsChasing = false;
            chasingStarted = false;
        }

        public void StopChasingImmediate()
        {
            for (int i = 0; i < currentChasingCars.Count; i++)
            {
                currentChasingCars[i].gameObject.ReturnToPool();
            }

            currentChasingCars.Clear();
            StopChasing();
        }

        #endregion

        #region Private methods

        private void FillPool()
        {
            for (int i = 0; i < prefabs.Count; i++)
            {
                prefabs[i].gameObject.SetActive(false);
                ObjectPool pool = PoolManager.Instance.PoolForObject(prefabs[i].gameObject);
                pool.preInstantiateCount = POOL_SIZE;
                carPools.Add(i, pool);
            }
        }

        private IEnumerator SpawnCoroutine()
        {
            chasingStarted = true;

            if (carNavMeshSurface)
            {
                carNavMeshSurface.gameObject.SetActive(true);
            }

            yield return new WaitForSeconds(0.5f);

            while (true)
            {
                if (ShouldSpawn)
                {
                    Spawn(1);
                }

                if (!IsChasing)
                {
                    break;
                }

                yield return null;
            }
        }

        public void Spawn(int count)
        {
            spawnedTrafficSpawnPoints.Clear();
            nextSpawnTime = Time.time + spawnDelay;

            int spawnedCount = 0;
            int attemptCount = 0;

            while (spawnedCount < count)
            {
                var targetPosition = shootTargetProvider.GetTarget();
                RigidTransform rigidTransform = ChaserCarSpawnHelper.GetSpawnPoint(targetPosition);

                bool hasSpawnpoint = rigidTransform.pos.x != 0 && rigidTransform.pos.z != 0;

                if (hasSpawnpoint && !spawnedTrafficSpawnPoints.Contains(rigidTransform))
                {
                    SpawnCar(targetPosition, rigidTransform, spawnedCount);
                    spawnedCount++;
                }

                attemptCount++;

                if (attemptCount > MAX_ATTEMP_COUNT)
                {
                    unlockTimestamp = Time.time + ATTEMP_TIME;
                    break;
                }
            }
        }

        private void SpawnCar(Vector3 targetPosition, RigidTransform rigidTransform, int spawnedCount)
        {
            spawnedTrafficSpawnPoints.Add(rigidTransform);

            ChasingCarBehaviour chasingCar = GetCar(chasingCarPrefabIndex);

            AddCar(chasingCar);
            chasingCar.Initialize(shootTargetProvider, mainCamera, spawnedCount);

            Vector3 directionToTarget = (targetPosition.Flat() - (Vector3)rigidTransform.pos.Flat()).normalized;

            Vector3 spawnPointForward = (Quaternion)rigidTransform.rot * Vector3.forward;
            float dot = Vector3.Dot(spawnPointForward, directionToTarget);

            Vector3 direction = dot > 0 ? spawnPointForward : -spawnPointForward;

            Vector3 spawnPosition = (Vector3)rigidTransform.pos + spawnOffset;
            Quaternion spawnRotation = Quaternion.LookRotation(direction);

            int index = GetFreeIndex();

            chasingCar.ChaseIndex = index;

            IndexInUse.Add(index);

            InitCarSlots(chasingCar);

            chasingCar.OutOfTargetRange += ChasingCar_OutOfTargetRange;
            chasingCar.GetComponent<HealthBehaviourBase>().OnDeathEffectFinished += ChasingCar_OnDeath;
            chasingCar.SetPositionAndRotation(spawnPosition, spawnRotation);
            chasingCar.enabled = true;
        }

        private void InitCarSlots(ChasingCarBehaviour chasingCar)
        {
            var carSlots = chasingCar.GetComponent<ICarSlots>();
            carSlots.Initialize(aiNpcExitCarService);

            for (int i = 0; i < initialNpcInCarCount; i++)
            {
                if (carSlots.HasSlots)
                {
                    var npcInCarSlot = carSlots.EnterCar(defaultNpcID);

                    if (npcInCarSlot)
                    {
                        var randomWeapon = availableWeapons.GetRandomElement();
                        var npc = npcInCarSlot.NpcInCarActor.GetComponent<NpcInCar>();
                        npc.WeaponHolder.ReleaseWeaponOnDisable = false;
                        npc.WeaponHolder.ReturnWeapons();
                        npc.WeaponHolder.InitializeWeapon(randomWeapon);
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
        }

        private ChasingCarBehaviour GetCar(int carModel)
        {
            ObjectPool trafficPool = carPools[carModel];
            ChasingCarBehaviour car = trafficPool.Pop().GetComponent<ChasingCarBehaviour>();

            return car;
        }

        private void AddCar(ChasingCarBehaviour car)
        {
            if (!currentChasingCars.Contains(car))
            {
                currentChasingCars.Add(car);
                SwitchSideChaseState();
            }
        }

        private void RemoveCar(ChasingCarBehaviour car)
        {
            if (currentChasingCars.Contains(car))
            {
                if (IndexInUse.Contains(car.ChaseIndex))
                {
                    IndexInUse.Remove(car.ChaseIndex);
                }

                currentChasingCars.Remove(car);
                car.gameObject.ReturnToPool();

                SwitchSideChaseState();
            }
        }

        private int GetFreeIndex()
        {
            int i = 0;

            while (i < maxCars)
            {
                if (IndexInUse.Contains(i))
                {
                    i++;
                }
                else
                {
                    return i;
                }
            }

            return i;
        }

        private void SwitchSideChaseState()
        {
            bool canSideChase = currentChasingCars.Count > 1;

            for (int i = 0; i < currentChasingCars.Count; i++)
            {
                currentChasingCars[i].CanSideChasing = canSideChase;
            }
        }

        #endregion

        #region Event handlers

        private void PlayerScaryTriggerSystem_OnStartShooting()
        {
            if (ReadyForTrigger)
            {
                StartChase();
            }
        }

        private void ChasingCar_OnDeath(HealthBehaviourBase chasingCarObj)
        {
            chasingCarObj.OnDeathEffectFinished -= ChasingCar_OnDeath;
            var chasingCar = chasingCarObj.GetComponent<ChasingCarBehaviour>();
            RemoveCar(chasingCar);
        }

        private void ChasingCar_OutOfTargetRange(ChasingCarBehaviour chasingCar)
        {
            RemoveCar(chasingCar);
        }

        #endregion
    }
}