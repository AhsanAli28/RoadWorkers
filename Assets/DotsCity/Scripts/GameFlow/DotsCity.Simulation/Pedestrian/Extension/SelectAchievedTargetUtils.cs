using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Events;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.Gameplay.Road;
using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public static class SelectAchievedTargetUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ProcessAchievedTarget(
            ref EntityCommandBuffer commandBuffer,
            in BufferLookup<NodeConnectionDataElement> nodeConnectionBufferLookup,
            in ComponentLookup<NodeSettingsComponent> nodeSettingsLookup,
            in ComponentLookup<NodeCapacityComponent> nodeCapacityLookup,
            in ComponentLookup<NodeLightSettingsComponent> nodeLightSettingsComponentLookup,
            in ComponentLookup<LightHandlerComponent> lightHandlerLookup,
            in ComponentLookup<LocalToWorld> worldTransformLookup,
            in DestinationConfigReference destinationConfigReference,
            in PedestrianGeneralSettingsReference pedestrianGeneralSettingsReference,
            in float timestamp,
            Entity pedestrianEntity,
            ref DestinationComponent destinationComponent,
            ref NextStateComponent nextStateComponent,
            ref EnabledRefRW<HasTargetTag> hasTargetTagRW,
            in LocalToWorld worldTransform)
        {
            bool disableTarget = true;
            Entity achievedTargetEntity = destinationComponent.DestinationNode;

            if (!nodeSettingsLookup.HasComponent(achievedTargetEntity))
            {
                destinationComponent = destinationComponent.SwapBack();
                return;
            }

            NodeSettingsComponent nodeSettingsComponent = nodeSettingsLookup[achievedTargetEntity];

            switch (nodeSettingsComponent.NodeType)
            {
                case PedestrianNodeType.Default:
                    {
                        disableTarget = false;

                        ProcessDefaultNodeSystem.Process(
                            in nodeConnectionBufferLookup,
                            in nodeSettingsLookup,
                            in nodeCapacityLookup,
                            in nodeLightSettingsComponentLookup,
                            in lightHandlerLookup,
                            in worldTransformLookup,
                            in destinationConfigReference,
                            in timestamp,
                            pedestrianEntity,
                            ref destinationComponent,
                            ref nextStateComponent,
                            in worldTransform);

                        break;
                    }
                case PedestrianNodeType.House:
                    {
                        PoolEntityUtils.DestroyEntity(ref commandBuffer, pedestrianEntity);
                        break;
                    }
                case PedestrianNodeType.Sit:
                    {
                        if (pedestrianGeneralSettingsReference.Config.Value.BenchSystemSupport)
                        {
                            commandBuffer.AddComponent<ProcessEnterSeatNodeTag>(pedestrianEntity);
                        }
                        else
                        {
                            disableTarget = false;

                            ProcessDefaultNodeSystem.Process(
                                in nodeConnectionBufferLookup,
                                in nodeSettingsLookup,
                                in nodeCapacityLookup,
                                in nodeLightSettingsComponentLookup,
                                in lightHandlerLookup,
                                in worldTransformLookup,
                                in destinationConfigReference,
                                in timestamp,
                                pedestrianEntity,
                                ref destinationComponent,
                                ref nextStateComponent,
                                in worldTransform);
                        }

                        break;
                    }
                case PedestrianNodeType.Idle:
                    {
                        commandBuffer.SetComponentEnabled<IdleTag>(pedestrianEntity, true);
                        commandBuffer.AddComponent(pedestrianEntity, new IdleTimeComponent()
                        {
                            IdleNode = achievedTargetEntity
                        });

                        break;
                    }
                case PedestrianNodeType.CarParking:
                    {
                        if (pedestrianGeneralSettingsReference.Config.Value.ParkingSupport)
                        {
                            commandBuffer.AddComponent<ProcessEnterCarParkingNodeTag>(pedestrianEntity);
                        }
                        else
                        {
                            disableTarget = false;

                            ProcessDefaultNodeSystem.Process(
                                in nodeConnectionBufferLookup,
                                in nodeSettingsLookup,
                                in nodeCapacityLookup,
                                in nodeLightSettingsComponentLookup,
                                in lightHandlerLookup,
                                in worldTransformLookup,
                                in destinationConfigReference,
                                in timestamp,
                                pedestrianEntity,
                                ref destinationComponent,
                                ref nextStateComponent,
                                in worldTransform);
                        }

                        break;
                    }
                case PedestrianNodeType.TalkArea:
                    {
                        disableTarget = false;

                        ProcessDefaultNodeSystem.Process(
                            in nodeConnectionBufferLookup,
                            in nodeSettingsLookup,
                            in nodeCapacityLookup,
                            in nodeLightSettingsComponentLookup,
                            in lightHandlerLookup,
                            in worldTransformLookup,
                            in destinationConfigReference,
                            in timestamp,
                            pedestrianEntity,
                            ref destinationComponent,
                            ref nextStateComponent,
                            in worldTransform);

                        break;
                    }
                case PedestrianNodeType.TrafficPublicStopStation:
                    {
                        if (pedestrianGeneralSettingsReference.Config.Value.TrafficPublicSupport)
                        {
                            commandBuffer.AddComponent<ProcessEnterTrafficStationNodeTag>(pedestrianEntity);
                        }
                        else
                        {
                            disableTarget = false;

                            ProcessDefaultNodeSystem.Process(
                                in nodeConnectionBufferLookup,
                                in nodeSettingsLookup,
                                in nodeCapacityLookup,
                                in nodeLightSettingsComponentLookup,
                                in lightHandlerLookup,
                                in worldTransformLookup,
                                in destinationConfigReference,
                                in timestamp,
                                pedestrianEntity,
                                ref destinationComponent,
                                ref nextStateComponent,
                                in worldTransform);
                        }

                        break;
                    }
                case PedestrianNodeType.TrafficPublicEntry:
                    {
                        commandBuffer.AddComponent<ProcessEnterTrafficEntryNodeTag>(pedestrianEntity);
                        break;
                    }
                case PedestrianNodeType.Trigger:
                    {
                        commandBuffer.SetComponentEnabled<TriggerNodeTag>(pedestrianEntity, true);

                        disableTarget = false;

                        ProcessDefaultNodeSystem.Process(
                            in nodeConnectionBufferLookup,
                            in nodeSettingsLookup,
                            in nodeCapacityLookup,
                            in nodeLightSettingsComponentLookup,
                            in lightHandlerLookup,
                            in worldTransformLookup,
                            in destinationConfigReference,
                            in timestamp,
                            pedestrianEntity,
                            ref destinationComponent,
                            ref nextStateComponent,
                            in worldTransform);

                        break;
                    }
                case PedestrianNodeType.TriggerAndDestroy:
                    {
                        commandBuffer.SetComponentEnabled<TriggerNodeTag>(pedestrianEntity, true);

                        disableTarget = false;

                        ProcessDefaultNodeSystem.Process(
                            in nodeConnectionBufferLookup,
                            in nodeSettingsLookup,
                            in nodeCapacityLookup,
                            in nodeLightSettingsComponentLookup,
                            in lightHandlerLookup,
                            in worldTransformLookup,
                            in destinationConfigReference,
                            in timestamp,
                            pedestrianEntity,
                            ref destinationComponent,
                            ref nextStateComponent,
                            in worldTransform);

                        break;
                    }
            }

            if (disableTarget)
            {
                hasTargetTagRW.ValueRW = false;
            }
        }
    }
}