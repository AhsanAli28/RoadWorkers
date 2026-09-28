using Spirit604.Attributes;
using Spirit604.DotsCity.Gameplay.Config.Common;
using Spirit604.DotsCity.Gameplay.Factory.Player;
using Spirit604.DotsCity.Gameplay.Initialization;
using Spirit604.DotsCity.Gameplay.Npc.Factory.Player;
using Spirit604.DotsCity.Gameplay.Player.Session;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Factory.Car;
using Spirit604.Gameplay.Config.Player;
using Spirit604.Gameplay.Factory;
using Spirit604.Gameplay.Factory.Npc;
using Spirit604.Gameplay.Player;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player.Spawn
{
    public class PlayerCitySpawnController : MonoBehaviour, IPlayerSpawnerService
    {
        [SerializeField] private CitySettingsInitializer citySettingsInitializer;

        [SerializeField] private PlayerActorTracker playerTargetHandler;

        [SerializeField] private PlayerSpawnDataConfig playerSpawnDataConfig;

        [SerializeField] private PlayerCarPool playerCarPool;

        [SerializeField] private PlayerNpcFactory playerNpcFactory;

        [SerializeField] private PlayerMonoNpcFactory playerMonoNpcFactory;

        [SerializeField] private PlayerHybridMonoNpcFactory playerHybridMonoNpcFactory;

        [SerializeField] private FreeFlyCameraFactory freeFlyCameraFactory;

        [SerializeField] private VehicleDataHolder vehicleDataHolder;

        [SerializeField] private PlayerSpawnTrafficControlService playerSpawnTrafficControlService;

        [SerializeField] private Transform spawnPoint;

        public SpawnPointData SpawnPointData { get; private set; }
        public PlayerSpawnDataConfig PlayerSpawnDataConfig => playerSpawnDataConfig;
        public PlayerCarPool PlayerCarPool => playerCarPool;
        public VehicleDataHolder VehicleDataHolder => vehicleDataHolder;
        public VehicleDataCollection VehicleDataCollection => vehicleDataHolder.VehicleDataCollection;
        public GeneralSettingData GeneralSettings => citySettingsInitializer.Settings;
        public PlayerSpawnTrafficControlService PlayerSpawnTrafficControlService => playerSpawnTrafficControlService;

        public event Action<GameObject> OnPlayerSpawned = delegate { };

        private IPlayerEntitySpawner spawner;
        private PlayerSession playerSession;
        private WeaponFactory weaponFactory;

        [InjectWrapper]
        public void Construct(
            IPlayerEntitySpawner spawner,
            PlayerSession playerSession,
            WeaponFactory weaponFactory)
        {
            this.spawner = spawner;
            this.playerSession = playerSession;
            this.weaponFactory = weaponFactory;
        }

        private void Awake()
        {
            if (spawnPoint != null)
            {
                SpawnPointData = new SpawnPointData()
                {
                    Position = spawnPoint.transform.position,
                    Rotation = spawnPoint.transform.rotation,
                };
            }
        }

        public void Initialize()
        {
            var type = GeneralSettings.CameraViewType;

            switch (type)
            {
                case PlayerAgentType.PlayerTrafficControl:
                    playerSpawnTrafficControlService.Initialize();
                    break;
                default:
                    spawner.Initialize();
                    break;
            }
        }

        public IEnumerator Spawn()
        {
            var type = GeneralSettings.CameraViewType;

            switch (type)
            {
                case PlayerAgentType.PlayerTrafficControl:
                    yield return playerSpawnTrafficControlService.Spawn();
                    break;
                default:
                    yield return DelayedSpawn(true);
                    break;
            }
        }

        public void Spawn(bool shouldDelay)
        {
            StartCoroutine(DelayedSpawn(shouldDelay));
        }

        public IEnumerator DelayedSpawn(bool shouldDelay)
        {
            IPlayerEntitySpawner spawner = GetSpawner();

            Vector3 spawnPosition = default;
            Quaternion spawnRotation = Quaternion.identity;

            if (SpawnPointData != null)
            {
                spawnPosition = SpawnPointData.Position;
                spawnRotation = SpawnPointData.Rotation;
            }

            GameObject target = null;

            if (spawner != null)
            {
                ValidateId();
                playerSession.InitSpawnConfig(playerSpawnDataConfig, weaponFactory);
                target = spawner.Spawn(playerSpawnDataConfig, spawnPosition, spawnRotation);
            }

            if (target)
            {
                playerTargetHandler.Actor = target;

                yield return new WaitForSeconds(0.2f);
                OnPlayerSpawned(target);
            }
        }

        public string[] GetNpcOptions()
        {
            switch (GeneralSettings.CurrentPlayerControllerType)
            {
                case GeneralSettingData.PlayerControllerType.BuiltIn:
                    {
                        List<string> options = new List<string>();

                        if (GeneralSettings.DOTSSimulation)
                        {
                            if (playerNpcFactory)
                            {
                                var prefabs = playerNpcFactory.Prefabs;

                                foreach (var item in prefabs.Keys)
                                {
                                    options.Add(item.ToString());
                                }
                            }
                        }
                        else
                        {
                            if (playerMonoNpcFactory)
                            {
                                var prefabs = playerMonoNpcFactory.Prefabs;

                                foreach (var item in prefabs.Keys)
                                {
                                    options.Add(item.ToString());
                                }
                            }
                        }

                        return options.ToArray();
                    }
                case GeneralSettingData.PlayerControllerType.BuiltInCustom:
                    return playerHybridMonoNpcFactory ? playerHybridMonoNpcFactory.Options.ToArray() : null;
            }

            return null;
        }

        public int[] GetVehicleIds()
        {
            return VehicleDataCollection.GetVehicleIds(playerCarPool.CarPrefabs);
        }

        public string[] GetVehicleNames()
        {
            return VehicleDataCollection.GetVehicleNames(playerCarPool.CarPrefabs);
        }

        private void ValidateId()
        {
#if UNITY_EDITOR

            if (playerSpawnDataConfig.CurrentSpawnPlayerType == PlayerSpawnDataConfig.SpawnPlayerType.Npc)
            {
                var npcIds = GetNpcOptions();

                if (npcIds?.Length > 0)
                {
                    if (!npcIds.Contains(playerSpawnDataConfig.SelectedNpcID))
                    {
                        var sourceNpcId = playerSpawnDataConfig.SelectedNpcID;
                        var newNpcId = npcIds[0];

                        UnityEngine.Debug.Log($"Player spawner. Selected Npc '{sourceNpcId}' ID not found. Player Npc ID changed to '{newNpcId}'. Make sure that you selected desired '{sourceNpcId}' ID in the player spawner & created/assigned NPC with '{sourceNpcId}' ID in the PlayerNpcFactory.");

                        playerSpawnDataConfig.SelectedNpcID = npcIds[0];
                    }
                }
                else
                {
                    if (GeneralSettings.BuiltInSolution)
                        UnityEngine.Debug.Log($"Player spawner. The player can't be spawned. Player NPC factory is empty.");
                }
            }

            if (playerSpawnDataConfig.CurrentSpawnPlayerType == PlayerSpawnDataConfig.SpawnPlayerType.Car)
            {
                var vehicleIds = GetVehicleIds();

                if (vehicleIds?.Length > 0)
                {
                    if (!vehicleIds.Contains(playerSpawnDataConfig.SelectedCarModel))
                    {
                        var sourceVehicleId = VehicleDataCollection.GetName(playerSpawnDataConfig.SelectedCarModel);
                        var newVehicleId = VehicleDataCollection.GetName(vehicleIds[0]);

                        UnityEngine.Debug.Log($"Player spawner. Selected Car '{sourceVehicleId}' ID not found. Player Car Model ID changed to '{newVehicleId}'. Make sure that you selected desired '{sourceVehicleId}' ID in the player spawner & created/assigned Car with '{sourceVehicleId}' ID in the PlayerCarPool & VehicleCollection.");

                        playerSpawnDataConfig.SelectedCarModel = vehicleIds[0];
                    }
                }
                else
                {
                    UnityEngine.Debug.Log($"Player spawner. The player's car cannot be spawned. PlayerCarPool is empty or VehicleCollection doesn't have the player's car ID.");
                }
            }
#endif
        }

        private IPlayerEntitySpawner GetSpawner()
        {
            return spawner;
        }

        public void OnInspectorEnabled()
        {
            ValidateId();
        }
    }
}