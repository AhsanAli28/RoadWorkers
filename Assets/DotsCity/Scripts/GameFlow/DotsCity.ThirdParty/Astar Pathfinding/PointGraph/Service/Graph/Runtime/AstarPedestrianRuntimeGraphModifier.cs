using Pathfinding;
using Spirit604.DotsCity.RuntimeRoad;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;

namespace Spirit604.DotsCity.ThirdParty.Astar
{
    public class AstarPedestrianRuntimeGraphModifier : AstarEntityGraphRuntimeModifierBase, IRuntimeGraph
    {
        private Dictionary<PedestrianNode, CustomEntityNode> sceneNodeBinding = new Dictionary<PedestrianNode, CustomEntityNode>();
        private List<PedestrianNode> processNodes = new List<PedestrianNode>();

        public event Action OnGraphChanged = delegate { };

        protected override void AddSegment(RuntimeSegment segment)
        {
            var nodes = segment.pedestrianNodes;

            AstarPath.active.AddWorkItem(() =>
            {
                for (int i = 0; i < nodes.Length; i++)
                {
                    PedestrianNode node = nodes[i];

                    var entity = runtimeRoadManager.TryToGetEntity(node);

                    var graph = PointGraph;

                    CustomEntityNode graphNode = null;

                    if (entityBinding.ContainsKey(entity))
                    {
                        graphNode = entityBinding[entity];
                        sceneNodeBinding.Add(node, graphNode);
                        continue;
                    }

                    graphNode = graph.AddNode(new CustomEntityNode(AstarPath.active)
                    {
                        Entity = entity,

                    }, PosToInt(node.transform.position));

                    entityBinding.Add(entity, graphNode);
                    entityBindingInversed.Add(graphNode, entity);

                    sceneNodeBinding.Add(node, graphNode);
                }

                for (int i = 0; i < nodes.Length; i++)
                {
                    PedestrianNode node = nodes[i];
                    var sourceGraphNode = sceneNodeBinding[node];

                    node.IterateConnectedNodes(connectedNode =>
                    {
                        var connectedGraphNode = sceneNodeBinding[connectedNode];

                        var directionality = OffMeshLinks.Directionality.TwoWay;

                        PointNode.Connect(sourceGraphNode, connectedGraphNode, GetCost(node.transform.position, connectedNode.transform.position), directionality);
                    });
                }
            });

            AstarPath.active.FlushWorkItems();
            OnGraphChanged();
        }

        protected override void RemoveSegment(RuntimeSegment segment)
        {
            processNodes.Clear();
            var nodes = segment.pedestrianNodes;

            AstarPath.active.AddWorkItem(() =>
            {
                for (int i = 0; i < nodes.Length; i++)
                {
                    PedestrianNode sourceNode = nodes[i];

                    var mergeNode = runtimeRoadManager.TryToGetMergeNode(sourceNode);

                    var graph = PointGraph;

                    var graphNode = sceneNodeBinding[sourceNode];

                    processNodes.Add(sourceNode);

                    if (mergeNode == null)
                    {
                        graph.RemoveNode(graphNode);
                        entityBinding.Remove(entityBindingInversed[graphNode]);
                        entityBindingInversed.Remove(graphNode);
                    }
                    else
                    {
                        var currentIndex = mergeNode.GetCrosswalk().GetCrosswalkNodeIndex(mergeNode);
                        var oppositeIndex = (currentIndex + 1) % 2;
                        var opposite = mergeNode.GetCrosswalk().GetCrosswalkNode(oppositeIndex);

                        sceneNodeBinding.TryGetValue(opposite, out var oppositeNodeData);

                        var connections = graphNode.connections;

                        for (int j = 0; j < connections.Length; j++)
                        {
                            var connectedCustomEntityNode = connections[j].node as CustomEntityNode;

                            bool disconnect = false;

                            if (entityBindingInversed.TryGetValue(connectedCustomEntityNode, out var connectedNodeEntity))
                            {
                                var connectedNodeData = entityBinding[connectedNodeEntity];

                                if (oppositeNodeData != null && connectedNodeData == oppositeNodeData)
                                {
                                    disconnect = !mergeNode.HasDoubleConnection(opposite);
                                }
                            }

                            if (disconnect)
                                PointNode.Disconnect(graphNode, connectedCustomEntityNode);
                        }
                    }
                }

                for (int i = 0; i < processNodes.Count; i++)
                {
                    sceneNodeBinding.Remove(processNodes[i]);
                }
            });

            AstarPath.active.FlushWorkItems();
            OnGraphChanged();
        }
    }
}
