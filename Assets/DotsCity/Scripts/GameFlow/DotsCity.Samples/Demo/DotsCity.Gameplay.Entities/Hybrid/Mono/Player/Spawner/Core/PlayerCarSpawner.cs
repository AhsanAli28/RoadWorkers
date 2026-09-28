using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Gameplay.Factory.Player;
using Spirit604.DotsCity.Gameplay.Player.Authoring;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.Gameplay.Car;
using Spirit604.Gameplay.Factory;
using Spirit604.Gameplay.Npc;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.GraphicsIntegration;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player.Spawn
{
    public class PlayerCarSpawner : MonoBehaviour
    {
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private VehicleDataHolder vehicleDataHolder;
        [SerializeField] private bool customPlayerPrefab;
        [SerializeField] private bool log = false;

        private EntityManager entityManager;
        private Dictionary<int, Entity> prefabEntities = new Dictionary<int, Entity>();
        private GeneralCoreSettingsDataReference generalConfig;
        private bool isInitialized;

        private PlayerCarPool playerCarPool;
        private IShootTargetProvider targetProvider;
        private INpcInteractCarService npcInteractCarService;

        [InjectWrapper]
        public void Construct(
            PlayerCarPool playerCarPool,
            IShootTargetProvider targetProvider,
            INpcInteractCarService npcInteractCarService)
        {
            this.playerCarPool = playerCarPool;
            this.targetProvider = targetProvider;
            this.npcInteractCarService = npcInteractCarService;
        }

        private void Start()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        public void Initialize()
        {
            InitializeInternal();
        }

        [Button]
        public void Spawn()
        {
            Spawn(0, spawnPoint.transform.position, Quaternion.identity);
        }

        public GameObject Spawn(int carModel, bool hasDriver = false)
        {
            return Spawn(carModel, out var vehicleEntity, hasDriver);
        }

        public GameObject Spawn(int carModel, out Entity vehicleEntity, bool hasDriver = false)
        {
            InitializeInternal();

            Log($"PlayerCarSpawner. Trying to spawn [{carModel}] '{GetVehicleName(carModel)}'");

            vehicleEntity = Entity.Null;

            if (playerCarPool == null)
            {
                UnityEngine.Debug.Log($"PlayerCarSpawner. PlayerCarPool is null.");
                return null;
            }

            var car = playerCarPool.GetCarGo(carModel);

            if (!car)
            {
                UnityEngine.Debug.Log($"PlayerCarSpawner. PlayerCarPool car model '{GetVehicleName(carModel)}' hull prefab not found!");
                return null;
            }

            Initialize(car, carModel, ref vehicleEntity, hasDriver);

            return car;
        }

        public GameObject Initialize(GameObject car, int carModel, ref Entity vehicleEntity, bool hasDriver = true)
        {
            var vehicleEntityRef = car.GetComponent<IVehicleEntityRef>();

            if (vehicleEntityRef == null)
            {
                UnityEngine.Debug.LogError($"PlayerCarSpawner. CarModel '{GetVehicleName(carModel)}' Each player car skin must have a component that implements the 'IVehicleEntityRef' interface, or add a 'CarReferences' component if you are using prefab entities from the built-in preset, or enable 'Custom Player Prefab' option in the inspector of PlayerCarSpawner & add hybrid runtime components to hull your prefab, for more info read the https://dotstrafficcity.readthedocs.io/en/latest/playerCustom.html#player-car.");
            }

            if (!customPlayerPrefab)
            {
                vehicleEntity = SpawnEntity(carModel, hasDriver);

                if (vehicleEntity == Entity.Null)
                {
                    return null;
                }

                if (vehicleEntityRef != null)
                {
                    vehicleEntityRef.Initialize(vehicleEntity);
                }

                if (car.TryGetComponent<PlayerNpcCarBehaviour>(out var playerNpcCarBehaviour))
                {
                    playerNpcCarBehaviour.Initialize(targetProvider);
                }

                if (car.TryGetComponent<ICarSlots>(out var carSlots))
                {
                    carSlots.Initialize(npcInteractCarService);
                }

                entityManager.AddComponentObject(vehicleEntity, car.transform);
            }

            if (vehicleEntityRef != null)
            {
                vehicleEntity = vehicleEntityRef.RelatedEntity;
            }

            return car;
        }

        public Entity SpawnEntity(int carModel, bool addDriver = true)
        {
            Entity entityPrefab;

            if (!prefabEntities.TryGetValue(carModel, out entityPrefab))
            {
                UnityEngine.Debug.Log($"PlayerCarSpawner. PlayerCar '{GetVehicleName(carModel)}' entity prefab not found! Make sure you have added your car to the player preset, otherwise try reopen the subscene.");
                return default;
            }

            var vehicleEntity = entityManager.Instantiate(entityPrefab);

            if (addDriver)
            {
                var set = new ComponentTypeSet(
                     ComponentType.ReadOnly<HasDriverTag>(),
                     ComponentType.ReadOnly<CarEngineStartedTag>());

                entityManager.AddComponent(vehicleEntity, set);
            }

            return vehicleEntity;
        }

        public GameObject Spawn(int carModel, Vector3 spawnPosition, Quaternion spawnRotation, bool hasDriver = false)
        {
            var car = Spawn(carModel, out var vehicleEntity, hasDriver);

            if (car != null)
            {
                entityManager.SetComponentData(vehicleEntity, LocalTransform.FromPositionRotation(spawnPosition, spawnRotation));

                if (entityManager.HasComponent<PhysicsGraphicalInterpolationBuffer>(vehicleEntity))
                {
                    entityManager.SetComponentData(vehicleEntity, new PhysicsGraphicalInterpolationBuffer()
                    {
                        PreviousTransform = new RigidTransform(spawnRotation, spawnPosition)
                    });
                }

                if (generalConfig.Config.Value.WorldSimulationType == WorldSimulationType.HybridMono)
                {
                    car.transform.position = spawnPosition;
                    car.transform.rotation = spawnRotation;

                    if (car.TryGetComponent<Rigidbody>(out var rb))
                    {
                        entityManager.AddComponentObject(vehicleEntity, rb);
                        rb.Move(spawnPosition, spawnRotation);
                    }
                    else
                    {
                        UnityEngine.Debug.Log($"PlayerCarSpawner. CarModel '{GetVehicleName(carModel)}' Rigidbody not found. Make sure you have created a player vehicle that is compatible with Hybrid Mono mode.");
                    }
                }

                return car;
            }

            return null;
        }

        public GameObject Spawn(int carModel, Vector3 spawnPosition, Quaternion spawnRotation, int health, bool hasDriver = false)
        {
            return Spawn(carModel, spawnPosition, spawnRotation, health, Vector3.zero, Vector3.zero, hasDriver);
        }

        public GameObject Spawn(int carModel, Vector3 spawnPosition, Quaternion spawnRotation, int health, Vector3 linearVelocity, Vector3 angularVelocity, bool hasDriver = false)
        {
            var car = Spawn(carModel, spawnPosition, spawnRotation, hasDriver);

            if (car == null)
                return null;

            var entity = car.GetComponent<IVehicleEntityRef>().RelatedEntity;

            if (health > 0 && entityManager.HasComponent<HealthComponent>(entity))
            {
                entityManager.SetComponentData(entity, new HealthComponent(health));
            }

            if (generalConfig.Config.Value.DOTSSimulation)
            {
                entityManager.SetComponentData(entity, new PhysicsVelocity() { Linear = linearVelocity, Angular = angularVelocity });
            }
            else
            {
                if (linearVelocity != Vector3.zero)
                {
                    if (car.TryGetComponent<Rigidbody>(out var rb))
                    {
#if UNITY_6000_0_OR_NEWER
                        rb.linearVelocity = linearVelocity;

#else
                        rb.velocity = linearVelocity;
#endif
                    }
                }
            }

            return car;
        }

        private string GetVehicleName(int carModel) => vehicleDataHolder.VehicleDataCollection.GetName(carModel);

        [Button]
        private void InspectVehiclePool()
        {
            UnityEngine.Debug.Log("Pool contains:");

            foreach (var item in prefabEntities)
            {
                UnityEngine.Debug.Log($"Car '{GetVehicleName(item.Key)}' Model '{item.Key}'");
            }
        }

        private void Log(string text)
        {
            if (!log) return;
            UnityEngine.Debug.Log(text);
        }

        private void InitializeInternal()
        {
            if (isInitialized)
                return;

            Log("PlayerCarSpawner. Starting init");
            isInitialized = true;
            var prefabQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<PlayerCarPrefabContainer>());
            generalConfig = entityManager.CreateEntityQuery(ComponentType.ReadOnly<GeneralCoreSettingsDataReference>()).GetSingleton<GeneralCoreSettingsDataReference>();
            var prefabs = prefabQuery.ToComponentDataArray<PlayerCarPrefabContainer>(Unity.Collections.Allocator.TempJob);

            for (int i = 0; i < prefabs.Length; i++)
            {
                var prefabEntity = prefabs[i].PrefabEntity;

                if (prefabs[i].CarModel == -1)
                    continue;

                Log($"PlayerCarSpawner. CarModel [{prefabs[i].CarModel}] '{GetVehicleName(prefabs[i].CarModel)}' entity added to pool");
                prefabEntities.Add(prefabs[i].CarModel, prefabEntity);
            }

            prefabs.Dispose();
        }
    }
}