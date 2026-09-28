using Pathfinding;
using Spirit604.DotsCity.RuntimeRoad;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;

namespace Spirit604.DotsCity.ThirdParty.Astar
{
    public class AstarTrafficRuntimeGraphModifier : AstarEntityGraphRuntimeModifierBase, IRuntimeGraph
    {
        private List<TrafficNode> processNodes = new List<TrafficNode>();

        private Dictionary<TrafficNode, List<CustomEntityNode>> sceneTrafficNodeBindingRight = new Dictionary<TrafficNode, List<CustomEntityNode>>();
        private Dictionary<TrafficNode, List<CustomEntityNode>> sceneTrafficNodeBindingLeft = new Dictionary<TrafficNode, List<CustomEntityNode>>();

        public event Action OnGraphChanged = delegate { };

        protected override void AddSegment(RuntimeSegment segment)
        {
            var crossroadData = segment.crossroadData;

            AstarPath.active.AddWorkItem(() =>
            {
                foreach (var item in crossroadData)
                {
                    var nodes = item.Key.TrafficNodes;

                    for (int i = 0; i < nodes.Count; i++)
                    {
                        var node = nodes[i];

                        var mergeNode = runtimeRoadManager.TryToGetMergeNode(node);
                        var graph = PointGraph;

                        if (mergeNode != null)
                        {
                            sceneTrafficNodeBindingLeft.Add(node, sceneTrafficNodeBindingRight[mergeNode]);
                            sceneTrafficNodeBindingRight.Add(node, sceneTrafficNodeBindingLeft[mergeNode]);
                        }
                        else
                        {
                            sceneTrafficNodeBindingRight.Add(node, new List<CustomEntityNode>());
                            sceneTrafficNodeBindingLeft.Add(node, new List<CustomEntityNode>());

                            node.IterateAllLanes((laneIndex, external) =>
                            {
                                var entity = runtimeRoadManager.TryToGetEntity(node, !external, laneIndex);

                                var pos = node.GetLanePosition(laneIndex, external);

                                var graphNode = graph.AddNode(new CustomEntityNode(AstarPath.active)
                                {
                                    Entity = entity,

                                }, PosToInt(pos));

                                entityBinding.Add(entity, graphNode);
                                entityBindingInversed.Add(graphNode, entity);

                                if (!external)
                                {
                                    sceneTrafficNodeBindingRight[node].Add(graphNode);
                                }
                                else
                                {
                                    sceneTrafficNodeBindingLeft[node].Add(graphNode);
                                }

                            }, true, false);
                        }
                    }

                    for (int i = 0; i < nodes.Count; i++)
                    {
                        var node = nodes[i];

                        node.IterateAllPaths(path =>
                        {
                            var sourceLaneIndex = path.SourceLaneIndex;
                            var connectedLaneIndex = path.ConnectedLaneIndex;

                            var sourceNode = sceneTrafficNodeBindingRight[node][sourceLaneIndex];
                            var dstNode = sceneTrafficNodeBindingLeft[path.ConnectedTrafficNode][connectedLaneIndex];

                            PointNode.Connect(sourceNode, dstNode, (uint)path.PathLength, OffMeshLinks.Directionality.OneWay);
                        });
                    }
                }
            });

            AstarPath.active.FlushWorkItems();
            OnGraphChanged();
        }

        protected override void RemoveSegment(RuntimeSegment segment)
        {
            processNodes.Clear();
            var nodes = segment.pedestrianNodes;
            var crossroadData = segment.crossroadData;

            AstarPath.active.AddWorkItem(() =>
            {
                foreach (var item in crossroadData)
                {
                    var nodes = item.Key.TrafficNodes;

                    for (int i = 0; i < nodes.Count; i++)
                    {
                        var sourceNode = nodes[i];

                        var mergeNode = runtimeRoadManager.TryToGetMergeNode(sourceNode);

                        var graph = PointGraph;

                        processNodes.Add(sourceNode);

                        if (mergeNode == null)
                        {
                            sourceNode.IterateAllLanes((laneIndex, external) =>
                            {
                                CustomEntityNode graphNode = null;

                                if (!external)
                                {
                                    graphNode = sceneTrafficNodeBindingRight[sourceNode][laneIndex];
                                }
                                else
                                {
                                    graphNode = sceneTrafficNodeBindingLeft[sourceNode][laneIndex];
                                }

                                graph.RemoveNode(graphNode);
                                entityBinding.Remove(entityBindingInversed[graphNode]);
                                entityBindingInversed.Remove(graphNode);

                            }, true, false);
                        }
                        else
                        {
                            sourceNode.IterateAllPaths(path =>
                            {
                                if (path.ConnectedTrafficNode == sourceNode)
                                {
                                    var sourceGraphNode = sceneTrafficNodeBindingRight[sourceNode][path.ConnectedLaneIndex];
                                    var dstNode = sceneTrafficNodeBindingLeft[sourceNode][path.ConnectedLaneIndex];

                                    PointNode.Disconnect(sourceGraphNode, dstNode);
                                }
                            });
                        }
                    }
                }

                for (int i = 0; i < processNodes.Count; i++)
                {
                    sceneTrafficNodeBindingRight.Remove(processNodes[i]);
                    sceneTrafficNodeBindingLeft.Remove(processNodes[i]);
                }
            });

            AstarPath.active.FlushWorkItems();
            OnGraphChanged();
        }
    }
}
