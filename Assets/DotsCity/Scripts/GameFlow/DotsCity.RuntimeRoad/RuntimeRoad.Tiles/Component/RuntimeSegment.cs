using Spirit604.Collections.Dictionary;
using Spirit604.DotsCity.Simulation.Road.Authoring;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    [DisallowMultipleComponent]
    public class RuntimeSegment : MonoBehaviour
    {
        private const float MinEdgeDistance = 3f;

        [Serializable]
        public class BakedData
        {
            public List<Path> NeighboardsPaths = new List<Path>();
            public List<Path> ParallelPaths = new List<Path>();
            public bool CustomNode;
        }

        [Serializable]
        public class CrossroadData
        {
            public List<TrafficLightObjectAuthoring> Lights = new List<TrafficLightObjectAuthoring>();
        }

        [Serializable]
        public class ConnectedNodeData
        {
            public List<Path> Paths = new List<Path>();
        }

        [Serializable]
        public class ConnectionEdge : IEquatable<ConnectionEdge>
        {
            public PedestrianNode Node1;
            public PedestrianNode Node2;

            public bool Equals(ConnectionEdge other) =>
                (this.Node1 == other.Node1 && this.Node2 == other.Node2) || (this.Node1 == other.Node2 && this.Node2 == other.Node1);

            public override int GetHashCode()
            {
                if (Node1 && Node2)
                    return Node1.GetHashCode() + Node2.GetHashCode();

                return 0;
            }
        }

        [Serializable]
        public class PathBakedDictionary : AbstractSerializableDictionary<Path, BakedData> { }

        [Serializable]
        public class CrossroadDataDictionary : AbstractSerializableDictionary<TrafficLightCrossroad, CrossroadData> { }

        [Serializable]
        public class ConnectedNodeDataDictionary : AbstractSerializableDictionary<TrafficNode, ConnectedNodeData> { }

        [Tooltip("Automatically adds a segment to the simulation if it is initially on the scene and this parameter is enabled.")]
        public bool addOnAwake;
        public bool autoUnregisterOnDisable;

        public PathBakedDictionary bakedData = new PathBakedDictionary();
        public CrossroadDataDictionary crossroadData = new CrossroadDataDictionary();
        public ConnectedNodeDataDictionary allConnectedPaths = new ConnectedNodeDataDictionary();
        public TrafficNode[] casterTrafficNodes;
        public PedestrianNode[] pedestrianNodes;
        public Path[] paths;
        public ConnectionEdge[] connectionEdges;
        public NodeCaster[] nodeCasters;

        private bool placed;

        public bool AddOnAwake
        {
            get => addOnAwake;
            set
            {
                if (addOnAwake != value)
                {
                    addOnAwake = value;
                    EditorSaver.SetObjectDirty(this);
                }
            }
        }

        public bool Placed => placed;

        public bool IsChunk => crossroadData.Keys.Count > 1;

        internal NativeList<int> TakenPaths { get; set; }

        private void OnEnable()
        {
            if (addOnAwake)
                PlaceSegment();
        }

        private void OnDisable()
        {
            if (autoUnregisterOnDisable)
                RemoveSegment();
        }

        public void Bake()
        {
            bakedData.Clear();
            crossroadData.Clear();

            var crossroads = GetComponentsInChildren<TrafficLightCrossroad>();
            var segments = GetComponentsInChildren<RoadSegment>();

            foreach (var crossroad in crossroads)
                crossroadData.Add(crossroad, new CrossroadData());

            foreach (var segment in segments)
                segment.BakeData();

            var nodes = new List<TrafficNode>();

            foreach (var item in crossroads)
            {
                nodes.AddRange(item.TrafficNodes);
            }

            var lights = GetComponentsInChildren<TrafficLightObjectAuthoring>();

            this.pedestrianNodes = GetComponentsInChildren<PedestrianNode>();

            if (!IsChunk)
            {
                this.nodeCasters = GetComponentsInChildren<NodeCaster>();
            }

            var connections = new HashSet<ConnectionEdge>();

            foreach (var node in pedestrianNodes)
            {
                var connectedNodes = node.DefaultConnectedPedestrianNodes;

                foreach (var connectedNode in connectedNodes)
                {
                    var distance = Vector3.Distance(connectedNode.transform.position, node.transform.position);

                    if (distance > MinEdgeDistance &&
                        (node.ConnectedTrafficNode != connectedNode.ConnectedTrafficNode || node.ConnectedTrafficNode == null))
                    {
                        var pair = new ConnectionEdge()
                        {
                            Node1 = node,
                            Node2 = connectedNode,
                        };

                        if (!IsChunk)
                        {
                            if (!connections.Contains(pair))
                            {
                                connections.Add(pair);
                            }
                        }
                    }
                }
            }

            if (!IsChunk)
            {
                connectionEdges = connections.ToArray();
            }

            for (int i = 0; i < lights?.Length; i++)
            {
                TrafficLightObjectAuthoring light = lights[i];
                light.AutoRegister = false;

                var connectedCrossroad = light.TrafficLightObject.TrafficLightCrossroad;

                if (connectedCrossroad != null && crossroadData.ContainsKey(connectedCrossroad))
                {
                    crossroadData[connectedCrossroad].Lights.Add(light);
                }

                EditorSaver.SetObjectDirty(light);
            }

            var paths = new List<Path>();
            var tempPaths = new List<Path>();

            allConnectedPaths.Clear();

            var casterNodes = new List<TrafficNode>();

            foreach (var node in nodes)
            {
                if (!node.TryGetComponent<NodeOwnerComponent>(out var comp))
                {
                    node.gameObject.AddComponent<NodeOwnerComponent>();
                }
                else
                {
                    comp.BindOwner();
                }

                if (!node.HasAtleastOneLane(true) && (!node.IsOneWay || node.IsEndOfOneWay) || (node.IsOneWay && !node.IsEndOfOneWay && !IsLinkedTrafficNode(node)))
                {
                    casterNodes.Add(node);
                }

                node.IterateAllPaths(path =>
                {
                    paths.Add(path);

                    var connectedNode = path.ConnectedTrafficNode;

                    if (connectedNode != null)
                    {
                        if (!allConnectedPaths.ContainsKey(connectedNode))
                        {
                            allConnectedPaths.Add(connectedNode, new ConnectedNodeData()
                            {
                                Paths = new List<Path>()
                            });
                        }

                        allConnectedPaths[connectedNode].Paths.Add(path);
                    }

                }, true);
            }

            this.casterTrafficNodes = casterNodes.ToArray();
            this.paths = paths.ToArray();

            foreach (var node in nodes)
            {
                node.IterateAllPaths((path) =>
                {
                    var data = new BakedData();

                    bool isExternal = path.IsExternal;

                    var laneData = node.TryToGetLaneData(path.SourceLaneIndex, isExternal);

                    tempPaths.Clear();

                    foreach (var neighbourPath in laneData.paths)
                    {
                        if (neighbourPath == path) continue;

                        tempPaths.Add(neighbourPath);
                    }

                    data.NeighboardsPaths = new List<Path>(tempPaths);

                    tempPaths.Clear();

                    var parallelData1 = node.TryToGetLaneData(path.SourceLaneIndex - 1, isExternal);

                    if (parallelData1 != null)
                    {
                        foreach (var parallelPath in parallelData1.paths)
                        {
                            if (parallelPath != path && parallelPath.ConnectedTrafficNode == path.ConnectedTrafficNode)
                            {
                                tempPaths.Add(parallelPath);
                            }
                        }
                    }

                    var parallelData2 = node.TryToGetLaneData(path.SourceLaneIndex + 1, isExternal);

                    if (parallelData2 != null)
                    {
                        foreach (var parallelPath in parallelData2.paths)
                        {
                            if (parallelPath != path && parallelPath.ConnectedTrafficNode == path.ConnectedTrafficNode)
                            {
                                tempPaths.Add(parallelPath);
                            }
                        }
                    }

                    data.ParallelPaths = new List<Path>(tempPaths);

                    bool customNode = false;

                    path.IterateWaypoints((waypoint) =>
                    {
                        if (waypoint.CustomGroup)
                        {
                            customNode = true;
                        }
                    });

                    data.CustomNode = customNode;

                    bakedData.Add(path, data);
                }, true);
            }

            EditorSaver.SetObjectDirty(this);
        }

        /// <summary>
        /// Editor method for prefab baking. Useful for baking with custom user code from the custom prefab collection, e.g.
        /// </summary>
        public void BakePrefab()
        {
#if UNITY_EDITOR
            PrefabExtension.EditPrefab<RuntimeSegment>(gameObject, (prefab =>
            {
                prefab.Bake();
            }));
#endif
        }

        public void PlaceSegment()
        {
            if (!placed)
            {
                placed = true;

#if RUNTIME_ROAD
                RuntimeRoadManager.Instance.AddSegment(this);
#endif
            }
        }

        public void RemoveSegment()
        {
            if (placed)
            {
                placed = false;

#if RUNTIME_ROAD
                if (RuntimeRoadManager.Instance)
                    RuntimeRoadManager.Instance.RemoveSegment(this);
#endif
            }
        }

        public int GetGlobalPathIndex(Path path)
        {
            if (!path) return -1;

            var localIndex = Array.IndexOf(paths, path);

            return TakenPaths[localIndex];
        }

        public List<Path> GetAllInnerPathsConnectedToNode(TrafficNode trafficNode)
        {
            if (allConnectedPaths != null && allConnectedPaths.TryGetValue(trafficNode, out var data))
            {
                return data.Paths;
            }

            return null;
        }

        public List<Path> GetInnerPathsConnectedToNode(TrafficNode trafficNode, int connectedLane)
        {
            List<Path> result = null;

            GetInnerPathsConnectedToNodeNonAlloc(trafficNode, connectedLane, ref result);
            return result;
        }

        public bool GetInnerPathsConnectedToNodeNonAlloc(TrafficNode trafficNode, int connectedLane, ref List<Path> paths)
        {
            bool changed = false;

            if (paths != null)
            {
                paths.Clear();
            }

            if (allConnectedPaths != null && allConnectedPaths.TryGetValue(trafficNode, out var data))
            {
                for (int i = 0; i < data.Paths.Count; i++)
                {
                    var path = data.Paths[i];

                    if (path.ConnectedLaneIndex == connectedLane)
                    {
                        if (paths == null)
                        {
                            paths = new List<Path>();
                        }

                        paths.Add(path);
                        changed = true;
                    }
                }
            }

            return changed;
        }

        public bool TrafficCasterNode(TrafficNode node) => casterTrafficNodes != null && casterTrafficNodes.Contains(node);

        public bool PathIsTaken(int globalPathIndex) => TakenPaths.Contains(globalPathIndex);

        public List<Path> GetNeighbourPaths(Path path)
        {
            if (bakedData.TryGetValue(path, out var data))
            {
                return data.NeighboardsPaths;
            }

            return null;
        }

        public List<Path> GetParallelPaths(Path path)
        {
            if (bakedData.TryGetValue(path, out var data))
            {
                return data.ParallelPaths;
            }

            return null;
        }

        public static bool IsLinkedTrafficNode(TrafficNode node) =>
             node.TrafficNodeType == TrafficNodeType.Parking || node.TrafficNodeType == TrafficNodeType.TrafficPublicStop;

        public void Validate()
        {
#if UNITY_EDITOR
            string message = string.Empty;

            for (int i = 0; i < pedestrianNodes.Length; i++)
            {
                var node = pedestrianNodes[i];

                if (node == null)
                {
                    TryToAddHeader(ref message);
                    message += $"- <b>PedestrianNode</b> Index {i} is null.\r\n";
                }
            }

            for (int i = 0; i < paths.Length; i++)
            {
                var path = paths[i];

                if (path == null)
                {
                    TryToAddHeader(ref message);
                    message += $"- <b>Path</b> Index {i} is null.\r\n";
                }
                else
                {
                    if (path.PathLength == 0)
                    {
                        TryToAddHeader(ref message);
                        message += $"- <b>Path</b> {path.name} Index {i} is not baked.\r\n";
                    }

                    if (path.PathConnectionType == PathConnectionType.TrafficNode)
                    {
                        if (path.SourceTrafficNode == null)
                            message += $"- <b>Path</b> {path.name} Index {i} <b>Source traffic node</b> is null.\r\n";

                        if (path.ConnectedTrafficNode == null)
                            message += $"- <b>Path</b> {path.name} Index {i} <b>Connected traffic node</b> is null.\r\n";

                    }
                    else if (path.PathConnectionType == PathConnectionType.PathPoint)
                    {
                        if (path.ConnectedPath == null)
                            message += $"- <b>Path</b> {path.name} Index {i} has <b>PathPoint</b> connection type, but <b>Connected path</b> is null.\r\n";
                    }
                }
            }

            var childCrossroads = this.GetComponentsInChildren<TrafficLightCrossroad>();

            if (childCrossroads.Length != crossroadData.Keys.Count)
            {
                TryToAddHeader(ref message);
                message += $"- Some of the <b>TrafficLightCrossroad</b> not added or not baked.\r\n";
            }

            var childPaths = this.GetComponentsInChildren<Path>();

            if (childPaths.Length != paths.Length)
            {
                TryToAddHeader(ref message);
                message += $"- Some of the <b>paths</b> not added or not baked.\r\n";
            }

            var childNodes = this.GetComponentsInChildren<PedestrianNode>();

            if (childNodes.Length != pedestrianNodes.Length)
            {
                TryToAddHeader(ref message);
                message += $"- Some of the <b>PedestrianNode</b> not added or not baked.\r\n";
            }

            int lightCount = 0;
            bool trafficNodeReported = false;

            foreach (var crossroad in crossroadData)
            {
                if (crossroad.Key == null)
                {
                    TryToAddHeader(ref message);
                    message += $"- Some of the crossroads is null.\r\n";
                    continue;
                }

                lightCount += crossroad.Value.Lights.Count;

                var lights = crossroad.Value.Lights;

                for (int i = 0; i < lights.Count; i++)
                {
                    TrafficLightObjectAuthoring light = lights[i];

                    if (light == null)
                    {
                        TryToAddHeader(ref message);
                        message += $"- {crossroad.Key.name}. TrafficLightObjectAuthoring Index {i} is null.\r\n";
                    }
                }

                var nodes = crossroad.Key.TrafficNodes;

                for (int i = 0; i < nodes.Count; i++)
                {
                    var node = nodes[i];

                    if (node == null)
                    {
                        TryToAddHeader(ref message);
                        message += $"- <b>TrafficNode</b> Index {i} is null.\r\n";
                    }
                    else
                    {
                        if (!node.TryGetComponent<NodeOwnerComponent>(out var comp))
                        {
                            if (!trafficNodeReported)
                            {
                                trafficNodeReported = true;
                                message += $"- Some of the <b>TrafficNode</b> doesn't have <b>NodeOwnerComponent</b>.\r\n";
                            }
                        }
                    }
                }
            }

            var childLights = this.GetComponentsInChildren<TrafficLightObjectAuthoring>();

            if (childLights.Length != lightCount)
            {
                TryToAddHeader(ref message);
                message += $"- Some of the <b>TrafficLightObjectAuthoring</b> not added or not baked.\r\n";
            }

            if (!string.IsNullOrEmpty(message))
            {
                message += $"\r\nOpen <b>{name}</b> <b>Prefab</b> & press <b>Bake</b> button in <b>RuntimeSegment</b> component.\r\n\r\n\r\n\r\n\r\n";
                Debug.Log(message);
            }
#endif
        }

        internal void Dispose()
        {
            if (TakenPaths.IsCreated)
            {
                TakenPaths.Dispose();
                TakenPaths = default;
            }
        }

#if UNITY_EDITOR
        private void TryToAddHeader(ref string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                text = $"RuntimeSegment. Prefab <b>{name}</b> has some errors:\r\n";
            }
        }

        private void Reset()
        {
            Bake();
            EditorSaver.SetObjectDirty(this);
        }
#endif
    }
}
