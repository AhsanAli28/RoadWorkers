using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;

namespace Spirit604.DotsCity.Simulation.Train.Authoring
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(BakingSystemGroup), OrderFirst = true)]
    public partial class TrafficWagonBakingSystem : SystemBase
    {
        private EntityQuery bakingCarQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            bakingCarQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<TrafficParentWagonBakingTag>()
                .WithOptions(EntityQueryOptions.IncludePrefab)
                .Build(this);

            RequireForUpdate(bakingCarQuery);
        }

        protected override void OnUpdate()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            var entityManager = EntityManager;
            var trafficCommonSettingsConfigBlobReference = SystemAPI.GetSingleton<TrafficCommonSettingsConfigBlobReference>();

            Entities
            .WithoutBurst()
            .WithEntityQueryOptions(EntityQueryOptions.IncludePrefab)
            .WithAll<TrafficParentWagonBakingTag>()
            .ForEach((
                Entity entity,
                DynamicBuffer<TrafficWagonElement> trafficWagonElements) =>
            {
                Entity nextEntity = entity;

                commandBuffer.SetComponentEnabled<TrafficCustomMovementTag>(entity, true);
                commandBuffer.AddComponent<HybridLinkedEntityTag>(entity);
                commandBuffer.AddComponent<IgnoreCullPhysicsTag>(entity);
                commandBuffer.AddComponent<TrainParentTag>(entity);
                commandBuffer.AddComponent<TrafficCustomLocomotion>(entity);

                for (int i = 0; i < trafficWagonElements.Length; i++)
                {
                    var wagonEntity = trafficWagonElements[i].Entity;

                    commandBuffer.AddComponent<IgnoreCullPhysicsTag>(wagonEntity);
                    commandBuffer.AddComponent<TrafficCustomLocomotion>(wagonEntity);
                    commandBuffer.AddComponent<TrafficCustomApproachTag>(wagonEntity);
                    commandBuffer.AddComponent<HasDriverTag>(wagonEntity);
                    commandBuffer.AddComponent<TrainTag>(wagonEntity);
                    commandBuffer.AddComponent<TrafficCustomTargetingTag>(wagonEntity);
                    commandBuffer.SetComponentEnabled<TrafficCustomMovementTag>(wagonEntity, true);

                    commandBuffer.AddComponent(wagonEntity, new TrainComponent()
                    {
                        IsParent = false,
                        NextEntity = nextEntity
                    });

                    if (entityManager.HasComponent<PoolableTag>(wagonEntity))
                    {
                        commandBuffer.RemoveComponent<PoolableTag>(wagonEntity);
                        commandBuffer.RemoveComponent<PooledEventTag>(wagonEntity);
                    }

                    if (entityManager.HasComponent<CullStateComponent>(wagonEntity))
                    {
                        commandBuffer.RemoveComponent<CulledEventTag>(wagonEntity);
                        commandBuffer.RemoveComponent<InViewOfCameraTag>(wagonEntity);
                        commandBuffer.RemoveComponent<InPermittedRangeTag>(wagonEntity);
                        commandBuffer.RemoveComponent<CullStateComponent>(wagonEntity);
                    }

                    if (entityManager.HasComponent<PhysicsVelocity>(wagonEntity))
                    {
                        commandBuffer.RemoveComponent<PhysicsVelocity>(wagonEntity);
                    }

                    if (entityManager.HasComponent<CarModelComponent>(wagonEntity))
                    {
                        commandBuffer.RemoveComponent<CarModelComponent>(wagonEntity);
                    }

                    if (entityManager.HasComponent<VehicleInputReader>(wagonEntity))
                    {
                        commandBuffer.RemoveComponent<VehicleInputReader>(wagonEntity);
                    }

                    commandBuffer.AddComponent(wagonEntity, new TrafficWagonComponent()
                    {
                        OwnerEntity = entity
                    });

                    nextEntity = wagonEntity;
                }

                if (trafficCommonSettingsConfigBlobReference.Reference.Value.EntityType != EntityType.HybridEntityMonoPhysics)
                {
                    commandBuffer.AddComponent<TrainWagonInitTag>(entity);
                }
                else
                {
                    commandBuffer.AddComponent<TrainWagonMonoInitTag>(entity);
                }

            }).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }
}