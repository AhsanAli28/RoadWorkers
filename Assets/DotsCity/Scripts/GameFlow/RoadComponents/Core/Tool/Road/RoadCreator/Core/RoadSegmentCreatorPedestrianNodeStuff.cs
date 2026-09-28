using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public partial class RoadSegmentCreator : MonoBehaviour
    {
        public void AddCornerNodes(List<Vector3> points, bool canSpawnAtNodes = false)
        {
            if (points == null || points.Count == 0) return;

            PedestrianNode previousNode = null;

            for (int i = 0; i < points.Count; i++)
            {
                Vector3 point = points[i];
                var node = CreateCornerNode(point);

                if (!canSpawnAtNodes)
                {
                    node.ChanceToSpawn = 0;
                    EditorSaver.SetObjectDirty(node);
                }

                if (previousNode)
                {
                    node.AddConnection(previousNode);
                }

                previousNode = node;
            }

            ReconnectCornerNodes();
        }

        private void InitializeTrafficNodeCrosswalk(TrafficNode trafficNode)
        {
            TrafficNodeCrosswalk trafficNodeCrosswalk = trafficNode.TrafficNodeCrosswalk;

            trafficNodeCrosswalk.CrossWalkOffset = CrossWalkOffset;

            var trafficLightCrossroad = trafficNode.TrafficLightCrossroad;

            var node1 = trafficNodeCrosswalk.GetCrosswalkNode(0);
            var node2 = trafficNodeCrosswalk.GetCrosswalkNode(1);

            if (trafficLightCrossroad)
            {
                var currentTrafficLightHandler = trafficNode.TrafficLightHandler;

                TrafficLightHandler relatedTrafficLightHandler = null;

                foreach (var item in trafficLightCrossroad.TrafficLightHandlers)
                {
                    if (item.Value != currentTrafficLightHandler)
                    {
                        relatedTrafficLightHandler = item.Value;
                        break;
                    }
                }

                node1.RelatedTrafficLightHandler = relatedTrafficLightHandler;
                node2.RelatedTrafficLightHandler = relatedTrafficLightHandler;
            }

            trafficNodeCrosswalk.SetType(crossWalkNodeShape);

            EditorSaver.SetObjectDirty(node1);
            EditorSaver.SetObjectDirty(node2);
        }

        private void ClearCornerNodes()
        {
            cornerPedestrianNodes.DestroyGameObjects();
            cornerPedestrianNodesBinding.Clear();

            int optionalNode = 1;

            if (CreateTrafficNodeCount == 3)
            {
                optionalNode = 0;
            }

            for (int i = 0; i < CreateTrafficNodeCount - 1 + optionalNode; i++)
            {
                int nextIndex = (i + 1) % CreateTrafficNodeCount;

                if (createdTrafficNodes.Count > i && createdTrafficNodes.Count > nextIndex)
                {
                    if (!createdTrafficNodes[i] || !createdTrafficNodes[nextIndex])
                    {
                        continue;
                    }

                    var crossWalkNode1 = createdTrafficNodes[i].GetCrosswalkNode(1);
                    var crossWalkNode2 = createdTrafficNodes[nextIndex].GetCrosswalkNode(0);

                    crossWalkNode1.TryToRemoveNode(crossWalkNode2);
                    crossWalkNode2.TryToRemoveNode(crossWalkNode1);
                }
            }
        }

        private void CreateCornerNodes()
        {
            int optionalNode = 1;

            if (CreateTrafficNodeCount == 3)
            {
                optionalNode = 0;
            }

            for (int i = 0; i < CreateTrafficNodeCount - 1 + optionalNode; i++)
            {
                int nextIndex = (i + 1) % CreateTrafficNodeCount;
                var crossWalkNode1 = createdTrafficNodes[i].GetCrosswalkNode(1);
                var crossWalkNode2 = createdTrafficNodes[nextIndex].GetCrosswalkNode(0);

                switch (pedestrianCornerConnectionType)
                {
                    case PedestrianCornerConnectionType.Disabled:
                        break;
                    case PedestrianCornerConnectionType.Corner:
                        {
                            Vector3 spawnPosition = GetCornerPointRelativeCenter(crossWalkNode1.transform.position, crossWalkNode2.transform.position);

                            PedestrianNode cornerPedestrianNode = CreateCornerNode(spawnPosition);

                            var dir = (cornerPedestrianNode.transform.position - transform.position).normalized;

                            cornerPedestrianNode.transform.position += MathUtilMethods.LookRotationSafe(dir, Vector3.up) * cornerOffset;

                            cornerPedestrianNode.AddNode(crossWalkNode1);
                            cornerPedestrianNode.AddNode(crossWalkNode2);

                            crossWalkNode1.AddNode(cornerPedestrianNode);
                            crossWalkNode2.AddNode(cornerPedestrianNode);

                            break;
                        }
                    case PedestrianCornerConnectionType.Straight:
                        {
                            crossWalkNode1.AddNode(crossWalkNode2);
                            crossWalkNode2.AddNode(crossWalkNode1);
                            break;
                        }
                }
            }
        }

        private PedestrianNode CreateCornerNode(Vector3 spawnPosition)
        {
            var cornerPedestrianNode = CreatePedestrianNode();
            cornerPedestrianNode.transform.SetParent(cornerNodes);
            cornerPedestrianNode.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
            cornerPedestrianNode.MaxPathWidth = pedestrianRouteWidth;

            cornerPedestrianNodes.TryToAdd(cornerPedestrianNode);

            return cornerPedestrianNode;
        }

        private void ReconnectCornerNodes()
        {
            var crosswalkNodes = GetAllCrosswalkNodes();

            IterateAllNodes(node =>
            {
                var crosswalk = node.TrafficNodeCrosswalk;

                if (crosswalk.Enabled)
                {
                    var pedNode1 = crosswalk.GetCrosswalkNode(0);
                    var pedNode2 = crosswalk.GetCrosswalkNode(1);
                    ReconnectClosestNode(pedNode1, crosswalkNodes);
                    ReconnectClosestNode(pedNode2, crosswalkNodes);
                }
            });
        }

        private void ReconnectClosestNode(PedestrianNode node, List<PedestrianNode> crosswalkNodes)
        {
            var removeNodes = new List<PedestrianNode>();

            node.IterateConnectedNodes(connectedNode =>
            {
                if (node.ConnectedTrafficNode != connectedNode.ConnectedTrafficNode)
                {
                    removeNodes.Add(connectedNode);
                }
            });

            foreach (var removeNode in removeNodes)
            {
                node.RemoveConnection(removeNode);
            }

            PedestrianNode closestCornerNode = GetClosestNode(node, cornerPedestrianNodes);
            PedestrianNode closestCrosswalkNode = GetClosestNode(node, crosswalkNodes);

            var closestNode = VectorExtensions.GetClosestTransform(node.transform.position, closestCornerNode, closestCrosswalkNode);

            if (closestNode != closestCrosswalkNode)
            {
                node.AddConnection(closestNode);
            }
            else
            {
                if (closestNode.ConnectedTrafficNode != node.ConnectedTrafficNode)
                    node.AddConnection(closestNode);
            }
        }

        private PedestrianNode GetClosestNode(PedestrianNode node, List<PedestrianNode> nodes)
        {
            var collection = nodes.OrderBy(a => Vector3.SqrMagnitude(a.transform.position - node.transform.position));

            var element = collection.ElementAt(0);

            if (element != node)
                return element;

            if (collection.Count() > 1)
            {
                return collection.ElementAt(1);
            }

            return null;
        }

        private List<PedestrianNode> GetAllCrosswalkNodes()
        {
            var list = new List<PedestrianNode>();

            IterateAllNodes(node =>
            {
                var crosswalk = node.TrafficNodeCrosswalk;

                if (crosswalk.Enabled)
                {
                    var pedNode1 = crosswalk.GetCrosswalkNode(0);
                    var pedNode2 = crosswalk.GetCrosswalkNode(1);

                    list.Add(pedNode1);
                    list.Add(pedNode2);
                }
            });

            return list;
        }

        private T GetClosestNode<T>(Vector3 sourcePos, T node1, T node2) where T : MonoBehaviour
        {
            var dist1 = Vector3.SqrMagnitude(sourcePos - node1.transform.position);
            var dist2 = Vector3.SqrMagnitude(sourcePos - node2.transform.position);

            return dist1 < dist2 ? node1 : node2;
        }

        private void TryToCreatePedestrianLoopedConnection()
        {
            if (!PedestrianLoopConnectionSupported)
                return;

            var crossWalkNode1 = createdTrafficNodes[0].GetComponent<TrafficNodeCrosswalk>().GetCrosswalkNode(0);
            var crossWalkNode2 = createdTrafficNodes[2].GetComponent<TrafficNodeCrosswalk>().GetCrosswalkNode(1);

            if (loopPedestrianConnection)
            {
                crossWalkNode1.AddNode(crossWalkNode2);
                crossWalkNode2.AddNode(crossWalkNode1);
            }
            else
            {
                crossWalkNode1.RemoveConnection(crossWalkNode2);
            }
        }

        private PedestrianNode CreatePedestrianNode()
        {
#if UNITY_EDITOR
            return (UnityEditor.PrefabUtility.InstantiatePrefab(roadSegmentCreatorConfig.PedestrianNodePrefab.gameObject, transform) as GameObject).GetComponent<PedestrianNode>();
#else
            return null;
#endif
        }

        private void ConnectStraightRoadCrosswalks()
        {
            if (createdTrafficNodes.Count < 2)
                return;

            var spawnPoint11 = createdTrafficNodes[0].GetCrosswalkNode(0);
            var spawnPoint12 = createdTrafficNodes[0].GetCrosswalkNode(1);
            var spawnPoint21 = createdTrafficNodes[1].GetCrosswalkNode(0);
            var spawnPoint22 = createdTrafficNodes[1].GetCrosswalkNode(1);

            spawnPoint11.AddNode(spawnPoint22);
            spawnPoint21.AddNode(spawnPoint12);

            spawnPoint12.AddNode(spawnPoint21);
            spawnPoint22.AddNode(spawnPoint11);
        }
    }
}