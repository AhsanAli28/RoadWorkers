using Spirit604.DotsCity.Gameplay.Player.Session;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.Extensions;
using Spirit604.Gameplay.Car;
using Spirit604.Gameplay.Config.Player;
using Spirit604.Gameplay.Factory.Npc;
using Spirit604.Gameplay.Npc;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player.Spawn
{
    public class DefaultPlayerCitySpawner : PlayerSpawnerBase
    {
        private EntityManager entityManager;

        private readonly PlayerSession playerSession;
        private readonly PlayerCarSpawner playerCarSpawner;
        private readonly IPlayerEntityTriggerProccesor playerEntityTriggerProccesor;
        private readonly IPlayerNpcFactory playerNpcFactory;
        private readonly IPlayerMobNpcFactory playerMobNpcFactory;

        public DefaultPlayerCitySpawner(
            PlayerSession playerSession,
            PlayerCarSpawner playerCarSpawner,
            IPlayerEntityTriggerProccesor playerEntityTriggerProccesor,
            IPlayerNpcFactory playerNpcFactory,
            IPlayerMobNpcFactory playerMobNpcFactory)
        {
            this.playerSession = playerSession;
            this.playerCarSpawner = playerCarSpawner;
            this.playerEntityTriggerProccesor = playerEntityTriggerProccesor;
            this.playerNpcFactory = playerNpcFactory;
            this.playerMobNpcFactory = playerMobNpcFactory;
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        public override void Initialize()
        {
            playerCarSpawner.Initialize();
        }

        public override GameObject Spawn(PlayerSpawnDataConfig playerSpawnDataConfig, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            GameObject player = null;
            playerEntityTriggerProccesor.TriggerIsBlocked = true;
            var playerInteractCarStateEntity = entityManager.CreateEntityQuery(typeof(PlayerInteractCarStateComponent)).GetSingletonEntity();

            var currentSessionData = playerSession.CurrentSessionData;

            if (playerSpawnDataConfig.CurrentSpawnPlayerType == PlayerSpawnDataConfig.SpawnPlayerType.Npc)
            {
                for (int i = 0; i < currentSessionData.TotalCharaterData.Count; i++)
                {
                    bool isAi = i > 0;

                    var playerData = currentSessionData.TotalCharaterData[i];

                    var currentSpawnPosition = currentSessionData.SpawnPosition;

                    if (currentSpawnPosition == Vector3.zero)
                    {
                        currentSpawnPosition = spawnPosition;
                    }

                    Vector3 randomPosition = currentSpawnPosition + (UnityEngine.Random.insideUnitCircle * 2f).ToVector3_3DSpace();

                    var npc = GetNpc(playerData.NpcId, isAi, randomPosition, Quaternion.identity);

                    if (npc.TryGetComponent<NpcBehaviourBase>(out var behaviour))
                    {
                        int index = i != 0 ? i : -1;
                        behaviour.Initialize(randomPosition, index);
                        playerSession.LinkNpc(behaviour, playerData);
                    }

                    if (i == 0)
                    {
                        player = npc;
                    }
                }

                entityManager.SetComponentData(playerInteractCarStateEntity, new PlayerInteractCarStateComponent()
                {
                    PlayerInteractCarState = PlayerInteractCarState.OutOfCar
                });
            }

            var spawnOnlyCar = playerSpawnDataConfig.CurrentSpawnPlayerType == PlayerSpawnDataConfig.SpawnPlayerType.Car;

            var carData = currentSessionData.CarData;

            bool spawnCar = carData != null && carData.HasData;

            if (spawnCar)
            {
                if (spawnOnlyCar)
                {
                    player = playerCarSpawner.Spawn(carData.CarModel, spawnPosition, spawnRotation, carData.CurrentHealth, true);

                    if (!player)
                        return null;

                    var carSlots = player.GetComponent<ICarSlots>();

                    for (int i = 0; i < currentSessionData.TotalCharaterData.Count; i++)
                    {
                        var playerData = currentSessionData.TotalCharaterData[i];

                        if (carSlots != null)
                        {
                            bool isAi = i > 0;
                            var npcSlot = AddToSlots(carSlots, playerData.NpcId, isAi);

                            if (npcSlot.NpcInCarActor.TryGetComponent<NpcBehaviourBase>(out var behaviour))
                            {
                                int index = i != 0 ? i : -1;
                                behaviour.Initialize(default, index);
                                playerSession.LinkNpc(behaviour, playerData);
                            }
                        }
                    }

                    entityManager.SetComponentData(playerInteractCarStateEntity, new PlayerInteractCarStateComponent()
                    {
                        PlayerInteractCarState = PlayerInteractCarState.InCar
                    });
                }
                else
                {
                    var carEntity = GetSavedCar(CarType.Traffic);
                    playerSession.LastCar = carEntity;
                }
            }

            return player;
        }

        public Entity GetSavedCar(CarType carType)
        {
            var carEntity = Entity.Null;

            var carData = playerSession.CurrentSessionData.CarData;

            if (carData != null && carData.HasData)
            {
                switch (carType)
                {
                    case CarType.Player:
                        {
                            var playerCar = playerCarSpawner.Spawn(carData.CarModel, carData.Position, carData.Rotation, carData.CurrentHealth);
                            carEntity = playerCar.GetComponent<IVehicleEntityRef>().RelatedEntity;
                            break;
                        }
                    case CarType.Traffic:
                        {
                            var trafficSpawnerSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<TrafficSpawnerSystem>();

                            trafficSpawnerSystem.Spawn((int)carData.CarModel, carData.Position, carData.Rotation, Vector3.zero, carData.CurrentHealth);
                            break;
                        }
                }
            }

            return carEntity;
        }

        protected GameObject GetNpc(string id, bool isAi, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            if (!isAi)
            {
                return playerNpcFactory.Get(id, spawnPosition, spawnRotation);
            }
            else
            {
                return playerMobNpcFactory.Get(id, spawnPosition, spawnRotation);
            }
        }

        protected GameObject GetNpc(string id, bool isAi)
        {
            return GetNpc(id, isAi, default, Quaternion.identity);
        }

        protected virtual CarSlot AddToSlots(ICarSlots carSlots, string id, bool isAi)
        {
            return carSlots.EnterCar(id, driver: !isAi);
        }
    }
}