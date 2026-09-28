using Spirit604.DotsCity.Core;
using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.TrafficPublic
{
    [UpdateInGroup(typeof(CleanupGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficPublicRouteCleanerSystem : ISystem
    {
        private EntityQuery updateGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateGroup = SystemAPI.QueryBuilder()
                .WithNone<PoolableTag>()
                .WithAll<TrafficFixedRouteLinkComponent>()
                .Build();

            state.RequireForUpdate(updateGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var cleanRouteJob = new CleanRouteJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                TrafficPublicRouteCapacityLookup = SystemAPI.GetComponentLookup<TrafficPublicRouteCapacityComponent>(false),
            };

            cleanRouteJob.Schedule();
        }

        [WithNone(typeof(PoolableTag))]
        [BurstCompile]
        public partial struct CleanRouteJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            public ComponentLookup<TrafficPublicRouteCapacityComponent> TrafficPublicRouteCapacityLookup;

            void Execute(
                Entity entity,
                ref TrafficFixedRouteLinkComponent trafficFixedRouteLinkComponent)
            {
                var routeEntity = trafficFixedRouteLinkComponent.RouteEntity;

                var route = TrafficPublicRouteCapacityLookup[routeEntity];

                route.CurrentVehicleCount--;

                TrafficPublicRouteCapacityLookup[routeEntity] = route;

                CommandBuffer.RemoveComponent<TrafficFixedRouteLinkComponent>(entity);
            }
        }
    }
}