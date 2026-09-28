using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Events;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.TrafficArea;
using Spirit604.Gameplay.Road;
using System.Runtime.CompilerServices;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public static class TrafficTargetUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ProcessAchievedTarget(
            ref EntityCommandBuffer commandBuffer,
            Entity entity,
            ref TrafficDestinationComponent destinationComponent,
            ref TrafficStateComponent trafficStateComponent,
            ref EnabledRefRW<TrafficEnteredTriggerNodeTag> trafficEnteredTriggerNodeTagRW,
            ref EnabledRefRW<TrafficEnteringTriggerNodeTag> trafficEnteringTriggerNodeTagRW,
            ref EnabledRefRW<TrafficSwitchTargetNodeRequestTag> trafficSwitchTargetNodeRequestTagRW,
            ref EnabledRefRW<TrafficIdleTag> trafficIdleTagRW,
            in ComponentLookup<TrafficNodeSettingsComponent> spawnPointSettingsLookup,
            in ComponentLookup<TrafficNodeCapacityComponent> trafficNodeCapacityLookup,
            in ComponentLookup<TrafficAreaNode> trafficAreaNodeLookup,
            in TrafficParkingConfigReference parkingConfig,
            bool lockAligment = false)
        {
            bool switchTarget = true;

            if (destinationComponent.PathConnectionType == PathConnectionType.TrafficNode)
            {
                var currentTrafficNodeEntity = destinationComponent.CurrentNode;

                if (currentTrafficNodeEntity != Entity.Null)
                {
                    var spawnpointSettingsComponent = spawnPointSettingsLookup[currentTrafficNodeEntity];
                    var capacityComponent = trafficNodeCapacityLookup[currentTrafficNodeEntity];
                    bool canLinkNode = capacityComponent.CanLinkNode(entity);

                    switch (spawnpointSettingsComponent.TrafficNodeType)
                    {
                        case TrafficNodeType.Parking:
                            {
                                if (canLinkNode)
                                {
                                    TrafficStateExtension.AddIdleState(ref trafficStateComponent, ref trafficIdleTagRW, TrafficIdleState.Parking);

                                    if (!parkingConfig.Config.Value.AligmentAtNode)
                                    {
                                        trafficEnteredTriggerNodeTagRW.ValueRW = true;
                                    }
                                    else
                                    {
                                        if (!capacityComponent.AlreadyLinked(entity))
                                        {
                                            trafficEnteringTriggerNodeTagRW.ValueRW = true;
                                        }

                                        commandBuffer.AddComponent<TrafficAccurateAligmentCustomMovementTag>(entity);
                                    }
                                }
                                else
                                {
                                    switchTarget = true;
                                }

                                break;
                            }
                        case TrafficNodeType.TrafficPublicStop:
                            {
                                if (canLinkNode)
                                {
                                    trafficEnteredTriggerNodeTagRW.ValueRW = true;

                                    if (!parkingConfig.Config.Value.AligmentAtNode || lockAligment)
                                    {
                                        trafficEnteredTriggerNodeTagRW.ValueRW = true;
                                    }
                                    else
                                    {
                                        if (!capacityComponent.AlreadyLinked(entity))
                                        {
                                            trafficEnteringTriggerNodeTagRW.ValueRW = true;
                                            commandBuffer.SetComponentEnabled<TrafficEnteringTriggerNodeTag>(entity, true);
                                        }

                                        commandBuffer.AddComponent<TrafficAccurateAligmentCustomMovementTag>(entity);
                                    }
                                }
                                else
                                {
                                    switchTarget = true;
                                }

                                break;
                            }
                        case TrafficNodeType.DestroyVehicle:
                            {
                                switchTarget = false;
                                PoolEntityUtils.DestroyEntity(ref commandBuffer, entity);
                                break;
                            }
                        case TrafficNodeType.TrafficArea:
                            {
                                break;
                            }
                        case TrafficNodeType.Idle:
                            {
                                commandBuffer.AddComponent<TrafficIdleNodeProcessComponent>(entity);
                                break;
                            }
                        case TrafficNodeType.TriggerLight:
                            {
                                commandBuffer.SetComponentEnabled<LightTriggerEnabledTag>(currentTrafficNodeEntity, true);
                                commandBuffer.SetComponent(currentTrafficNodeEntity, new LightTriggerBinding()
                                {
                                    TriggerEntity = entity
                                });

                                break;
                            }
                        case TrafficNodeType.Trigger:
                            {
                                commandBuffer.SetComponentEnabled<TriggerNodeTag>(entity, true);
                                break;
                            }
                        case TrafficNodeType.TriggerAndDestroy:
                            {
                                commandBuffer.SetComponentEnabled<TriggerNodeTag>(entity, true);
                                break;
                            }
                    }

                    if (trafficAreaNodeLookup.HasComponent(currentTrafficNodeEntity))
                    {
                        commandBuffer.SetComponent(currentTrafficNodeEntity, new TrafficAreaEntryNodeComponent()
                        {
                            TrafficEntity = entity
                        });

                        commandBuffer.SetComponentEnabled<TrafficAreaProcessEnteredNodeTag>(currentTrafficNodeEntity, true);
                    }
                }
            }

            if (switchTarget)
            {
                trafficSwitchTargetNodeRequestTagRW.ValueRW = true;
            }
        }
    }
}