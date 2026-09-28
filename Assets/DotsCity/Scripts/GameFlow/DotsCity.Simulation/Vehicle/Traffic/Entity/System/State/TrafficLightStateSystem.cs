using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.Gameplay.Road;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(PreEarlyJobGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficLightStateSystem : ISystem
    {
        private EntityQuery vehicleQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            vehicleQuery = SystemAPI.QueryBuilder()
                .WithNone<TrafficChangingLaneEventTag, TrafficWagonComponent>()
                .WithAllRW<TrafficStateComponent, TrafficLightDataComponent>()
                .WithPresentRW<TrafficIdleTag>()
                .WithAll<TrafficTag, HasDriverTag, TrafficDestinationComponent>()
                .Build();

            state.RequireForUpdate(vehicleQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var lightStateJob = new LightStateJob()
            {
                TrafficNodeLookup = SystemAPI.GetComponentLookup<TrafficNodeComponent>(true),
                LightHandlerLookup = SystemAPI.GetComponentLookup<LightHandlerComponent>(true),
                PathConnectionLookup = SystemAPI.GetBufferLookup<PathConnectionElement>(true),
            };

            state.Dependency = lightStateJob.ScheduleParallel(vehicleQuery, state.Dependency);
        }

        [WithNone(typeof(TrafficChangingLaneEventTag), typeof(TrafficWagonComponent))]
        [WithAll(typeof(TrafficTag), typeof(HasDriverTag))]
        [BurstCompile]
        private partial struct LightStateJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<TrafficNodeComponent> TrafficNodeLookup;

            [ReadOnly]
            public ComponentLookup<LightHandlerComponent> LightHandlerLookup;

            [ReadOnly]
            public BufferLookup<PathConnectionElement> PathConnectionLookup;

            void Execute(
                ref TrafficStateComponent trafficStateComponent,
                ref TrafficLightDataComponent trafficLightDataComponent,
                EnabledRefRW<TrafficIdleTag> trafficIdleTagRW,
                in TrafficDestinationComponent destinationComponent)
            {
                var lightState = LightState.Uninitialized;
                bool nextNodeState = false;

                if (TrafficNodeLookup.HasComponent(destinationComponent.DestinationNode))
                {
                    var lightEntity = TrafficNodeLookup[destinationComponent.DestinationNode].LightEntity;

                    if (LightHandlerLookup.HasComponent(lightEntity))
                    {
                        lightState = LightHandlerLookup[lightEntity].State;
                    }
                }

                if (lightState == LightState.Uninitialized && destinationComponent.NextShortPath && TrafficNodeLookup.HasComponent(destinationComponent.NextDestinationNode))
                {
                    var lightEntity = TrafficNodeLookup[destinationComponent.NextDestinationNode].LightEntity;

                    if (LightHandlerLookup.HasComponent(lightEntity))
                    {
                        lightState = LightHandlerLookup[lightEntity].State;
                        nextNodeState = true;
                    }
                }

                trafficLightDataComponent.NextNodeState = nextNodeState;

                if (trafficLightDataComponent.LightStateOfTargetNode != lightState)
                {
                    trafficLightDataComponent.LightStateOfTargetNode = lightState;
                }

                if (trafficStateComponent.TrafficLightCarState == TrafficLightCarState.FarFromLight)
                {
                    if (destinationComponent.CurrentNode != Entity.Null)
                    {
                        trafficStateComponent.TrafficLightCarState = TrafficLightCarState.InRange;
                    }
                }

                if (trafficStateComponent.TrafficLightCarState == TrafficLightCarState.InRange)
                {
                    var currentNodeEntity = destinationComponent.CurrentNode;
                    Entity targetNode = Entity.Null;

                    if (destinationComponent.NextDestinationNode != Entity.Null)
                    {
                        targetNode = destinationComponent.NextDestinationNode;
                    }
                    else if (destinationComponent.CurrentNode != destinationComponent.DestinationNode)
                    {
                        targetNode = destinationComponent.DestinationNode;
                    }

                    if (TrafficNodeLookup.HasComponent(currentNodeEntity) && TrafficNodeLookup.HasComponent(targetNode))
                    {
                        var customLightEntity = Entity.Null;

                        var trafficNodeEntity = currentNodeEntity;

                        if (PathConnectionLookup.HasBuffer(trafficNodeEntity))
                        {
                            var pathConnections = PathConnectionLookup[trafficNodeEntity];

                            for (int i = 0; i < pathConnections.Length; i++)
                            {
                                if (pathConnections[i].ConnectedNodeEntity == targetNode)
                                {
                                    customLightEntity = pathConnections[i].CustomLightEntity;
                                    break;
                                }
                            }
                        }

                        Entity lightEntity = Entity.Null;

                        if (customLightEntity == Entity.Null)
                        {
                            lightEntity = TrafficNodeLookup[currentNodeEntity].LightEntity;
                        }
                        else
                        {
                            lightEntity = customLightEntity;
                        }

                        trafficLightDataComponent.CurrentLightEntity = lightEntity;
                        trafficStateComponent.TrafficLightCarState = TrafficLightCarState.InRangeAndInitialized;
                    }
                }

                if (trafficStateComponent.TrafficLightCarState == TrafficLightCarState.InRangeAndInitialized)
                {
                    TrafficState carState = TrafficState.Default;

                    var lightEntity = trafficLightDataComponent.CurrentLightEntity;

                    if (LightHandlerLookup.HasComponent(lightEntity))
                    {
                        LightState currentLightState = LightHandlerLookup[lightEntity].State;

                        if (currentLightState != LightState.Green && currentLightState != LightState.Uninitialized)
                        {
                            carState = TrafficState.IsWaitingForGreenLight;
                        }
                    }

                    if (trafficStateComponent.TrafficState != carState)
                    {
                        if (trafficStateComponent.TrafficState != TrafficState.IsWaitingForGreenLight && carState == TrafficState.IsWaitingForGreenLight)
                        {
                            TrafficStateExtension.AddIdleState(ref trafficStateComponent, ref trafficIdleTagRW, TrafficIdleState.IsWaitingForGreenLight);
                        }

                        if (trafficStateComponent.TrafficState == TrafficState.IsWaitingForGreenLight && carState == TrafficState.Default)
                        {
                            TrafficStateExtension.RemoveIdleState(ref trafficStateComponent, ref trafficIdleTagRW, TrafficIdleState.IsWaitingForGreenLight);
                        }

                        if (trafficStateComponent.TrafficState == TrafficState.Default || trafficStateComponent.TrafficState == TrafficState.IsWaitingForGreenLight)
                        {
                            trafficStateComponent.TrafficState = carState;
                        }
                    }

                    if (destinationComponent.CurrentNode == Entity.Null || destinationComponent.CurrentNode != trafficLightDataComponent.LastCurrentNode)
                    {
                        trafficLightDataComponent.LastCurrentNode = destinationComponent.CurrentNode;

                        if (trafficStateComponent.TrafficState == TrafficState.IsWaitingForGreenLight)
                        {
                            trafficStateComponent.TrafficState = TrafficState.Default;
                            TrafficStateExtension.RemoveIdleState(ref trafficStateComponent, ref trafficIdleTagRW, TrafficIdleState.IsWaitingForGreenLight);
                        }

                        trafficStateComponent.TrafficLightCarState = TrafficLightCarState.FarFromLight;
                    }
                }
            }
        }
    }
}