using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Level.Streaming;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.TrafficPublic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(TrafficProcessNodeGroup), OrderLast = true)]
    [BurstCompile]
    public partial struct TrafficWaitForResolveNodeSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<TrafficCustomTargetControlTag>()
                .WithAll<HasDriverTag, TrafficNoTargetTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
            state.RequireForUpdate<TrafficNodeResolverSystem.RuntimePathDataRef>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var switchTrafficNodeJob = new SwitchTrafficNodeJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                RuntimePathDataRef = SystemAPI.GetSingleton<TrafficNodeResolverSystem.RuntimePathDataRef>(),
                TrafficNodeSettingsLookup = SystemAPI.GetComponentLookup<TrafficNodeSettingsComponent>(true),
                TrafficFixedRouteTagLookup = SystemAPI.GetComponentLookup<TrafficFixedRouteComponent>(true),
                PathConnectionLookup = SystemAPI.GetBufferLookup<PathConnectionElement>(true),
                Graph = SystemAPI.GetSingleton<PathGraphSystem.Singleton>(),
            };

            switchTrafficNodeJob.Run();
        }

        [WithNone(typeof(TrafficCustomTargetControlTag))]
        [WithAll(typeof(HasDriverTag), typeof(TrafficNoTargetTag))]
        [BurstCompile]
        private partial struct SwitchTrafficNodeJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public TrafficNodeResolverSystem.RuntimePathDataRef RuntimePathDataRef;

            [ReadOnly]
            public ComponentLookup<TrafficNodeSettingsComponent> TrafficNodeSettingsLookup;

            [ReadOnly]
            public ComponentLookup<TrafficFixedRouteComponent> TrafficFixedRouteTagLookup;

            [ReadOnly]
            public BufferLookup<PathConnectionElement> PathConnectionLookup;

            [ReadOnly]
            public PathGraphSystem.Singleton Graph;

            private void Execute(
                Entity entity,
                ref TrafficPathComponent trafficPathComponent,
                ref TrafficDestinationComponent destinationComponent,
                ref TrafficStateComponent trafficStateComponent,
                EnabledRefRW<TrafficIdleTag> trafficIdleTagRW,
                in TrafficTypeComponent trafficTypeComponent)
            {
                var connectedNode = RuntimePathDataRef.TryToGetConnectedNode(trafficPathComponent.CurrentGlobalPathIndex);

                if (connectedNode == Entity.Null)
                    return;

                bool resolved = false;

                if (PathConnectionLookup.HasBuffer(connectedNode))
                {
                    if (connectedNode == destinationComponent.CurrentNode)
                    {
                        var pathConnections = PathConnectionLookup[connectedNode];

#if UNITY_EDITOR
                        bool allNodesExist = true;
#endif

                        for (int i = 0; i < pathConnections.Length; i++)
                        {
                            var pathSettingsLocal = pathConnections[i];

                            if (TrafficNodeSettingsLookup.HasComponent(pathSettingsLocal.ConnectedNodeEntity))
                            {
                                destinationComponent.DestinationNode = connectedNode;

                                ref readonly var localPathData = ref Graph.GetPathData(pathSettingsLocal.GlobalPathIndex);

                                var isAvailable = localPathData.IsAvailable(in trafficTypeComponent);

                                if (!TrafficFixedRouteTagLookup.HasComponent(entity))
                                {
                                    if (isAvailable)
                                    {
                                        CommandBuffer.SetComponentEnabled<TrafficNextTrafficNodeRequestTag>(entity, true);
                                        resolved = true;
                                        break;
                                    }
                                }
                                else
                                {
#if UNITY_EDITOR
                                    allNodesExist = false;
#endif

                                    var fixedRoute = TrafficFixedRouteTagLookup[entity];

                                    if (!fixedRoute.FinishedPath)
                                    {
                                        resolved = true;
                                        break;
                                    }
                                }
                            }
                            else
                            {
#if UNITY_EDITOR
                                allNodesExist = false;
#endif
                            }
                        }

#if UNITY_EDITOR
                        if (allNodesExist && !resolved)
                        {
                            for (int i = 0; i < pathConnections.Length; i++)
                            {
                                UnityEngine.Debug.Log($"TrafficWaitForResolveNodeSystem. TrafficCar {entity.Index} GlobalPathIndex {pathConnections[i].GlobalPathIndex} seems to be stucked forever. Make sure that the path has the correct TrafficGroup");
                            }
                        }
#endif

                    }
                    else
                    {
                        destinationComponent.DestinationNode = connectedNode;
                        resolved = true;
                    }
                }

                if (resolved)
                {
                    TrafficStateExtension.RemoveIdleState<TrafficNoTargetTag>(ref CommandBuffer, entity, ref trafficStateComponent, ref trafficIdleTagRW, TrafficIdleState.NoTarget);
                }
            }
        }
    }
}