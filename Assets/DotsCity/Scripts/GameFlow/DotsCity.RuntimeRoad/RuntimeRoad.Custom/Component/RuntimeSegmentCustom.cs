using Spirit604.CityEditor.Utils;
using Spirit604.Gameplay.Config.Road;
using Spirit604.Gameplay.Road;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad.Custom
{
    public class RuntimeSegmentCustom : MonoBehaviour
    {
        public SharedLightStateContainer LightStateContainer;
        public List<TrafficNodeData> Nodes = new List<TrafficNodeData>();
        public List<PedestrianNodeData> PedestrianNodes = new List<PedestrianNodeData>();
        public List<PathData> AllPaths = new List<PathData>();

        public Vector3 Position { get; set; }

        /// <summary>
        /// Enable this flag if you want the intersection to have no traffic lights & traffic to pass according to the priority set for each path of this intersection.
        /// </summary>
        public bool ForceDisableLight { get; set; }

        public bool Intersection => NodeCount != 2;

        public int NodeCount => Nodes.Count;

        public bool StraightRoad => !Intersection || NodeCount == 2;

        public bool LightCrossroad => NodeCount >= 3;

        public bool TrafficLightEnabled => LightCrossroad && !ForceDisableLight;

        public int CrossroadID { get; internal set; }

        public NativeList<int> TakenPaths { get; internal set; }

        internal int CrossroadIndex { get; set; }

        public void AddNodes(List<RoadSceneUtils.NodeSettings> nodeList)
        {
            int pathIndex = 0;

            for (int i = 0; i < nodeList.Count; i++)
            {
                RoadSceneUtils.NodeSettings node = nodeList[i];
                var newNode = new TrafficNodeData(this, i, node);
                Nodes.Add(newNode);

                newNode.MinLocalPathIndex = pathIndex;

                for (int j = 0; j < node.Paths.Count; j++)
                {
                    RoadSceneUtils.PathData path = node.Paths[j];
                    AllPaths.Add(new PathData(path, pathIndex++));
                }

                newNode.MaxLocalPathIndex = pathIndex;
            }

            var node1 = Nodes[0];

            int lightIndex = 0;
            Nodes[0].LightIndex = lightIndex;

            for (int i = 1; i < nodeList.Count; i++)
            {
                var node2 = Nodes[i];

                var dir = Vector3.Normalize(node1.Position - node2.Position);
                var angle = Vector3.Angle(node1.Forward, dir);

                if (angle > 180)
                {
                    angle -= 180;
                }

                const float angleOffset = 20;

                var currentIndex = angle < angleOffset || angle > 180 - angleOffset ? lightIndex : (lightIndex + 1) % 2;

                node2.LightIndex = currentIndex;
            }
        }

        /// <summary>
        /// Adds a set of nodes & all paths of the segment with automatic binding to its nodes. Will incur additional overhead, use the AddNodes method if you have already binded paths to nodes yourself.
        /// </summary>
        /// <param name="nodeList">All runtime traffic nodes in the runtime segment.</param>
        /// <param name="allPaths">All paths in the runtime segment.</param>
        public void AddNodesRaw(List<RoadSceneUtils.NodeSettings> nodeList, List<RoadSceneUtils.PathData> allPaths)
        {
            for (int i = 0; i < nodeList.Count; i++)
            {
                nodeList[i].Paths = new List<RoadSceneUtils.PathData>();
            }

            for (int i = 0; i < allPaths.Count; i++)
            {
                RoadSceneUtils.NodeSettings closestNode = null;
                float maxDistance = float.MaxValue;

                var startPosition = allPaths[i].Waypoints[0];

                for (int j = 0; j < nodeList.Count; j++)
                {
                    var distance = Vector3.SqrMagnitude(nodeList[j].Position - startPosition);

                    if (distance < maxDistance)
                    {
                        maxDistance = distance;
                        closestNode = nodeList[j];
                    }
                }

                closestNode.Paths.Add(allPaths[i]);
            }

            for (int i = 0; i < nodeList.Count; i++)
            {
                if (nodeList[i].Paths.Count > 1 && nodeList[i].Settings.LaneCount > 1)
                    nodeList[i].Paths = nodeList[i].Paths
                        .OrderBy(a => Vector3.SqrMagnitude(nodeList[i].Position - a.Waypoints[0])).ToList();
            }

            AddNodes(nodeList);
        }

        /// <summary>
        /// Add a line of connected pedestrian nodes, such as along a spline path.
        /// </summary>
        public void AddPedestrianNodes(List<Vector3> points, bool crossing = false, float width = 1f, float height = 2f, NodeShapeType crosswalkShapeType = NodeShapeType.Rectangle)
        {
            if (crossing && points.Count > 2)
            {
                points = new List<Vector3>() { points[0], points[points.Count - 1] };
            }

            PedestrianNodeData prev = null;

            for (int i = 0; i < points.Count; i++)
            {
                bool endNode = i == 0 || i == points.Count - 1;
                var current = CreatePedestrianNode(points[i], width, height, crosswalkShapeType, endNode);

                if (prev != null)
                {
                    current.Previous = prev;
                    prev.Next = current;
                }

                prev = current;
            }
        }

        /// <summary>
        /// Add a line of connected pedestrian nodes, such as along a spline path.
        /// </summary>
        public void AddPedestrianNodes<T>(List<T> nodes, bool crossing = false, float width = 1f, float height = 2f, NodeShapeType crosswalkShapeType = NodeShapeType.Rectangle) where T : MonoBehaviour
        {
            if (crossing && nodes.Count > 2)
            {
                nodes = new List<T>() { nodes[0], nodes[nodes.Count - 1] };
            }

            PedestrianNodeData prev = null;

            for (int i = 0; i < nodes.Count; i++)
            {
                bool endNode = i == 0 || i == nodes.Count - 1;
                var shape = !crossing ? NodeShapeType.Circle : crosswalkShapeType;
                var current = CreatePedestrianNode(nodes[i].transform.position, width, height, shape, endNode);

                if (prev != null)
                {
                    current.Previous = prev;
                    prev.Next = current;
                }

                prev = current;
            }
        }

        /// <summary>
        /// Create a single pedestrian node. The connection should be provided by the user.
        /// </summary>
        public PedestrianNodeData CreatePedestrianNode(Vector3 position, float width, float height = 2f, NodeShapeType nodeShapeType = NodeShapeType.Circle, bool endNode = false)
        {
            int nodeIndex = -1;

            if (endNode)
            {
                var node = this.GetClosestNode(position);
                nodeIndex = node.LocalNodeIndex;
            }

            var nodeData = new PedestrianNodeData()
            {
                Owner = this,
                Position = position,
                ConnectedNodeIndex = nodeIndex,
                NodeShapeType = nodeShapeType,
                Width = width,
                Height = height,
            };

            PedestrianNodes.Add(nodeData);

            return nodeData;
        }

        public TrafficNodeData GetNode(int nodexIndex)
        {
            if (nodexIndex >= 0 && Nodes.Count > nodexIndex)
                return Nodes[nodexIndex];

            return null;
        }

        public TrafficNodeData GetConnectedNode(PedestrianNodeData nodeData)
        {
            return GetNode(nodeData.ConnectedNodeIndex);
        }

        public int GetNodeIndex(TrafficNodeData node)
        {
            return Nodes.IndexOf(node);
        }

        public int GetPathIndex(int localPathIndex)
        {
            return TakenPaths[localPathIndex];
        }

        public bool HasLocalPathIndex(TrafficNodeData nodeData, int localPathIndex)
        {
            return nodeData.MinLocalPathIndex <= localPathIndex && localPathIndex < nodeData.MaxLocalPathIndex;
        }

        public int GetPathIndex(PathData localPathIndex)
        {
            return TakenPaths[localPathIndex.LocalPathIndex];
        }

        public int GetGlobalPath(int localPathIndex)
        {
            return TakenPaths[localPathIndex];
        }

        public bool RemoveGlobalPath(int globalPathIndex)
        {
            var index = TakenPaths.IndexOf(globalPathIndex);

            if (index >= 0)
            {
                TakenPaths.RemoveAt(index);
                return true;
            }

            return false;
        }

        public void Dispose()
        {
            TakenPaths.Dispose();
        }

        public PathData GetPathData(int i)
        {
            return AllPaths[i];
        }

        public void AddLightContainer(SharedLightStateContainer lightStateContainer)
        {
            this.LightStateContainer = lightStateContainer;
        }
    }
}
