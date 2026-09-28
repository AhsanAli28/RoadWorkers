using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Spirit604.DotsCity.Simulation.TrafficArea
{
    [UpdateAfter(typeof(TrafficAreaExitWaitSystem))]
    [UpdateInGroup(typeof(TrafficAreaSimulationGroup))]
    [BurstCompile]
    public partial struct TrafficAreaEnterQueueSystem : ISystem
    {
        private SystemHandle trafficNodeCalculateOverlapSystem;
        private SystemHandle trafficAreaExitWaitSystem;
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            trafficNodeCalculateOverlapSystem = state.WorldUnmanaged.GetExistingUnmanagedSystem<TrafficNodeCalculateOverlapSystem>();
            trafficAreaExitWaitSystem = state.WorldUnmanaged.GetExistingUnmanagedSystem<TrafficAreaExitWaitSystem>();

            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<TrafficAreaProcessingEnterQueueTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            ref var trafficNodeCalculateOverlapSystemRef = ref state.WorldUnmanaged.ResolveSystemStateRef(trafficNodeCalculateOverlapSystem);
            ref var trafficAreaExitWaitSystemRef = ref state.WorldUnmanaged.ResolveSystemStateRef(trafficAreaExitWaitSystem);

            var depJob = JobHandle.CombineDependencies(trafficNodeCalculateOverlapSystemRef.Dependency, trafficAreaExitWaitSystemRef.Dependency);

            var enterQueueNodeJob = new EnterQueueNodeJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                TrafficTargetLookup = SystemAPI.GetComponentLookup<TrafficDestinationComponent>(true),
                TrafficNodeAvailableLookup = SystemAPI.GetComponentLookup<TrafficNodeAvailableComponent>(true),
                WaitEnterForEnterAreaLookup = SystemAPI.GetComponentLookup<TrafficWaitForEnterAreaTag>(true),
                TrafficStateLookup = SystemAPI.GetComponentLookup<TrafficStateComponent>(true),
            };

            state.Dependency = enterQueueNodeJob.Schedule(depJob);
        }

        [WithAll(typeof(TrafficAreaProcessingEnterQueueTag))]
        [BurstCompile]
        public partial struct EnterQueueNodeJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public ComponentLookup<TrafficDestinationComponent> TrafficTargetLookup;

            [ReadOnly]
            public ComponentLookup<TrafficNodeAvailableComponent> TrafficNodeAvailableLookup;

            [ReadOnly]
            public ComponentLookup<TrafficWaitForEnterAreaTag> WaitEnterForEnterAreaLookup;

            [ReadOnly]
            public ComponentLookup<TrafficStateComponent> TrafficStateLookup;

            void Execute(
                Entity entity,
                ref DynamicBuffer<TrafficAreaEnterCarQueueElement> enterQueue,
                ref DynamicBuffer<TrafficAreaExitCarQueueElement> exitQueue,
                ref TrafficAreaComponent trafficAreaComponent)
            {
                bool skipped = trafficAreaComponent.SkipOrderSupported && trafficAreaComponent.SkippedOrderCount >= trafficAreaComponent.MaxSkipOrderCount && trafficAreaComponent.ActiveCurrentCarCount == 0;

                if (enterQueue.Length > 0 && (trafficAreaComponent.ExitCarCount == 0 || skipped))
                {
                    var enterData = enterQueue[0];
                    var queueTrafficEntity = enterData.TrafficEntity;

                    if (!TrafficTargetLookup.HasComponent(queueTrafficEntity))
                    {
                        enterQueue.RemoveAt(0);
                        return;
                    }

                    if (skipped)
                    {
                        var trafficTargetComponent = TrafficTargetLookup[queueTrafficEntity];
                        var targetEntity = trafficTargetComponent.DestinationNode;
                        var targetAvailable = TrafficNodeAvailableLookup[targetEntity].IsAvailable;

                        if (!targetAvailable)
                        {
                            trafficAreaComponent.SkippedOrderCount = 0;
                            return;
                        }
                    }

                    if (WaitEnterForEnterAreaLookup.HasComponent(queueTrafficEntity) && WaitEnterForEnterAreaLookup.IsComponentEnabled(queueTrafficEntity))
                    {
                        enterQueue.RemoveAt(0);

                        exitQueue.Add(new TrafficAreaExitCarQueueElement()
                        {
                            TrafficEntity = queueTrafficEntity
                        });

                        trafficAreaComponent.SkippedOrderCount = 0;

                        CommandBuffer.SetComponentEnabled<TrafficAreaProcessingExitQueueTag>(entity, true);
                        CommandBuffer.SetComponentEnabled<TrafficAreaUpdateLockStateTag>(entity, true);

                        var trafficStateComponent = TrafficStateLookup[queueTrafficEntity];

                        TrafficStateExtension.RemoveIdleState<TrafficWaitForEnterAreaTag>(ref CommandBuffer, queueTrafficEntity, ref trafficStateComponent, TrafficIdleState.WaitForEnterArea);

                        CommandBuffer.SetComponent(queueTrafficEntity, trafficStateComponent);
                    }
                }

                if (enterQueue.Length == 0)
                {
                    CommandBuffer.SetComponentEnabled<TrafficAreaProcessingEnterQueueTag>(entity, false);
                }
            }
        }
    }
}
