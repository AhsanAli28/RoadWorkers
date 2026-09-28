using UnityEngine;
using Spirit604.DotsCity.Simulation.Pedestrian;

#if RUNTIME_ROAD
using Spirit604.CityEditor.Road;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.RuntimeRoad.Custom;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.Gameplay.Road;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
#endif

namespace Spirit604.DotsCity.RuntimeRoad
{
    [DefaultExecutionOrder(-10000)]
    public class RuntimeRoadManagerCustom : RuntimeRoadManagerBase
    {
#if RUNTIME_ROAD

        private const bool destroyEntityImmediate = true;

        private bool mergeNeighbour = true;

        private HashSet<PathData> tempPaths = new();
        private List<PathData> tempPaths2 = new();
        private List<PathData> tempPaths3 = new();
        private List<int> neighbours = new();
        private List<int> parallel = new();

        private Dictionary<TrafficNodeData, int> crosswalkBinding = new();

        private Dictionary<int, List<Entity>> crosswalkRight = new();
        private Dictionary<int, List<Entity>> crosswalkLeft = new();
        private Dictionary<RuntimeSegmentCustom, List<Entity>> lightBinding = new();

        // Item1 right node, Item2 left node
        private Dictionary<TrafficNodeData, (Entity, Entity)> crosswalkPedestrianBinding = new();

        private Dictionary<PedestrianNodeData, Entity> pedestrianBinding = new();
        private Dictionary<Entity, PedestrianNodeData> pedestrianBindingInversed = new();
        private Dictionary<TrafficNodeData, TrafficNodeData> reconnectNodes = new();
        private List<RuntimeSegmentCustom> allSegments = new();

        private int crosswalkIndex;
        private int pedestrianNodeIndex;
        private int crossroadIndex;

        public static RuntimeRoadManagerCustom Instance { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Instance = this;
            StartCoroutine(InitRoutine());
        }

        private void OnDestroy()
        {
            foreach (var segment in allSegments)
            {
                segment.Dispose();
            }
        }

        public void AddSegments(List<RuntimeSegmentCustom> segments)
        {
            if (segments.Count == 0)
                return;

            EntityManager.CompleteAllTrackedJobs();

            CreateLights(segments);
            InitialGraphCreation(segments);

            ConnectPaths(segments);
            ReconnectExistingNodes();

            SaveGraph();

            CreatePedestrianNodes(segments);
        }

        public void RemoveSegments(List<RuntimeSegmentCustom> segments)
        {
            if (segments.Count == 0) return;

            EntityManager.CompleteAllTrackedJobs();

            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            var destroyEntities = new NativeHashSet<Entity>(200, Allocator.Temp);
            var ignoreEntities = new NativeHashSet<Entity>(20, Allocator.Temp);
            var connectedEntities = new NativeList<Entity>(20, Allocator.Temp);

            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];

                var pedestrianNodes = segment.PedestrianNodes;

                ignoreEntities.Clear();

                for (int j = 0; j < pedestrianNodes.Count; j++)
                {
                    PedestrianNodeData pedestrianNode = pedestrianNodes[j];

                    var entity = pedestrianBinding[pedestrianNode];

                    if (ignoreEntities.Contains(entity))
                        continue;

                    if (!pedestrianNode.CrosswalkNode)
                    {
                        DestroyPedestrian(ref commandBuffer, entity, ref connectedEntities);
                        destroyEntities.Add(entity);
                    }
                    else
                    {
                        var connectedNode = GetConnectedTrafficNode(pedestrianNode);
                        var externalConnected = connectedNode.ConnectedExternalNode;

                        if (externalConnected != null && !segments.Contains(externalConnected.Owner))
                        {
                            if (mergeNeighbour)
                            {
                                if (segment.LightCrossroad)
                                {
                                    if (segment.TrafficLightEnabled)
                                    {
                                        SetLightEntity(entity, Entity.Null);
                                    }

                                    var opposite = GetOppositeCrosswalkEntity(entity, connectedNode);

                                    ignoreEntities.Add(opposite);

                                    DestroyPedestrian(ref commandBuffer, entity, ref connectedEntities);
                                    DestroyPedestrian(ref commandBuffer, opposite, ref connectedEntities);

                                    PedestrianNodeEntityUtils.RemoveConnection(EntityManager, entity, opposite);
                                }
                            }
                            else
                            {
                                destroyEntities.Add(entity);

                                var connectedEntity = GetExternalConnectedCrosswalkEntity(entity, connectedNode);

                                if (connectedEntity != Entity.Null)
                                {
                                    DestroyPedestrian(ref commandBuffer, entity, connectedEntity);
                                    PedestrianNodeEntityUtils.RemoveConnection(EntityManager, entity, connectedEntity);
                                }
                            }
                        }
                        else
                        {
                            DestroyPedestrian(ref commandBuffer, entity, ref connectedEntities);
                            destroyEntities.Add(entity);
                        }
                    }
                }

