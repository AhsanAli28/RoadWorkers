using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Gameplay.Player.Spawn;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Car
{
    public class CarConvertService : ICarConverter
    {
        private readonly EntityManager entityManager;
        private readonly PlayerCarSpawner playerCarSpawner;
        private readonly TrafficSpawnerSystem trafficSpawnerSystem;

        public CarConvertService(PlayerCarSpawner playerCarSpawner)
        {
            this.entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            this.playerCarSpawner = playerCarSpawner;
            this.trafficSpawnerSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<TrafficSpawnerSystem>();
        }

        public Entity Convert(ref EntityCommandBuffer commandBuffer, Entity oldEntity, CarType newType)
        {
            var oldCarType = entityManager.GetComponentData<CarTypeComponent>(oldEntity).CarType;

            if (oldCarType == newType)
            {
                return oldEntity;
            }

            var carModelComponent = entityManager.GetComponentData<CarModelComponent>(oldEntity);
            var carModel = carModelComponent.Value;

            int health = 0;

            if (entityManager.HasComponent<HealthComponent>(oldEntity))
            {
                HealthComponent healthComponent = entityManager.GetComponentData<HealthComponent>(oldEntity);
                health = healthComponent.Value;
            }

            Vector3 velocity = entityManager.GetComponentData<VelocityComponent>(oldEntity).Value;

            var transform = entityManager.GetComponentData<LocalToWorld>(oldEntity);

            Entity carEntity = Entity.Null;

            switch (newType)
            {
                case CarType.Player:
                    {
                        var spawnOffset = new Vector3(0, 0f, 0);
                        var spawnPosition = (Vector3)transform.Position + spawnOffset;
                        var playerCar = playerCarSpawner.Spawn(carModel, spawnPosition, transform.Rotation, health, velocity, Vector3.zero);

                        if (playerCar)
                        {
                            carEntity = playerCar.GetComponent<IVehicleEntityRef>().RelatedEntity;

                            if (entityManager.HasComponent<PhysicsCollider>(carEntity))
                            {
                                // Immediately disable physics for 1 frame to avoid collision with existing traffic car
                                entityManager.SetSharedComponent(carEntity, new PhysicsWorldIndex()
                                {
                                    Value = ProjectConstants.NoPhysicsWorldIndex
                                });

                                commandBuffer.SetSharedComponent(carEntity, new PhysicsWorldIndex()
                                {
                                    Value = 0
                                });
                            }
                        }

                        break;
                    }
                case CarType.Traffic:
                    {
                        var shouldStopEngine = entityManager.HasComponent<CarEngineStartedTag>(oldEntity);
                        trafficSpawnerSystem.Spawn((int)carModel, transform.Position, transform.Rotation, velocity, health, false, shouldStopEngine);
                        break;
                    }
            }

            return carEntity;
        }
    }
}