using Spirit604.DotsCity.Simulation.Level.Streaming;
using Spirit604.DotsCity.Simulation.Navigation;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.TrafficPublic;
using Spirit604.Extensions;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(TrafficSimulationGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficPathfindingSystem : ISystem, ISystemStartStop
    {
        public struct PathRequest : IComponentData, IEnableableComponent
        {
            public int StartPathIndex;
            public Entity TargetNode;
            public int Index;
        }

        private struct NativePriorityQueueContainer
        {
            public NativePriorityQueue<int> Container;
        }

        private struct CameFromContainer
        {
            public NativeHashMap<int, int> Container;
        }

        private struct ScoreContainerContainer
        {
            public NativeHashMap<int, float> Container;
        }

        private struct PathContainer
        {
            public NativeList<int> Container;
        }

        private NativeArray<NativePriorityQueueContainer> openSet;
        private NativeArray<CameFromContainer> cameFrom;
        private NativeArray<ScoreContainerContainer> fScore;
        private NativeArray<ScoreContainerContainer> gScore;
        private NativeArray<PathContainer> pathResult;

        private EntityQuery _pathRequestQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PathGraphSystem.Singleton>();
            state.RequireForUpdate<TrafficPathAgentConfigReference>();
        }

        public void OnStartRunning(ref SystemState state)
        {
            if (cameFrom.IsCreated) return;

            var config = SystemAPI.GetSingleton<TrafficPathAgentConfigReference>();
            int agentCount = config.Config.Value.AgentCount;
            int bufferSize = config.Config.Value.BufferSize;

            openSet = new NativeArray<NativePriorityQueueContainer>(agentCount, Allocator.Persistent);
            cameFrom = new NativeArray<CameFromContainer>(agentCount, Allocator.Persistent);
            fScore = new NativeArray<ScoreContainerContainer>(agentCount, Allocator.Persistent);
            gScore = new NativeArray<ScoreContainerContainer>(agentCount, Allocator.Persistent);
            pathResult = new NativeArray<PathContainer>(agentCount, Allocator.Persistent);

            for (int i = 0; i < agentCount; i++)
            {
                openSet[i] = new NativePriorityQueueContainer()
                {
                    Container = new NativePriorityQueue<int>(bufferSize, Allocator.Persistent)
                };

                cameFrom[i] = new CameFromContainer()
                {
                    Container = new NativeHashMap<int, int>(bufferSize, Allocator.Persistent)
                };

                fScore[i] = new ScoreContainerContainer()
                {
                    Container = new NativeHashMap<int, float>(bufferSize, Allocator.Persistent)
                };

                gScore[i] = new ScoreContainerContainer()
                {
                    Container = new NativeHashMap<int, float>(bufferSize, Allocator.Persistent)
                };

                pathResult[i] = new PathContainer()
                {
                    Container = new NativeList<int>(bufferSize, Allocator.Persistent)
                };
            }

            _pathRequestQuery =
                SystemAPI.QueryBuilder()
                .WithAllRW<PathRequest, TrafficFixedRouteComponent>()
                .WithAllRW<FixedRouteNodeElement>()
                .Build();

            state.RequireForUpdate(_pathRequestQuery);

            state.RequireForUpdate(_pathRequestQuery);
        }

        public void OnStopRunning(ref SystemState state) { }

        public void OnDestroy(ref SystemState state)
        {
            foreach (var item in openSet)
            {
                item.Container.Dispose();
            }

            foreach (var item in cameFrom)
            {
                item.Container.Dispose();
            }

            foreach (var item in fScore)
            {
                item.Container.Dispose();
            }

            foreach (var item in gScore)
            {
                item.Container.Dispose();
            }

            foreach (var item in pathResult)
            {
                item.Container.Dispose();
            }

            openSet.Dispose();
            cameFrom.Dispose();
            gScore.Dispose();
            fScore.Dispose();
            pathResult.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var entityGraph = SystemAPI.GetSingleton<TrafficNodeResolverSystem.RuntimePathDataRef>();
            var roadGraph = SystemAPI.GetSingleton<PathGraphSystem.Singleton>();

            var job = new PathfindingJob
            {
                EntityGraph = entityGraph,
                RoadGraph = roadGraph,
                EntityHandle = SystemAPI.GetEntityTypeHandle(),
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                PathRequestHandle = SystemAPI.GetComponentTypeHandle<PathRequest>(false),
                PathAgentComponentHandle = SystemAPI.GetComponentTypeHandle<PathAgentComponent>(false),
                PathAgentUpdateStateHandle = SystemAPI.GetComponentTypeHandle<PathAgentUpdateState>(false),
                TrafficFixedRouteComponentHandle = SystemAPI.GetComponentTypeHandle<TrafficFixedRouteComponent>(false),
                TrafficCustomDestinationHandle = SystemAPI.GetComponentTypeHandle<TrafficDestinationComponent>(false),
                FixedRouteNodeElementHandle = SystemAPI.GetBufferTypeHandle<FixedRouteNodeElement>(false),
                OpenSetContainer = openSet,
                CameFromContainer = cameFrom,
                FScoreContainer = fScore,
                GScoreContainer = gScore,
                PathResultContainer = pathResult,
            };

            state.Dependency = job.ScheduleParallel(_pathRequestQuery, state.Dependency);
        }

        [BurstCompile]
        private struct PathfindingJob : IJobChunk
        {
            [NativeDisableContainerSafetyRestriction]
            [ReadOnly] public TrafficNodeResolverSystem.RuntimePathDataRef EntityGraph;

            [NativeDisableContainerSafetyRestriction]
            [ReadOnly] public PathGraphSystem.Singleton RoadGraph;

            [ReadOnly] public EntityTypeHandle EntityHandle;

            [NativeDisableContainerSafetyRestriction]
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;

            public ComponentTypeHandle<PathRequest> PathRequestHandle;

            public ComponentTypeHandle<PathAgentComponent> PathAgentComponentHandle;
            public ComponentTypeHandle<PathAgentUpdateState> PathAgentUpdateStateHandle;

            public ComponentTypeHandle<TrafficFixedRouteComponent> TrafficFixedRouteComponentHandle;

            [NativeDisableContainerSafetyRestriction]
            public ComponentTypeHandle<TrafficDestinationComponent> TrafficCustomDestinationHandle;

            public BufferTypeHandle<FixedRouteNodeElement> FixedRouteNodeElementHandle;

            [NativeDisableContainerSafetyRestriction]
            public NativeArray<NativePriorityQueueContainer> OpenSetContainer;

            [NativeDisableContainerSafetyRestriction]
            public NativeArray<CameFromContainer> CameFromContainer;

            [NativeDisableContainerSafetyRestriction]
            public NativeArray<ScoreContainerContainer> FScoreContainer;

            [NativeDisableContainerSafetyRestriction]
            public NativeArray<ScoreContainerContainer> GScoreContainer;

            [NativeDisableContainerSafetyRestriction]
            public NativeArray<PathContainer> PathResultContainer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(EntityHandle);
                var pathRequests = chunk.GetNativeArray(ref PathRequestHandle);
                var pathAgentComponents = chunk.GetNativeArray(ref PathAgentComponentHandle);
                var fixedComponents = chunk.GetNativeArray(ref TrafficFixedRouteComponentHandle);
                var destinationComponents = chunk.GetNativeArray(ref TrafficCustomDestinationHandle);
                var routeNodes = chunk.GetBufferAccessor(ref FixedRouteNodeElementHandle);

                var count = RoadGraph.Count();

                for (int index = 0; index < chunk.Count; index++)
                {
                    if (useEnabledMask && !chunk.IsComponentEnabled(ref PathRequestHandle, index))
                        continue;

                    chunk.SetComponentEnabled(ref PathRequestHandle, index, false);
                    chunk.SetComponentEnabled(ref PathAgentUpdateStateHandle, index, true);

                    var entity = entities[index];
                    var request = pathRequests[index];
                    var trafficFixedRouteComponent = fixedComponents[index];
                    var pathBuffer = routeNodes[index];
                    var startNode = request.StartPathIndex;
                    var endNode = request.TargetNode;

                    // Node was deleted
                    if (!LocalTransformLookup.HasComponent(endNode))
                    {
                        pathBuffer.Clear();

                        pathAgentComponents[index] = new PathAgentComponent()
                        {
                            Status = PathStatus.TargetNotExist
                        };

                        continue;
                    }

                    var goalPos = LocalTransformLookup[endNode].Position;
                    var openSet = OpenSetContainer[request.Index].Container;
                    var cameFrom = CameFromContainer[request.Index].Container;
                    var gScore = GScoreContainer[request.Index].Container;
                    var fScore = FScoreContainer[request.Index].Container;
                    var pathResult = PathResultContainer[request.Index].Container;

                    openSet.Clear();
                    cameFrom.Clear();
                    gScore.Clear();
                    fScore.Clear();

                    // Initialize scores
                    for (int i = 0; i < count; i++)
                    {
                        gScore[i] = float.MaxValue;
                        fScore[i] = float.MaxValue;
                    }

                    gScore[startNode] = 0;
                    fScore[startNode] = HeuristicCostEstimate(startNode, goalPos, in RoadGraph, ref trafficFixedRouteComponent);

                    openSet.Enqueue(startNode, fScore[startNode]);

                    bool found = false;

                    // A* algorithm
                    while (!openSet.IsEmpty)
                    {
                        int current = openSet.Dequeue();
                        var currentNode = EntityGraph.TryToGetConnectedNode(current);

                        if (currentNode == endNode)
                        {
                            ReconstructPath(in cameFrom, current, ref pathResult, ref pathBuffer, in RoadGraph, in EntityGraph);
                            found = true;
                            break;
                        }

                        // Process connected
                        var connectedPaths = RoadGraph.GetConnectedPaths(current);

                        for (int j = 0; j < connectedPaths.Length; j++)
                        {
                            int connectedPath = connectedPaths[j];
                            ref readonly var neighborPath = ref RoadGraph.GetPathData(connectedPath);
                            float tentativeGScore = gScore[current] + neighborPath.PathLength;

                            if (tentativeGScore < gScore[connectedPath])
                            {
                                cameFrom[connectedPath] = current;
                                gScore[connectedPath] = tentativeGScore;
                                fScore[connectedPath] = gScore[connectedPath] + HeuristicCostEstimate(connectedPath, goalPos, in RoadGraph, ref trafficFixedRouteComponent);

                                if (!openSet.Contains(connectedPath))
                                {
                                    openSet.Enqueue(connectedPath, fScore[connectedPath]);
                                }
                                else
                                {
                                    openSet.UpdateFScore(connectedPath, fScore[connectedPath]);
                                }
                            }
                        }
                    }

                    if (!found)
                    {
                        pathBuffer.Clear();

                        //#if UNITY_EDITOR
                        //                        UnityEngine.Debug.Log($"TrafficPathfindingSystem. Agent {entity.Index} Path not found");
                        //#endif

                        pathAgentComponents[index] = new PathAgentComponent()
                        {
                            Status = PathStatus.NotFound
                        };
                    }
                    else
                    {
                        pathAgentComponents[index] = new PathAgentComponent()
                        {
                            Status = PathStatus.Found
                        };
                    }

                    trafficFixedRouteComponent.RouteNodeIndex = 0;
                    trafficFixedRouteComponent.RouteEntity = entity;
                    trafficFixedRouteComponent.RouteLength = pathBuffer.Length;

                    fixedComponents[index] = trafficFixedRouteComponent;

                    var destinationComponent = destinationComponents[index];

                    if (pathBuffer.Length > 1)
                    {
                        destinationComponent.NextDestinationNode = pathBuffer[1].TrafficNodeEntity;
                        destinationComponent.NextGlobalPathIndex = pathBuffer[1].PathKey;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static float HeuristicCostEstimate(int start, float3 goalPos, in PathGraphSystem.Singleton singleton, ref TrafficFixedRouteComponent trafficFixedRouteComponent)
            {
                var startPos = singleton.GetEndPosition(start);

                return math.distance(startPos, goalPos);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void ReconstructPath(
                in NativeHashMap<int, int> cameFrom,
                int current,
                ref NativeList<int> pathResult,
                ref DynamicBuffer<FixedRouteNodeElement> pathBuffer,
                in PathGraphSystem.Singleton roadGraph,
                in TrafficNodeResolverSystem.RuntimePathDataRef entityGraph)
            {
                pathResult.Clear();

                while (cameFrom.ContainsKey(current))
                {
                    pathResult.Add(current);
                    current = cameFrom[current];
                }

                pathResult.Add(current);

                pathBuffer.Clear();

                for (int i = pathResult.Length - 2; i >= 0; i--)
                {
                    var pathIndex = pathResult[i];
                    entityGraph.TryGetValue(pathIndex, out var sourceNode, out var targetNode);

                    pathBuffer.Add(new FixedRouteNodeElement()
                    {
                        TrafficNodeEntity = sourceNode,
                        PathKey = pathIndex,
                        Position = roadGraph.GetStartPosition(pathIndex),
                    });

                    if (i == 0)
                    {
                        pathBuffer.Add(new FixedRouteNodeElement()
                        {
                            TrafficNodeEntity = targetNode,
                            PathKey = -1,
                            Position = roadGraph.GetEndPosition(pathIndex),
                        });
                    }
                }
            }
        }
    }
}