                segment.IterateAllNodes(node =>
                {
                    segment.IterateAllLanes(node, (lane, external) =>
                    {
                        bool destroyEntity = true;

                        var connectedNode = node.ConnectedExternalNode;

                        if (connectedNode != null)
                        {
                            if (mergeNeighbour)
                            {
                                if (!segments.Contains(connectedNode.Owner))
                                {
                                    destroyEntity = false;

                                    var nodeEntity = GetTrafficNode(node, lane, false);

                                    var buffer = EntityManager.GetBuffer<PathConnectionElement>(nodeEntity);

                                    for (int i = 0; i < buffer.Length; i++)
                                    {
                                        nodeResolver.RemovePath(buffer[i].GlobalPathIndex);
                                    }

                                    buffer.Clear();

                                    if (segment.LightCrossroad)
                                    {
                                        commandBuffer.SetComponent(nodeEntity, new TrafficNodeComponent()
                                        {
                                            CrossRoadIndex = connectedNode.Owner.CrossroadIndex
                                        });
                                    }

                                    connectedNode.ConnectedExternalNode = null;

#if UNITY_EDITOR
                                    connectedNode.ConnectedExternalSegment = null;
#endif
                                }
                            }
                            else
                            {
                                // External connected segment will not be destroyed so we should clean up paths connected to current destroyed segment.
                                if (!segments.Contains(connectedNode.Owner))
                                {
                                    var connectedByNodeEntity = GetTrafficNode(connectedNode, lane, true);

                                    if (connectedByNodeEntity != Entity.Null)
                                    {
                                        var buffer = EntityManager.GetBuffer<PathConnectionElement>(connectedByNodeEntity);

                                        for (int i = 0; i < buffer.Length; i++)
                                        {
                                            var removePathIndex = buffer[i].GlobalPathIndex;
                                            nodeResolver.RemovePath(removePathIndex);

                                            connectedNode.Owner.RemoveGlobalPath(removePathIndex);
                                            graph.RemovePath(removePathIndex);
                                            DestroyTrafficRecord(removePathIndex, ref destroyEntities);
                                        }

                                        buffer.Clear();
                                    }

                                    connectedNode.ConnectedExternalNode = null;

#if UNITY_EDITOR
                                    connectedNode.ConnectedExternalSegment = null;
#endif
                                }
                            }
                        }

                        if (destroyEntity)
                        {
                            var nodeEntity = GetTrafficNode(node, lane, external);

                            var buffer = EntityManager.GetBuffer<PathConnectionElement>(nodeEntity, true);

                            for (int i = 0; i < buffer.Length; i++)
                            {
                                nodeResolver.RemovePath(buffer[i].GlobalPathIndex);
                            }

                            TryToAddEntity(ref destroyEntities, nodeEntity);
                        }
                    });
                });

                if (segment.TakenPaths.IsCreated)
                {
                    DestroyTrafficRecord(segment.TakenPaths, ref destroyEntities);
                    graph.RemovePaths(segment.TakenPaths);
                }

                if (segment.TrafficLightEnabled)
                {
                    var lightEntities = lightBinding[segment];

                    for (int j = 0; j < lightEntities.Count; j++)
                    {
                        destroyEntities.Add(lightEntities[j]);
                    }

                    lightBinding.Remove(segment);
                }

