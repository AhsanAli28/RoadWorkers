using Spirit604.DotsCity.Events;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.DotsCity.Simulation.Traffic.Authoring;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Car.Authoring
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateAfter(typeof(CarModelBakingSystem))]
    [UpdateInGroup(typeof(BakingSystemGroup))]
    public partial class TrafficCarEntityEditorBakingSystem : SystemBase
    {
        private EntityQuery bakingCarQuery;
        private EntityQuery triggerNodeConfigQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            bakingCarQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<TrafficEntityEditorBakingTag>()
                .WithOptions(EntityQueryOptions.IncludePrefab)
                .Build(this);

            triggerNodeConfigQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<TriggerNodeConfigReference>()
                .WithOptions(EntityQueryOptions.IncludePrefab)
                .Build(this);

            RequireForUpdate(bakingCarQuery);
            RequireForUpdate<TrafficDestinationConfigReference>();
        }

        protected override void OnUpdate()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            var destinationConfig = SystemAPI.GetSingleton<TrafficDestinationConfigReference>();

            bool hasTriggerNodes = false;

            if (triggerNodeConfigQuery.HasSingleton<TriggerNodeConfigReference>())
            {
                hasTriggerNodes = triggerNodeConfigQuery.GetSingleton<TriggerNodeConfigReference>().Config.Value.IsActivated;
            }

            Entities
            .WithoutBurst()
            .WithEntityQueryOptions(EntityQueryOptions.IncludePrefab)
            .WithAll<TrafficEntityEditorBakingTag>()
            .ForEach((
                Entity prefabEntity,
                in CarModelComponent carModelComponent) =>
            {
                if (hasTriggerNodes)
                {
                    commandBuffer.AddComponent<TriggerNodeTag>(prefabEntity);
                    commandBuffer.SetComponentEnabled<TriggerNodeTag>(prefabEntity, false);
                }

                if (EntityManager.HasComponent<TrafficDestinationBakingSharedConfig>(prefabEntity))
                {
                    var overridenDestinationConfig = EntityManager.GetComponentData<TrafficDestinationBakingSharedConfig>(prefabEntity);

                    commandBuffer.AddSharedComponent(prefabEntity, new TrafficDestinationSharedConfig()
                    {
                        MinDistanceToTarget = overridenDestinationConfig.MinDistanceToTarget,
                        MinDistanceToPathPointTarget = overridenDestinationConfig.MinDistanceToPathPointTarget,
                        MaxDistanceFromPreviousLightSQ = overridenDestinationConfig.MaxDistanceFromPreviousLightSQ,
                        MinDistanceToNewLight = overridenDestinationConfig.MinDistanceToNewLight,
                        MinDistanceToTargetRouteNode = overridenDestinationConfig.MinDistanceToTargetRouteNode,
                        MinDistanceToTargetRailRouteNode = overridenDestinationConfig.MinDistanceToTargetRailRouteNode,
                        Unique = true
                    });

                    commandBuffer.AddComponent<TrafficCustomTargetingTag>(prefabEntity);
                }

            }).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }
}