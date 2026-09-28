using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Gameplay.Player;
using Spirit604.DotsCity.NavMesh;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Car.Sound;
using Spirit604.DotsCity.Simulation.Car.Sound.Authoring;
using Spirit604.DotsCity.Simulation.Common;
using Spirit604.DotsCity.Simulation.Sound;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.DotsCity.Simulation.Traffic.Authoring;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.TestScene
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(PostBakingSystemGroup), OrderLast = true)]
    public partial class VehicleComponentCleanerBakingSystem : SystemBase
    {
        private EntityQuery bakingCarQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            bakingCarQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<TrafficComponentCleanerTag>()
                .WithOptions(EntityQueryOptions.IncludePrefab)
                .Build(this);

            RequireForUpdate(bakingCarQuery);
        }

        protected override void OnUpdate()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            Entities
            .WithoutBurst()
            .WithEntityQueryOptions(EntityQueryOptions.IncludePrefab)
            .ForEach((
                Entity entity,
                in TrafficComponentCleanerTag trafficComponentCleanerData) =>
            {
                var entityManager = EntityManager;
                Clean(entity, ref entityManager, ref commandBuffer, trafficComponentCleanerData.CleanSound);

                if (trafficComponentCleanerData.AddPoolable)
                {
                    PoolEntityUtils.AddPoolComponents(ref commandBuffer, entity, EntityWorldType.PureEntity);
                }

            }).Run();

            CompleteDependency();
            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }

        public static void Clean(Entity entity, ref EntityManager entityManager, ref EntityCommandBuffer commandBuffer, bool cleanSound)
        {
            entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(TrafficTag));
            entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(TrafficDefaultTag));
            entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(TrafficTargetDirectionComponent));
            entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(TrafficSettingsComponent));
            entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(TrafficPathComponent));
            entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(TrafficStateComponent));
            entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(TrafficLightDataComponent));
            entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(TrafficObstacleComponent));
            entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(TrafficMovementComponent));
            entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(TrafficDestinationComponent));
            entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(TrafficApproachDataComponent));
            entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(TrafficRotationSpeedComponent));
            entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(TrafficTypeComponent));

            entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(TrafficSwitchTargetNodeRequestTag));
            entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(TrafficNextTrafficNodeRequestTag));
            entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(TrafficEnteredTriggerNodeTag));
            entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(TrafficEnteringTriggerNodeTag));

            entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(CarTag));
            entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(ObstacleTag));
            entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(CarStartExplodeComponent));
            entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(CarCollisionComponent));
            entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(FactionTypeComponent));
            entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(CarTypeComponent));

            entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(TrafficEntityBakingTag));

            entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(CullStateComponent));
            entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(InPermittedRangeTag));
            entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(CulledEventTag));

            entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(CarRelatedHullComponent));
            entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(BoundsComponent));
            entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(CarIgnitionData));

            entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(HealthComponent));
            entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(EngineDamageData));
            entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(PlayerTargetComponent));
            entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(WorldEntitySharedType));

            if (cleanSound)
            {
                entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(CarModelComponent));
                entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(InViewOfCameraTag));

                entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(HasSoundTag));
                entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(CarSoundEntityBakingTag));

                entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(CarSoundData));
                entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(CarUpdateSound));
                entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(CarHornComponent));
            }

            entityManager.TryToRemoveComponent(ref commandBuffer, entity, typeof(NavMeshObstacleData));
        }
    }
}