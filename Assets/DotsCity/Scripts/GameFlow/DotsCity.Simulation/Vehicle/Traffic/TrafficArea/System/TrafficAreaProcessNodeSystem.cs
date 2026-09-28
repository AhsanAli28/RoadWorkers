using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.Gameplay.Road;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.TrafficArea
{
    [UpdateInGroup(typeof(TrafficAreaSimulationGroup))]
    [BurstCompile]
    public partial struct TrafficAreaProcessNodeSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<TrafficAreaProcessEnteredNodeTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var enteredTrafficAreaNodeJob = new EnteredTrafficAreaNodeJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                EnterQueueLookup = SystemAPI.GetBufferLookup<TrafficAreaEnterCarQueueElement>(false),
                ExitQueueLookup = SystemAPI.GetBufferLookup<TrafficAreaExitCarQueueElement>(false),
                TrafficStateLookup = SystemAPI.GetComponentLookup<TrafficStateComponent>(true),
                TrafficAreaLookup = SystemAPI.GetComponentLookup<TrafficAreaComponent>(true),
                TrafficAreaLinkedLookup = SystemAPI.GetComponentLookup<TrafficAreaLinked>(true),
                TrafficAreaHasExitParkingOrderLookup = SystemAPI.GetComponentLookup<TrafficAreaHasExitParkingOrderTag>(true),
            };

            enteredTrafficAreaNodeJob.Schedule();
        }

        [WithAll(typeof(TrafficAreaProcessEnteredNodeTag))]
        [BurstCompile]
        public partial struct EnteredTrafficAreaNodeJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            public BufferLookup<TrafficAreaEnterCarQueueElement> EnterQueueLookup;
            public BufferLookup<TrafficAreaExitCarQueueElement> ExitQueueLookup;

            [ReadOnly]
            public ComponentLookup<TrafficStateComponent> TrafficStateLookup;

            [ReadOnly]
            public ComponentLookup<TrafficAreaComponent> TrafficAreaLookup;

            [ReadOnly]
            public ComponentLookup<TrafficAreaLinked> TrafficAreaLinkedLookup;

            [ReadOnly]
            public ComponentLookup<TrafficAreaHasExitParkingOrderTag> TrafficAreaHasExitParkingOrderLookup;

            void Execute(
                Entity nodeEntity,
                ref TrafficAreaEntryNodeComponent trafficAreaQueueEntryComponent,
                in TrafficNodeSettingsComponent trafficNodeSettingsComponent,
                in TrafficAreaNode trafficAreaNode)
            {
                var trafficEntity = trafficAreaQueueEntryComponent.TrafficEntity;

                if (!TrafficStateLookup.HasComponent(trafficEntity))
                {
                    trafficAreaQueueEntryComponent.TrafficEntity = Entity.Null;
                    CommandBuffer.SetComponentEnabled<TrafficAreaProcessEnteredNodeTag>(nodeEntity, false);
                    return;
                }

                var areaEntity = trafficAreaNode.AreaEntity;

                bool initialized = TrafficAreaLinkedLookup.HasComponent(trafficEntity);
                bool shouldInit = false;

                if (!initialized && trafficAreaNode.TrafficAreaNodeType != TrafficAreaNodeType.Exit)
                {
                    shouldInit = true;
                    CommandBuffer.AddComponent<TrafficAreaAlignedTag>(trafficEntity);
                    CommandBuffer.AddComponent(trafficEntity, new TrafficAreaLinked()
                    {
                        AreaEntity = areaEntity
                    });

                    CommandBuffer.AddComponent<TrafficWaitForExitTag>(trafficEntity);
                    CommandBuffer.AddComponent<TrafficMovingToExitTag>(trafficEntity);
                    CommandBuffer.AddComponent<TrafficWaitForEnterAreaTag>(trafficEntity);

                    CommandBuffer.SetComponentEnabled<TrafficWaitForExitTag>(trafficEntity, false);
                    CommandBuffer.SetComponentEnabled<TrafficMovingToExitTag>(trafficEntity, false);
                    CommandBuffer.SetComponentEnabled<TrafficWaitForEnterAreaTag>(trafficEntity, false);
                }

                switch (trafficAreaNode.TrafficAreaNodeType)
                {
                    case TrafficAreaNodeType.Default:

                        if (shouldInit)
                        {
                            ExitQueueLookup[areaEntity].Add(new TrafficAreaExitCarQueueElement()
                            {
                                TrafficEntity = trafficEntity
                            });

                            CommandBuffer.SetComponentEnabled<TrafficAreaProcessingExitQueueTag>(areaEntity, true);
                        }

                        if (TrafficAreaHasExitParkingOrderLookup.HasComponent(areaEntity) && trafficNodeSettingsComponent.TrafficNodeType == TrafficNodeType.Parking)
                        {
                            CommandBuffer.SetComponentEnabled<TrafficWaitForExitTag>(trafficEntity, true);
                        }

                        break;
                    case TrafficAreaNodeType.Enter:
                        {
                            bool added = false;
                            var queue = EnterQueueLookup[areaEntity];

#if UNITY_EDITOR
                            for (int i = 0; i < queue.Length; i++)
                            {
                                if (queue[i].TrafficEntity == trafficEntity)
                                {
                                    added = true;
                                    break;
                                }
                            }
#endif

                            if (!added)
                            {
                                queue.Add(new TrafficAreaEnterCarQueueElement()
                                {
                                    TrafficEntity = trafficEntity
                                });
                            }
                            else
                            {
#if UNITY_EDITOR
                                UnityEngine.Debug.Log("TrafficAreaProcessNodeSystem. Attempt to add exist enter queue. Make sure the car enters the enter node first and then the queue node.");
#endif
                            }

                            CommandBuffer.SetComponentEnabled<TrafficAreaProcessingEnterQueueTag>(areaEntity, true);
                            CommandBuffer.SetComponentEnabled<TrafficAreaUpdateLockStateTag>(areaEntity, true);

                            break;
                        }
                    case TrafficAreaNodeType.Queue:
                        {
                            if (shouldInit)
                            {
                                EnterQueueLookup[areaEntity].Insert(0, new TrafficAreaEnterCarQueueElement()
                                {
                                    TrafficEntity = trafficEntity
                                });

                                CommandBuffer.SetComponentEnabled<TrafficAreaProcessingEnterQueueTag>(areaEntity, true);
                                CommandBuffer.SetComponentEnabled<TrafficAreaUpdateLockStateTag>(areaEntity, true);
                            }

                            bool entryAllowed = TrafficAreaLookup[areaEntity].ExitCarCount == 0;

                            if (entryAllowed)
                            {
                                var enterQueue = EnterQueueLookup[areaEntity];
                                var exitQueue = ExitQueueLookup[areaEntity];

                                for (int i = 0; i < enterQueue.Length; i++)
                                {
                                    if (enterQueue[i].TrafficEntity == trafficEntity)
                                    {
                                        enterQueue.RemoveAt(i);
                                        CommandBuffer.SetComponentEnabled<TrafficAreaUpdateLockStateTag>(areaEntity, true);
                                        break;
                                    }
                                }

                                exitQueue.Add(new TrafficAreaExitCarQueueElement()
                                {
                                    TrafficEntity = trafficEntity
                                });

                                CommandBuffer.SetComponentEnabled<TrafficAreaProcessingExitQueueTag>(areaEntity, true);
                            }
                            else
                            {
                                var trafficStateComponent = TrafficStateLookup[trafficEntity];
                                TrafficStateExtension.AddIdleState(ref CommandBuffer, trafficEntity, ref trafficStateComponent, TrafficIdleState.WaitForEnterArea);
                                CommandBuffer.SetComponentEnabled<TrafficWaitForEnterAreaTag>(trafficEntity, true);
                                CommandBuffer.SetComponent(trafficEntity, trafficStateComponent);
                            }

                            break;
                        }
                    case TrafficAreaNodeType.Exit:
                        {
                            if (initialized)
                            {
                                CommandBuffer.RemoveComponent<TrafficAreaAlignedTag>(trafficEntity);
                                CommandBuffer.RemoveComponent<TrafficWaitForExitTag>(trafficEntity);
                                CommandBuffer.RemoveComponent<TrafficMovingToExitTag>(trafficEntity);
                                CommandBuffer.RemoveComponent<TrafficWaitForEnterAreaTag>(trafficEntity);
                            }

                            break;
                        }
                }

                trafficAreaQueueEntryComponent.TrafficEntity = Entity.Null;

                CommandBuffer.SetComponentEnabled<TrafficAreaProcessEnteredNodeTag>(nodeEntity, false);
            }
        }
    }
}