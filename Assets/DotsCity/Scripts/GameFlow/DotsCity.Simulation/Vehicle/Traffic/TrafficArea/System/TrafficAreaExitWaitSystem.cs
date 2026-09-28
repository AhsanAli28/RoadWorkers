using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.TrafficArea
{
    [UpdateAfter(typeof(TrafficAreaObserveSystem))]
    [UpdateInGroup(typeof(TrafficAreaSimulationGroup))]
    [BurstCompile]
    public partial struct TrafficAreaExitWaitSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<TrafficAreaHasExitParkingOrderTag, TrafficAreaProcessingExitQueueTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var exitWaitJob = new ExitWaitJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                HasDriverLookup = SystemAPI.GetComponentLookup<HasDriverTag>(true),
                TrafficWaitForExitLookup = SystemAPI.GetComponentLookup<TrafficWaitForExitTag>(true),
                TrafficStateLookup = SystemAPI.GetComponentLookup<TrafficStateComponent>(true),
            };

            exitWaitJob.Schedule();
        }

        [WithAll(typeof(TrafficAreaHasExitParkingOrderTag), typeof(TrafficAreaProcessingExitQueueTag))]
        [BurstCompile]
        public partial struct ExitWaitJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public ComponentLookup<HasDriverTag> HasDriverLookup;

            [ReadOnly]
            public ComponentLookup<TrafficWaitForExitTag> TrafficWaitForExitLookup;

            [ReadOnly]
            public ComponentLookup<TrafficStateComponent> TrafficStateLookup;

            void Execute(
                Entity entity,
                in TrafficAreaComponent trafficAreaComponent,
                in DynamicBuffer<TrafficAreaExitCarQueueElement> exitQueue,
                in DynamicBuffer<TrafficAreaEnterCarQueueElement> enterQueue)
            {
                bool shouldSkipOrder = trafficAreaComponent.SkipOrderSupported && trafficAreaComponent.SkippedOrderCount >= trafficAreaComponent.MaxSkipOrderCount && enterQueue.Length > 0;

                if (exitQueue.Length > 0 && trafficAreaComponent.ActiveCurrentCarCount == 0 && !shouldSkipOrder)
                {
                    for (int i = 0; i < exitQueue.Length; i++)
                    {
                        var trafficEntity = exitQueue[i].TrafficEntity;

                        if (HasDriverLookup.HasComponent(trafficEntity) && TrafficStateLookup.HasComponent(trafficEntity))
                        {
                            var trafficStateComponent = TrafficStateLookup[trafficEntity];

                            if (trafficStateComponent.TrafficIdleState == TrafficIdleState.WaitForExitArea)
                            {
                                TrafficStateExtension.RemoveIdleState(ref CommandBuffer, trafficEntity, ref trafficStateComponent, TrafficIdleState.WaitForExitArea);
                                CommandBuffer.SetComponentEnabled<TrafficMovingToExitTag>(trafficEntity, true);
                                CommandBuffer.SetComponent(trafficEntity, trafficStateComponent);

                                if (TrafficWaitForExitLookup.HasComponent(trafficEntity))
                                {
                                    CommandBuffer.SetComponentEnabled<TrafficWaitForExitTag>(trafficEntity, false);
                                }

                                break;
                            }
                        }
                    }
                }

                if (exitQueue.Length == 0)
                {
                    CommandBuffer.SetComponentEnabled<TrafficAreaProcessingExitQueueTag>(entity, false);
                }
            }
        }
    }
}