                allSegments.Remove(segment);
            }

            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];

                segment.IterateAllNodes(node =>
                {
                    if (crosswalkPedestrianBinding.ContainsKey(node))
                    {
                        crosswalkPedestrianBinding.Remove(node);
                    }

                    var nodeEntity = GetTrafficNode(node, 0);

                    if (destroyEntities.Contains(nodeEntity))
                    {
                        RemoveCrosswalk(node.CrosswalkIndex);
                    }
                });

                if (segment.TakenPaths.IsCreated)
                {
                    UnselectPaths(ref commandBuffer, segment.TakenPaths, destroyEntities);
                    segment.Dispose();
                }
            }

            for (int i = 0; i < segments.Count; i++)
            {
                RuntimeSegmentCustom segment = segments[i];

                var pedestrianNodes = segment.PedestrianNodes;

                for (int j = 0; j < pedestrianNodes.Count; j++)
                {
                    PedestrianNodeData pedestrianNode = pedestrianNodes[j];

                    var entity = pedestrianBinding[pedestrianNode];

                    if (pedestrianBindingInversed.ContainsKey(entity))
                        pedestrianBindingInversed.Remove(entity);

                    pedestrianBinding.Remove(pedestrianNode);
                }
            }

            if (!destroyEntities.IsEmpty)
                commandBuffer.DestroyEntity(destroyEntities.ToNativeArray(Allocator.Temp));

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();

            connectedEntities.Dispose();
            ignoreEntities.Dispose();

            SaveGraph();
        }

        /// <summary>
        /// Reconnect paths from previously existing nodes to newly created paths.
        /// </summary>
        /// <param name="reconnectNodes">Value existing traffic node, Key newly created traffic node</param>
        private void ReconnectExistingNodes()
        {
            foreach (var item in reconnectNodes)
            {
                var existNode = item.Value;
                var newlyCreatedNode = item.Key;

                for (int laneIndex = 0; laneIndex < existNode.LaneCount; laneIndex++)
                {
                    tempPaths2.Clear();
                    tempPaths3.Clear();
                    existNode.GetAllPathsConnectedToNonAlloc(laneIndex, tempPaths2);
                    newlyCreatedNode.GetSourcePathsNonAlloc(laneIndex, tempPaths3);

                    for (int pathIndex1 = 0; pathIndex1 < tempPaths2.Count; pathIndex1++)
                    {
                        var sourcePath = tempPaths2[pathIndex1];

                        for (int pathIndex2 = 0; pathIndex2 < tempPaths3.Count; pathIndex2++)
                        {
                            var targetPath = tempPaths3[pathIndex2];

                            graph.AddConnectedPath(sourcePath.GlobalPathIndex, targetPath.GlobalPathIndex);
                            graph.AddConnectedByPath(targetPath.GlobalPathIndex, sourcePath.GlobalPathIndex);
                        }
                    }
                }
            }

            reconnectNodes.Clear();
        }

        private void CreatePedestrianNodes(List<RuntimeSegmentCustom> segments)
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                var nodes = segment.PedestrianNodes;

                for (int j = 0; j < nodes.Count; j++)
                {
                    CreatePedestrianNode(nodes[j], ref commandBuffer);
                }
            }

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();

            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                var nodes = segment.PedestrianNodes;

                for (int j = 0; j < nodes.Count; j++)
                {
                    ConnectPedestrianNode(nodes[j]);
                }
            }
        }

        private Entity TryToGetEntity(PedestrianNodeData pedestrianNodeData)
        {
            return pedestrianBinding[pedestrianNodeData];
        }

        private void ConnectPedestrianNode(PedestrianNodeData pedestrianNodeData)
        {
            var sourceEntity = TryToGetEntity(pedestrianNodeData);

            if (pedestrianNodeData.Previous != null)
            {
                var connectedEntity = TryToGetEntity(pedestrianNodeData.Previous);

                PedestrianNodeEntityUtils.AddConnectedEntity(EntityManager, sourceEntity, connectedEntity);
            }

            if (pedestrianNodeData.Next != null)
            {
                var connectedEntity = TryToGetEntity(pedestrianNodeData.Next);

                PedestrianNodeEntityUtils.AddConnectedEntity(EntityManager, sourceEntity, connectedEntity);
            }

            if (pedestrianNodeData.CrosswalkNode)
            {
                var connectedTrafficNode = GetConnectedTrafficNode(pedestrianNodeData);

                if (pedestrianNodeData.Owner.LightCrossroad)
                {
                    Entity connectedEntity = GetOppositeCrosswalkEntity(sourceEntity, connectedTrafficNode);

                    PedestrianNodeEntityUtils.AddConnectedEntity(EntityManager, sourceEntity, connectedEntity);
                }

                if (!mergeNeighbour)
                {
                    Entity externalCrosswalkEntity = GetExternalConnectedCrosswalkEntity(sourceEntity, connectedTrafficNode);

                    if (externalCrosswalkEntity != Entity.Null)
                        PedestrianNodeEntityUtils.AddConnectedEntity(EntityManager, sourceEntity, externalCrosswalkEntity);
                }
            }
        }

        private Entity GetExternalConnectedCrosswalkEntity(Entity sourceEntity, TrafficNodeData connectedTrafficNode)
        {
            var connectedExternalNode = connectedTrafficNode.ConnectedExternalNode;

            if (connectedExternalNode != null && crosswalkPedestrianBinding.ContainsKey(connectedExternalNode))
            {
                var pedestrianNode = pedestrianBindingInversed[sourceEntity];
                var isRight = connectedTrafficNode.IsRight(pedestrianNode.Position);

                var crosswalk = crosswalkPedestrianBinding[connectedExternalNode];

                // By default, Item1 is recorded for the right node, but it should be left for the connected crosswalk, so Item2 is taken
                return isRight ? crosswalk.Item2 : crosswalk.Item1;
            }

            return Entity.Null;
        }

        private Entity GetOppositeCrosswalkEntity(Entity sourceEntity, TrafficNodeData connectedTrafficNode)
        {
            var crosswalk = crosswalkPedestrianBinding[connectedTrafficNode];

            return crosswalk.Item1 != sourceEntity ? crosswalk.Item1 : crosswalk.Item2;
        }

        private void CreateLights(List<RuntimeSegmentCustom> segments)
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];

                if (!segment.LightCrossroad) continue;

                segment.CrossroadIndex = crossroadIndex++;

                if (!segment.TrafficLightEnabled) continue;

                lightBinding.Add(segment, new List<Entity>());
                segment.CrossroadID = TrafficLightCrossroad.TryToGenerateIDStatic();

                var container = segment.LightStateContainer;

                for (int lightIndex = 0; lightIndex < container.LightCount; lightIndex++)
                {
                    var states = container.GetStates(lightIndex);

                    var lightEntity = CreateLightEntity(ref commandBuffer, states, segment.CrossroadIndex, segment.CrossroadID, lightIndex);

                    lightBinding[segment].Add(lightEntity);
                }
            }

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }

        private void TryToAddEntity(ref NativeHashSet<Entity> destroyEntities, Entity entity)
        {
            if (!destroyEntities.Contains(entity))
            {
                destroyEntities.Add(entity);
            }
        }

        private void InitialGraphCreation(List<RuntimeSegmentCustom> segments)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                allSegments.Add(segment);

                segment.TakenPaths = graph.GetEmptyPaths(segment.AllPaths.Count, Allocator.Persistent);

                var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

                segment.IterateAllNodes(node =>
                {
                    segment.IterateAllLanes(node, (laneIndex, external) =>
                    {
                        CreateTrafficNode(ref commandBuffer, node, laneIndex, external, segments);
                    }, true);
                });

                commandBuffer.Playback(EntityManager);
                commandBuffer.Dispose();

                segment.IterateAllNodes(node =>
                {
                    segment.IterateAllNodePaths(node, path =>
                    {
                        int globalPathIndex = segment.GetPathIndex(path.LocalPathIndex);
                        path.GlobalPathIndex = globalPathIndex;
                        int sourceLaneIndex = path.ConnectedLaneIndex;
                        var sourceExternal = false;

                        if (!mergeNeighbour)
                        {
                            var sourceLaneData = node.GetClosestLane(path.StartPosition, out var position);

                            sourceLaneIndex = sourceLaneData.Item1;
                            sourceExternal = sourceLaneData.Item2;
                            path.External = sourceExternal;
                        }

                        int priority = 0;
                        bool customPriority = false;

                        if (!path.DefaultPriority)
                        {
                            customPriority = true;
                            priority = path.Priority;
                        }

                        var pathOptions = PathOptions.None;

                        var pathCurveType = PathCurveType.StraightLine;
                        var pathRoadType = PathRoadType.StraightRoad;

                        float pathLength = RoadUtils.GetLineLength(path.Waypoints);

                        if (segment.Intersection)
                        {
                            if (mergeNeighbour)
                            {
                                var sourceLaneData = node.GetClosestLane(path.StartPosition, out var position);
                                sourceLaneIndex = sourceLaneData.Item1;
                                path.StartPosition = position;
                            }

                            path.SourceLaneIndex = sourceLaneIndex;

                            segment.GetClosestLaneOnSegment(path.EndPosition, out var closestNode, out var connectedLane, out var external, out var connectedPosition);

                            path.ConnectedNodeIndex = segment.GetNodeIndex(closestNode);
                            path.ConnectedLaneIndex = connectedLane;

                            if (mergeNeighbour)
                            {
                                path.EndPosition = connectedPosition;
                            }

                            if (segment.LightCrossroad) // Intersection
                            {
                                pathOptions = PathOptions.EnterOfCrossroad;

                                var directionDot = Mathf.Abs(Vector3.Dot(Vector3.Normalize(path.EndPosition - path.StartPosition), -node.Forward));

                                var isTurn = directionDot < 0.8f;

                                if (isTurn)
                                {
                                    if (!customPriority)
                                        priority = -1;

                                    pathRoadType = PathRoadType.TurnRoad;
                                }
                            }
                            else if (segment.NodeCount == 1) // Dead end
                            {
                                pathRoadType = PathRoadType.TurnRoad;
                            }
                        }
                        else // Straight road
                        {
                            // If user is not explicitly assigned, then calculate indexes manually
                            if (path.SourceLaneIndex == -1 || path.ConnectedLaneIndex == -1 || path.ConnectedNodeIndex == -1)
                            {
                                segment.GetClosestLaneOnSegment(path.EndPosition, out var closestNode,
                                    out var connectedLane, out var external, out var connectedPosition);

                                path.ConnectedNodeIndex = segment.GetNodeIndex(closestNode);
                                path.ConnectedLaneIndex = connectedLane;
                                path.SourceLaneIndex = connectedLane;
                                sourceLaneIndex = connectedLane;
                            }
                        }

                        var pathData = new PathGraphSystem.PathData()
                        {
                            PathIndex = globalPathIndex,

                            ConnectedPathIndex = -1,
                            SourceLaneIndex = sourceLaneIndex,
                            PathLength = pathLength,
                            Priority = priority,
                            Options = pathOptions,
                            PathCurveType = pathCurveType,
                            PathRoadType = pathRoadType,
                            PathConnectionType = PathConnectionType.TrafficNode,
                            TrafficGroup = (TrafficGroupType)(~0)
                        };

                        graph.AddPath(pathData);

                        var nodeEntity = GetTrafficNode(node, sourceLaneIndex, sourceExternal);

                        var buffer = EntityManager.GetBuffer<PathConnectionElement>(nodeEntity);

                        buffer.Add(new PathConnectionElement()
                        {
                            GlobalPathIndex = globalPathIndex,
                            ConnectedSubHash = -1,
                            ConnectedHash = -1,
                        });

                        graph.InitRouteNodes(globalPathIndex, path.Waypoints.Count);

                        for (int i = 0; i < path.Waypoints.Count; i++)
                        {
                            graph.AddRouteNode(globalPathIndex, new RouteNodeData()
                            {
                                SpeedLimit = path.SpeedLimit,
                                ForwardNodeDirection = true,
                                Position = path.Waypoints[i],
                                TrafficGroup = (TrafficGroupType)(~0)
                            });
                        }
                    });
                });

                segment.IterateAllNodes(node =>
                {
                    segment.IterateAllNodePaths(node, path =>
                    {
                        var globalPathIndex = segment.GetPathIndex(path);

                        if (segment.StraightRoad)
                        {
                            var leftPath = path.LocalPathIndex - 1;
                            var rightPath = path.LocalPathIndex + 1;

                            parallel.Clear();

                            if (segment.HasLocalPathIndex(node, leftPath))
                            {
                                parallel.Add(segment.GetPathIndex(leftPath));
                            }

                            if (segment.HasLocalPathIndex(node, rightPath))
                            {
                                parallel.Add(segment.GetPathIndex(rightPath));
                            }

                            graph.InitParallelPaths(globalPathIndex, parallel.Count);

                            for (int i = 0; i < parallel.Count; i++)
                            {
                                graph.AddParallelPath(globalPathIndex, parallel[i]);
                            }
                        }
                        else
                        {
                            neighbours.Clear();

                            segment.IterateAllNodePaths(node, path2 =>
                            {
                                if (path != path2 && path.SourceLaneIndex == path2.SourceLaneIndex)
                                {
                                    neighbours.Add(segment.GetPathIndex(path2));
                                }
                            });

                            graph.InitNeighbourPaths(globalPathIndex, neighbours.Count);

                            for (int i = 0; i < neighbours.Count; i++)
                            {
                                graph.AddNeighbourPath(globalPathIndex, neighbours[i]);
                            }
                        }
                    });
                });

                FindIntersections(segment);
            }
        }

        private void FindIntersections(RuntimeSegmentCustom segment)
        {
            if (!segment.Intersection || segment.NodeCount < 3)
                return;

            tempPaths.Clear();

            for (int j = 0; j < segment.Nodes.Count; j++)
            {
                var node1 = segment.Nodes[j];

                segment.IterateAllNodePaths(node1, path1 =>
                {
                    for (int k = j + 1; k < segment.Nodes.Count; k++)
                    {
                        var node2 = segment.Nodes[k];

                        segment.IterateAllNodePaths(node2, path2 =>
                        {
                            var intersectPoint = PathHelper.GetPathIntersection(path1.Waypoints, path2.Waypoints, out var index1, out var index2);

                            if (intersectPoint != Vector3.zero)
                            {
                                var pathIndex1 = segment.GetPathIndex(path1);
                                var pathIndex2 = segment.GetPathIndex(path2);

                                graph.InitIntersection(pathIndex1, 1);
                                graph.InitIntersection(pathIndex2, 1);

                                if (!tempPaths.Contains(path1))
                                    tempPaths.Add(path1);

                                if (!tempPaths.Contains(path2))
                                    tempPaths.Add(path2);

                                graph.AddIntersection(pathIndex1,
                                    new IntersectPathInfo()
                                    {
                                        IntersectedPathIndex = pathIndex2,
                                        LocalNodeIndex = (byte)index1,
                                        IntersectPosition = intersectPoint
                                    });

                                graph.AddIntersection(pathIndex2,
                                    new IntersectPathInfo()
                                    {
                                        IntersectedPathIndex = pathIndex1,
                                        LocalNodeIndex = (byte)index2,
                                        IntersectPosition = intersectPoint
                                    });
                            }
                        });
                    }
                });
            }

            foreach (var path in tempPaths)
            {
                graph.SortIntersection(path.GlobalPathIndex);
            }
        }

        private void ConnectPaths(List<RuntimeSegmentCustom> segments)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];

                segment.IterateAllNodes(node =>
                {
                    segment.IterateAllNodePaths(node, path =>
                    {
                        if (mergeNeighbour)
                        {
                            var connectedNode = segment.GetNode(path.ConnectedNodeIndex);
                            var connectedExternalNode = connectedNode.ConnectedExternalNode;

                            if (connectedExternalNode != null)
                            {
                                ConnectPath(node, path, connectedExternalNode);
                            }
                            else
                            {
                                // Dead end, segment doesn't merge with other segment at connected node
                                ConnectPath(node, path, connectedNode, false);
                            }
                        }
                        else
                        {
                            if (path.External)
                            {
                                var connectedNode = segment.GetNode(path.ConnectedNodeIndex).ConnectedExternalNode;

                                ConnectPath(node, path, connectedNode);
                            }
                            else
                            {
                                var connectedNode = segment.GetNode(path.ConnectedNodeIndex);

                                ConnectPath(node, path, connectedNode);
                            }
                        }
                    });
                });
            }
        }

        private void ConnectPath(TrafficNodeData sourceNode, PathData path, TrafficNodeData connectedNode, bool externalMerge = true)
        {
            tempPaths2.Clear();

            if (mergeNeighbour)
            {
                if (externalMerge)
                    connectedNode.GetSourcePathsNonAlloc(path.ConnectedLaneIndex, tempPaths2);
            }
            else
            {
                connectedNode.GetSourcePathsNonAlloc(path.ConnectedLaneIndex, tempPaths2, !path.External);
            }

            Entity sourceNodeEntity;
            Entity connectedNodeEntity;

            if (mergeNeighbour)
            {
                sourceNodeEntity = GetTrafficNode(sourceNode, path.SourceLaneIndex, false);
                connectedNodeEntity = GetTrafficNode(connectedNode, path.ConnectedLaneIndex, !externalMerge);
            }
            else
            {
                if (sourceNode != connectedNode)
                {
                    // Inside segment connection
                    if (sourceNode.Owner == connectedNode.Owner)
                    {
                        sourceNodeEntity = GetTrafficNode(sourceNode, path.SourceLaneIndex, false);
                        connectedNodeEntity = GetTrafficNode(connectedNode, path.ConnectedLaneIndex, true);
                    }
                    else // Connection with other segment
                    {
                        sourceNodeEntity = GetTrafficNode(sourceNode, path.SourceLaneIndex, true);
                        connectedNodeEntity = GetTrafficNode(connectedNode, path.ConnectedLaneIndex, false);
                    }
                }
                else // U-turn
                {
                    sourceNodeEntity = GetTrafficNode(sourceNode, path.SourceLaneIndex, path.External);
                    connectedNodeEntity = GetTrafficNode(connectedNode, path.ConnectedLaneIndex, !path.External);
                }
            }

            AddConnection(path, sourceNodeEntity, connectedNodeEntity);
            nodeResolver.AddPath(path.GlobalPathIndex, sourceNodeEntity, connectedNodeEntity);

            for (int j = 0; j < tempPaths2.Count; j++)
            {
                var connectedPath = tempPaths2[j];

                graph.AddConnectedPath(path.GlobalPathIndex, connectedPath.GlobalPathIndex);
                graph.AddConnectedByPath(connectedPath.GlobalPathIndex, path.GlobalPathIndex);
            }
        }

        private void AddConnection(PathData path, Entity sourceNodeEntity, Entity connectedNodeEntity)
        {
            var buffer = EntityManager.GetBuffer<PathConnectionElement>(sourceNodeEntity);

            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].GlobalPathIndex == path.GlobalPathIndex)
                {
                    var bufferElement = buffer[i];
                    bufferElement.ConnectedNodeEntity = connectedNodeEntity;
                    bufferElement.ConnectedSubNodeEntity = connectedNodeEntity;
                    buffer[i] = bufferElement;
                    nodeResolver.AddPath(path.GlobalPathIndex, sourceNodeEntity, connectedNodeEntity);
                    break;
                }
            }
        }

        private Entity GetTrafficNode(TrafficNodeData node, int laneIndex, bool external = false)
        {
            external = !node.InverseCrosswalk ? external : !external;
            var data = !external || node.IsOneWay ? crosswalkRight : crosswalkLeft;

            if (data.ContainsKey(node.CrosswalkIndex) && data[node.CrosswalkIndex].Count > laneIndex)
                return data[node.CrosswalkIndex][laneIndex];
            else
                return Entity.Null;
        }

        private Entity CreateTrafficNode(ref EntityCommandBuffer commandBuffer, TrafficNodeData node, int laneIndex, bool external, List<RuntimeSegmentCustom> segments)
        {
            if (node.CrosswalkIndex == -1 && mergeNeighbour)
            {
                var connectedExternalNode = node.ConnectedExternalNode;

                if (connectedExternalNode != null && connectedExternalNode.CrosswalkIndex != -1)
                {
                    node.CrosswalkIndex = connectedExternalNode.CrosswalkIndex;
                    node.InverseCrosswalk = !connectedExternalNode.InverseCrosswalk;

                    crosswalkBinding.Add(node, node.CrosswalkIndex);
                }
            }

            var currentLightEntity = Entity.Null;

            var existEntity = GetTrafficNode(node, laneIndex, external);

            if (existEntity != Entity.Null)
            {
                if (!external)
                {
                    currentLightEntity = TryToGetLightEntity(node.Owner, node.LightIndex);
                }

                if (currentLightEntity != Entity.Null)
                {
                    commandBuffer.SetComponent(existEntity, new TrafficNodeComponent()
                    {
                        LightEntity = currentLightEntity,
                        CrossRoadIndex = node.Owner.CrossroadIndex,
                    });
                }

                if (mergeNeighbour)
                {
                    var connectedExternalNode = node.ConnectedExternalNode;

                    if (!reconnectNodes.ContainsKey(node) && !segments.Contains(connectedExternalNode.Owner))
                    {
                        reconnectNodes.Add(node, connectedExternalNode);
                    }
                }

                return existEntity;
            }

            var side = !external ? 1 : -1;
            var pos = node.GetLanePosition(laneIndex, external);
            var rot = node.GetNodeRotation(side);

            var entity = EntityManager.CreateEntity(
                typeof(TrafficNodeComponent),
                typeof(TrafficNodeAvailableComponent),
                typeof(TrafficNodeSettingsComponent),
                typeof(TrafficNodeCapacityComponent),
                typeof(TrafficNodeAvailableTag),
                typeof(LocalToWorld),
                typeof(LocalTransform),
                typeof(PathConnectionElement));

            commandBuffer.AddComponent(entity, CullComponentsExtension.GetComponentSet());

            if (!external)
            {
                currentLightEntity = TryToGetLightEntity(node.Owner, node.LightIndex);
            }

            commandBuffer.SetComponent(entity, new TrafficNodeComponent()
            {
                LightEntity = currentLightEntity,
                CrossRoadIndex = node.Owner.CrossroadIndex,
            });

            commandBuffer.SetComponent(entity, new TrafficNodeSettingsComponent()
            {
                TrafficNodeType = TrafficNodeType.Default,
                LaneDirectionSide = side,
                LaneIndex = laneIndex,
                ChanceToSpawn = 1f,
                Weight = 1f,
                CustomAchieveDistance = 0,
                HasCrosswalk = node.AddCrosswalk,
                AllowedRouteRandomizeSpawning = node.Owner.StraightRoad,
                IsAvailableForSpawn = true,
                IsAvailableForSpawnTarget = true,
            });

            commandBuffer.SetComponent(entity, new TrafficNodeCapacityComponent()
            {
                Capacity = -1
            });

            commandBuffer.SetComponent(entity, LocalTransform.FromPositionRotation(pos, rot));

            commandBuffer.AddBuffer<PathConnectionElement>(entity);

            if (!crosswalkBinding.ContainsKey(node))
            {
                var index = crosswalkIndex++;
                node.CrosswalkIndex = index;

                crosswalkBinding.Add(node, index);
            }

            var data = !external || node.IsOneWay ? crosswalkRight : crosswalkLeft;

            if (!data.ContainsKey(node.CrosswalkIndex))
            {
                data.Add(node.CrosswalkIndex, new List<Entity>());
            }

            data[node.CrosswalkIndex].Add(entity);

            return entity;
        }

        private TrafficNodeData GetConnectedTrafficNode(PedestrianNodeData pedestrianNode)
        {
            return pedestrianNode.Owner.GetConnectedNode(pedestrianNode);
        }

        private Entity CreatePedestrianNode(PedestrianNodeData pedestrianNode, ref EntityCommandBuffer commandBuffer)
        {
            pedestrianNode.RuntimeID = pedestrianNodeIndex++;

            TrafficNodeData connectedNode = null;

            if (pedestrianNode.CrosswalkNode)
            {
                connectedNode = GetConnectedTrafficNode(pedestrianNode);

                if (connectedNode != null && mergeNeighbour)
                {
                    var externalNode = connectedNode.ConnectedExternalNode;

                    if (externalNode != null && crosswalkPedestrianBinding.ContainsKey(externalNode))
                    {
                        if (!crosswalkPedestrianBinding.ContainsKey(connectedNode))
                        {
                            var crosswalkData = crosswalkPedestrianBinding[externalNode];
                            var temp = crosswalkData.Item2;

                            crosswalkData.Item2 = crosswalkData.Item1;
                            crosswalkData.Item1 = temp;

                            crosswalkPedestrianBinding.Add(connectedNode, crosswalkData);
                        }

                        var isRight = connectedNode.IsRight(pedestrianNode.Position);

                        var nodeEntity = isRight ? crosswalkPedestrianBinding[connectedNode].Item1 : crosswalkPedestrianBinding[connectedNode].Item2;

                        if (pedestrianNode.Owner.TrafficLightEnabled)
                        {
                            int lightIndex = GetLightIndex(pedestrianNode, connectedNode);

                            var localLightEntity = TryToGetLightEntity(pedestrianNode.Owner, lightIndex);
                            SetLightEntity(nodeEntity, localLightEntity);
                        }

                        pedestrianBinding.Add(pedestrianNode, nodeEntity);
                        return nodeEntity;
                    }
                }
            }

            var entity = EntityManager.CreateEntity(
                typeof(NodeCapacityComponent),
                typeof(NodeLightSettingsComponent),
                typeof(NodeSettingsComponent),
                typeof(NodeConnectionDataElement),
                typeof(LocalTransform),
                typeof(LocalToWorld));

            commandBuffer.AddComponent(entity, CullComponentsExtension.GetComponentSet());

            //ProcessAdditionalSettings(pedestrianNode, entity);

            commandBuffer.SetComponent(entity, new NodeCapacityComponent { MaxAvailableCount = -1, CurrentCount = -1 });

            var connectionBuffer = commandBuffer.AddBuffer<NodeConnectionDataElement>(entity);

            var nodeSettingsComponent = new NodeSettingsComponent()
            {
                NodeType = pedestrianNode.PedestrianNodeType,
                NodeShapeType = pedestrianNode.NodeShapeType,
                Weight = pedestrianNode.Weight,
                CustomAchieveDistance = pedestrianNode.CustomAchieveDistance,
                CanSpawnInVision = pedestrianNode.CanSpawnInView ? 1 : 0,
                ChanceToSpawn = pedestrianNode.ChanceToSpawn,
                MaxPathWidth = pedestrianNode.Width,
                Height = pedestrianNode.Height,
                HasMovementRandomOffset = pedestrianNode.HasMovementRandomOffset ? 1 : 0,
                SumWeight = 0
            };

            EntityManager.SetComponentData(entity, nodeSettingsComponent);

            if (pedestrianNode.CanSpawnInView)
            {
                commandBuffer.AddComponent<NodeCanSpawnInVisionTag>(entity);
            }

            var rotation = Quaternion.identity;
            var crosswalkIndex = -1;
            var lightEntity = Entity.Null;

            if (connectedNode != null)
            {
                int lightIndex = GetLightIndex(pedestrianNode, connectedNode);

                lightEntity = TryToGetLightEntity(pedestrianNode.Owner, lightIndex);
                crosswalkIndex = crosswalkBinding[connectedNode];
                rotation = connectedNode.Rotation;
            }

            commandBuffer.SetComponent(entity, LocalTransform.FromPositionRotation(pedestrianNode.Position, rotation));

            EntityManager.SetComponentData(entity, new NodeLightSettingsComponent()
            {
                HasCrosswalk = true,
                LightEntity = lightEntity,
                CrosswalkIndex = crosswalkIndex,
            });

            if (connectedNode != null)
            {
                if (!crosswalkPedestrianBinding.ContainsKey(connectedNode))
                {
                    crosswalkPedestrianBinding.Add(connectedNode, default);
                }

                var data = crosswalkPedestrianBinding[connectedNode];

                if (connectedNode.IsRight(pedestrianNode.Position))
                {
                    data.Item1 = entity;
                }
                else
                {
                    data.Item2 = entity;
                }

                crosswalkPedestrianBinding[connectedNode] = data;

                if (!mergeNeighbour)
                {
                    pedestrianBindingInversed.Add(entity, pedestrianNode);
                }
            }
            else
            {
                pedestrianBindingInversed.Add(entity, pedestrianNode);
            }

            pedestrianBinding.Add(pedestrianNode, entity);

            return entity;
        }

        private void SetLightEntity(Entity nodeEntity, Entity localLightEntity)
        {
            var data = EntityManager.GetComponentData<NodeLightSettingsComponent>(nodeEntity);

            data.LightEntity = localLightEntity;

            EntityManager.SetComponentData(nodeEntity, data);
        }

        private static int GetLightIndex(PedestrianNodeData pedestrianNode, TrafficNodeData connectedNode)
        {
            var lightIndex = pedestrianNode.LightIndex;

            if (lightIndex == -1)
                lightIndex = (connectedNode.LightIndex + 1) % 2;

            return lightIndex;
        }

        private void RemoveCrosswalk(int crosswalkIndex)
        {
            if (crosswalkRight.ContainsKey(crosswalkIndex))
                crosswalkRight.Remove(crosswalkIndex);

            if (crosswalkLeft.ContainsKey(crosswalkIndex))
                crosswalkLeft.Remove(crosswalkIndex);
        }

        private Entity TryToGetLightEntity(RuntimeSegmentCustom segment, int lightIndex)
        {
            if (lightBinding.TryGetValue(segment, out var lightEntities))
            {
                if (lightEntities.Count > lightIndex)
                {
                    return lightEntities[lightIndex];
                }
            }

            return Entity.Null;
        }

#endif
    }
}
