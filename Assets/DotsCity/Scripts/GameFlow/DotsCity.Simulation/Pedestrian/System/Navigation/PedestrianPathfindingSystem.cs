using Spirit604.DotsCity.Simulation.Navigation;
using Spirit604.Extensions;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(SimulationGroup))]
    [BurstCompile]
    [DisableAutoCreation]
    [DisableAutoTypeRegistration]
    public partial struct PedestrianPathfindingSystem : ISystem, ISystemStartStop
    {
        public struct PathRequest : IComponentData, IEnableableComponent
        {
            public Entity StartNode;
            public Entity TargetNode;
            public int Index;
        }

        private EntityQuery _pathRequestQuery;
        private EntityQuery _pathNodeQuery;

        private struct NativePriorityQueueContainer
        {
            public NativePriorityQueue<Entity> Container;
        }

        private struct CameFromContainer
        {
            public NativeHashMap<Entity, Entity> Container;
        }

        private struct ScoreContainerContainer
        {
            public NativeHashMap<Entity, float> Container;
        }

        private struct PathContainer
        {
            public NativeList<Entity> Container;
        }

        private NativeArray<NativePriorityQueueContainer> openSet;
        private NativeArray<CameFromContainer> cameFrom;
        private NativeArray<ScoreContainerContainer> fScore;
        private NativeArray<ScoreContainerContainer> gScore;
        private NativeArray<PathContainer> pathResult;

        public void OnCreate(ref SystemState state)
        {
            _pathNodeQuery =
                SystemAPI.QueryBuilder()
                .WithAll<NodeSettingsComponent>()
                .Build();

            state.RequireForUpdate<PedestrianPathAgentConfigReference>();
        }

        public void OnStartRunning(ref SystemState state)
        {
            if (cameFrom.IsCreated) return;

            var config = SystemAPI.GetSingleton<PedestrianPathAgentConfigReference>();
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
                    Container = new NativePriorityQueue<Entity>(bufferSize, Allocator.Persistent)
                };

                cameFrom[i] = new CameFromContainer()
                {
                    Container = new NativeHashMap<Entity, Entity>(bufferSize, Allocator.Persistent)
                };

                fScore[i] = new ScoreContainerContainer()
                {
                    Container = new NativeHashMap<Entity, float>(bufferSize, Allocator.Persistent)
                };

                gScore[i] = new ScoreContainerContainer()
                {
                    Container = new NativeHashMap<Entity, float>(bufferSize, Allocator.Persistent)
                };

                pathResult[i] = new PathContainer()
                {
                    Container = new NativeList<Entity>(bufferSize, Allocator.Persistent)
                };
            }

            _pathRequestQuery =
                SystemAPI.QueryBuilder()
                .WithAllRW<PedestrianPathfindingSystem.PathRequest, DestinationComponent>()
                .WithAllRW<NodeSequenceElement>()
                .Build();

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
            var nodeEntities = _pathNodeQuery.ToEntityListAsync(Allocator.TempJob, state.Dependency, out var dep);

            var job = new PathfindingJob
            {
                NodeEntities = nodeEntities,
                EntityHandle = SystemAPI.GetEntityTypeHandle(),
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                NodeConnectionDataElementLookup = SystemAPI.GetBufferLookup<NodeConnectionDataElement>(true),
                PathRequestHandle = SystemAPI.GetComponentTypeHandle<PathRequest>(false),
                PathAgentComponentHandle = SystemAPI.GetComponentTypeHandle<PathAgentComponent>(false),
                PathAgentUpdateStateHandle = SystemAPI.GetComponentTypeHandle<PathAgentUpdateState>(false),
                DestinationComponentHandle = SystemAPI.GetComponentTypeHandle<DestinationComponent>(false),
                NodeSequenceElementHandle = SystemAPI.GetBufferTypeHandle<NodeSequenceElement>(false),
                OpenSetContainer = openSet,
                CameFromContainer = cameFrom,
                FScoreContainer = fScore,
                GScoreContainer = gScore,
                PathResultContainer = pathResult,
            };

            state.Dependency = job.ScheduleParallel(_pathRequestQuery, dep);

            nodeEntities.Dispose(state.Dependency);
        }

        [BurstCompile]
        private struct PathfindingJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle EntityHandle;

            [NativeDisableContainerSafetyRestriction]
            [ReadOnly] public NativeList<Entity> NodeEntities;

            [NativeDisableContainerSafetyRestriction]
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;

            [NativeDisableContainerSafetyRestriction]
            [ReadOnly] public BufferLookup<NodeConnectionDataElement> NodeConnectionDataElementLookup;

            public ComponentTypeHandle<PathRequest> PathRequestHandle;

            public ComponentTypeHandle<PathAgentComponent> PathAgentComponentHandle;

            public ComponentTypeHandle<PathAgentUpdateState> PathAgentUpdateStateHandle;

            [NativeDisableContainerSafetyRestriction]
            public ComponentTypeHandle<DestinationComponent> DestinationComponentHandle;

            public BufferTypeHandle<NodeSequenceElement> NodeSequenceElementHandle;

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
                var destinationComponents = chunk.GetNativeArray(ref DestinationComponentHandle);
                var routeNodes = chunk.GetBufferAccessor(ref NodeSequenceElementHandle);

                var count = NodeEntities.Length;

                for (int index = 0; index < chunk.Count; index++)
                {
                    var agentEntity = entities[index];
                    var request = pathRequests[index];
                    var pathBuffer = routeNodes[index];
                    var destinationComponent = destinationComponents[index];

                    if (useEnabledMask && !chunk.IsComponentEnabled(ref PathRequestHandle, index))
                        continue;

                    chunk.SetComponentEnabled(ref PathRequestHandle, index, false);
                    chunk.SetComponentEnabled(ref PathAgentUpdateStateHandle, index, true);

                    var startNode = request.StartNode;
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
                        gScore[NodeEntities[i]] = float.MaxValue;
                        fScore[NodeEntities[i]] = float.MaxValue;
                    }

                    gScore[startNode] = 0;
                    fScore[startNode] = HeuristicCostEstimate(startNode, goalPos, in LocalTransformLookup);

                    openSet.Enqueue(startNode, fScore[startNode]);

                    bool found = false;

                    // A* algorithm
                    while (!openSet.IsEmpty)
                    {
                        var current = openSet.Dequeue();

                        if (current == endNode)
                        {
                            ReconstructPath(in cameFrom, agentEntity, current, in LocalTransformLookup, ref pathResult, ref pathBuffer, ref destinationComponent);
                            found = true;
                            break;
                        }

                        // Process connected
                        var connectedPaths = NodeConnectionDataElementLookup[current];

                        var currentSourcePos = LocalTransformLookup[current].Position;

                        for (int j = 0; j < connectedPaths.Length; j++)
                        {
                            var connectedEntity = connectedPaths[j].ConnectedEntity;

                            var connectedPos = LocalTransformLookup[connectedEntity].Position;

                            float tentativeGScore = gScore[current] + math.distance(currentSourcePos, connectedPos);

                            if (tentativeGScore < gScore[connectedEntity])
                            {
                                cameFrom[connectedEntity] = current;
                                gScore[connectedEntity] = tentativeGScore;
                                fScore[connectedEntity] = gScore[connectedEntity] + HeuristicCostEstimate(connectedEntity, goalPos, in LocalTransformLookup);

                                if (!openSet.Contains(connectedEntity))
                                {
                                    openSet.Enqueue(connectedEntity, fScore[connectedEntity]);
                                }
                                else
                                {
                                    openSet.UpdateFScore(connectedEntity, fScore[connectedEntity]);
                                }
                            }
                        }
                    }

                    if (!found)
                    {
                        pathBuffer.Clear();

                        //#if UNITY_EDITOR
                        // UnityEngine.Debug.Log($"PedestrianPathfindingSystem. Agent {agentEntity.Index} Path not found Source {request.StartNode} Target {request.TargetNode}");
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

                        destinationComponents[index] = destinationComponent;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static float HeuristicCostEstimate(Entity start, float3 goalPos, in ComponentLookup<LocalTransform> localTransformLookup)
            {
                var startPos = localTransformLookup[start].Position;

                return math.distance(startPos, goalPos);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void ReconstructPath(
                in NativeHashMap<Entity, Entity> cameFrom,
                Entity agentEntity,
                Entity current,
                in ComponentLookup<LocalTransform> transformLookup,
                ref NativeList<Entity> pathResult,
                ref DynamicBuffer<NodeSequenceElement> pathBuffer,
                ref DestinationComponent destinationComponent)
            {
                pathResult.Clear();

                while (cameFrom.ContainsKey(current))
                {
                    pathResult.Add(current);
                    current = cameFrom[current];
                }

                pathResult.Add(current);

                int startIndex = pathResult.Length - 1;

                if (pathResult.Length > 1)
                {
                    var sourcePos = transformLookup[agentEntity].Position;
                    var firstPoint = transformLookup[pathResult[startIndex]].Position;
                    var secondPoint = transformLookup[pathResult[startIndex - 1]].Position;

                    if (math.distancesq(firstPoint, sourcePos) > 0.25f)
                    {
                        var dir1 = math.normalizesafe(firstPoint - sourcePos);
                        var dir2 = math.normalizesafe(secondPoint - firstPoint);

                        var dot = math.dot(dir1, dir2);

                        // Target node is behind
                        if (dot < -0.2f)
                        {
                            startIndex--;
                            destinationComponent.PreviousDestination = destinationComponent.Value;
                            destinationComponent.PreviuosDestinationNode = destinationComponent.DestinationNode;
                            destinationComponent.PreviousLightEntity = destinationComponent.DestinationLightEntity;

                            destinationComponent.Value = transformLookup[pathResult[startIndex]].Position;
                            destinationComponent.DestinationNode = pathResult[startIndex];
                        }
                    }
                }

                pathBuffer.Clear();

                for (int i = startIndex; i >= 0; i--)
                {
                    var node = pathResult[i];

                    pathBuffer.Add(new NodeSequenceElement()
                    {
                        Entity = node,
                    });
                }
            }
        }
    }
}