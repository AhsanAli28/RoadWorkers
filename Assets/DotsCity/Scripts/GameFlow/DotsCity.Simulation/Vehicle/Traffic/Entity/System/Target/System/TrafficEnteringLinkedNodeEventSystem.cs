using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Road;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateAfter(typeof(TrafficFindNextTrafficNodeSystem))]
    [UpdateInGroup(typeof(TrafficProcessNodeGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficEnteringLinkedNodeEventSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<TrafficNodeLinkedComponent>()
                .WithAll<TrafficTag, HasDriverTag, TrafficEnteringTriggerNodeTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var enteringTrafficNodeJob = new EnteringTrafficNodeJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                TrafficNodeCapacityLookup = SystemAPI.GetComponentLookup<TrafficNodeCapacityComponent>(false),
                NodeSettingsLookup = SystemAPI.GetComponentLookup<TrafficNodeSettingsComponent>(true),
                TrafficRoadConfigReference = SystemAPI.GetSingleton<TrafficRoadConfigReference>()
            };

            enteringTrafficNodeJob.Run();
        }

        [WithNone(typeof(TrafficNodeLinkedComponent), typeof(TrafficWagonComponent))]
        [WithAll(typeof(TrafficTag), typeof(HasDriverTag), typeof(TrafficEnteringTriggerNodeTag))]
        [BurstCompile]
        private partial struct EnteringTrafficNodeJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            public ComponentLookup<TrafficNodeCapacityComponent> TrafficNodeCapacityLookup;

            [ReadOnly]
            public ComponentLookup<TrafficNodeSettingsComponent> NodeSettingsLookup;

            [ReadOnly]
            public TrafficRoadConfigReference TrafficRoadConfigReference;

            private void Execute(
                Entity entity,
                ref TrafficDestinationComponent destinationComponent,
                EnabledRefRW<TrafficEnteringTriggerNodeTag> trafficEnteringTriggerNodeTagRW)
            {
                trafficEnteringTriggerNodeTagRW.ValueRW = false;

                var destinationNode = destinationComponent.DestinationNode;

                if (NodeSettingsLookup.HasComponent(destinationNode))
                {
                    TrafficNodeCapacityUtils.TryToLinkNode(
                        entity,
                        destinationNode,
                        ref CommandBuffer,
                        ref TrafficNodeCapacityLookup,
                        in NodeSettingsLookup,
                        in TrafficRoadConfigReference);
                }
            }
        }
    }
}
