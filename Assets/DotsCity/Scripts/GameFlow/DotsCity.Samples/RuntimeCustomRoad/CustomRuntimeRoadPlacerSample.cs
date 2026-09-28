using Spirit604.CityEditor.Road;
using Spirit604.CityEditor.Utils;
using Spirit604.DotsCity.RuntimeRoad;
using Spirit604.DotsCity.RuntimeRoad.Custom;
using Spirit604.DotsCity.Simulation.Road.Authoring;
using Spirit604.Gameplay.Config.Road;
using Spirit604.Gameplay.Road;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.Samples.RuntimeCustomRoad
{
    /// <summary>
    /// Sample class for placing runtime roads based on custom data (current example uses built-in road segments for data)
    /// </summary>
    public class CustomRuntimeRoadPlacerSample : MonoBehaviour
    {
        [SerializeField] private RuntimeRoadManagerCustom runtimeRoadManagerCustom;
        [SerializeField] private SharedLightStateContainer baseLightContainer;

        [SerializeField] private List<Connector> sceneRoads = new List<Connector>();

#pragma warning disable CS0414

        [SerializeField] private bool autoAddRoadsAtStart = true;
        [SerializeField] private bool addPedestrianRoutes = true;

#pragma warning restore CS0414

        /// <summary>
        /// Binding road scene components to the generated runtime data.
        /// </summary>
        private Dictionary<RoadSegmentCreator, RuntimeSegmentCustom> sceneBinding = new();

        private void Start()
        {
            if (runtimeRoadManagerCustom == null)
            {
                Debug.LogError("CustomRuntimeRoadPlacerSample. RuntimeRoadManagerCustom not created or not assigned");
                return;
            }

            if (baseLightContainer == null)
            {
                Debug.LogError("CustomRuntimeRoadPlacerSample. BaseLightContainer not assigned");
                return;
            }

#if UNITY_EDITOR
            Gameplay.Road.Debug.PathDebugger.ShouldDrawEditorPath = false;
            Gameplay.Road.Debug.PathDebugger.ShouldDrawPedestrianConnectionPath = false;
#endif

            StartCoroutine(WaitForInit());
        }

        private IEnumerator WaitForInit()
        {
#if RUNTIME_ROAD
            yield return new WaitWhile(() => !runtimeRoadManagerCustom.Init);

            if (autoAddRoadsAtStart)
                ProcessInitialRoads();
#else
            yield return null;
#endif
        }

        public void ProcessInitialRoads()
        {
            AddRoads(sceneRoads);
        }

        public void AddRoads(List<Connector> sceneRoads)
        {
            var newSegments = new List<RuntimeSegmentCustom>();

            ConvertRoads(sceneRoads, newSegments);
            ConnectRoads(sceneRoads);

#if RUNTIME_ROAD
            runtimeRoadManagerCustom.AddSegments(newSegments);
#endif

            RegisterTrafficLights(sceneRoads);
        }

        public void RemoveRoads(List<Connector> sceneRoads)
        {
            var removeSegments = new List<RuntimeSegmentCustom>();

            foreach (var road in sceneRoads)
            {
                var runtimeSegment = sceneBinding[road.Current];

                removeSegments.Add(runtimeSegment);

                sceneBinding.Remove(road.Current);
            }

#if RUNTIME_ROAD
            runtimeRoadManagerCustom.RemoveSegments(removeSegments);
#endif
        }

        /// <summary>
        /// Convert all scene roads to run-time road data.
        /// </summary>
        private void ConvertRoads(List<Connector> newRoads, List<RuntimeSegmentCustom> newSegments)
        {
            foreach (var sceneRoad in newRoads)
            {
                RuntimeSegmentCustom newSegmentCustom = GetSegment(sceneRoad);

                newSegments.Add(newSegmentCustom);

                sceneBinding.Add(sceneRoad.Current, newSegmentCustom);
            }
        }

        /// <summary>
        /// In this sample built-in roads converted to runtime roads, but data can be anything in the same format.
        /// </summary>
        private RuntimeSegmentCustom GetSegment(Connector sceneRoad)
        {
            var sourceSegment = sceneRoad.Current;
            var sourceTrafficNodes = sourceSegment.CreatedTrafficNodes;

            var runtimeSegment = sourceSegment.gameObject.AddComponent<RuntimeSegmentCustom>();

            runtimeSegment.Position = sourceSegment.transform.position;
            runtimeSegment.AddLightContainer(baseLightContainer);

            var runtimeTrafficNodes = new List<RoadSceneUtils.NodeSettings>();

            for (var index = 0; index < sourceTrafficNodes.Count; index++)
            {
                var trafficNode = sourceTrafficNodes[index];

                // Populate runtime traffic node settings, these settings are the same as in TrafficNode, which is used for editor roads
                var runtimeNodeSettings = new RoadSceneUtils.NodeSettings()
                {
                    Position = trafficNode.transform.position,
                    Rotation = trafficNode.transform.rotation,
                    Settings = new RoadSceneUtils.BaseSettings(trafficNode)
                };

                var newPaths = new List<RoadSceneUtils.PathData>();

                trafficNode.IterateAllPaths(path =>
                {
                    var newPath = new RoadSceneUtils.PathData();

                    // Populate path with waypoints
                    path.IterateWaypoints(waypoint =>
                    {
                        newPath.Waypoints.Add(waypoint.transform.position);
                    });

                    newPath.SpeedLimit = path.PathSpeedLimit;
                    newPath.Priority = path.Priority;

                    // Straight road, recommended to set this explicitly to avoid extra calculations, for intersections path indexes are calculated automatically
                    if (sourceTrafficNodes.Count == 2)
                    {
                        newPath.StartLaneIndex = path.ConnectedLaneIndex;
                        newPath.ConnectedLaneIndex = path.ConnectedLaneIndex;
                        newPath.ConnectedNodeIndex = (index + 1) % 2;
                    }

                    newPaths.Add(newPath);
                });

                // Add runtime paths to created runtime traffic node
                runtimeNodeSettings.Paths = newPaths;

                runtimeTrafficNodes.Add(runtimeNodeSettings);
            }

            runtimeSegment.AddNodes(runtimeTrafficNodes);

            if (addPedestrianRoutes)
            {
                // The current example uses built-in pedestrian nodes, but you can create nodes from your own data using node position & route width
                var tempPedestrianBinding = new Dictionary<PedestrianNode, PedestrianNodeData>();

                // Create pedestrian nodes for crosswalks
                sourceSegment.IterateAllNodes(node =>
                {
                    var node1 = node.GetCrosswalkNode(0);
                    var node2 = node.GetCrosswalkNode(1);

                    if (node1 && node2 && node1.gameObject.activeSelf)
                    {
                        var newNode1 = runtimeSegment.CreatePedestrianNode(node1.transform.position, node1.MaxPathWidth,
                            node1.Height,
                            NodeShapeType.Rectangle, true);

                        var newNode2 = runtimeSegment.CreatePedestrianNode(node2.transform.position, node2.MaxPathWidth,
                            node2.Height,
                            NodeShapeType.Rectangle, true);

                        tempPedestrianBinding.Add(node1, newNode1);
                        tempPedestrianBinding.Add(node2, newNode2);
                    }
                });

                var corners = sourceSegment.cornerPedestrianNodes;

                // Create pedestrian nodes for corners or along path nodes
                foreach (var corner in corners)
                {
                    var newNode = runtimeSegment.CreatePedestrianNode(corner.transform.position, corner.MaxPathWidth,
                        corner.Height,
                        NodeShapeType.Circle, false);

                    tempPedestrianBinding.Add(corner, newNode);
                }

                // Connect pedestrian nodes
                foreach (var data in tempPedestrianBinding)
                {
                    var sourceNode = data.Key;
                    var sourceRuntimeNode = data.Value;

                    sourceNode.IterateConnectedNodes(connectedNode =>
                    {
                        // Connect pedestrian nodes with null or different crosswalks, connections within the crosswalk are controlled by the HasCrosswalk flag in the TrafficNode settings.
                        if (sourceNode.ConnectedTrafficNode == null ||
                            sourceNode.ConnectedTrafficNode != connectedNode.ConnectedTrafficNode)
                        {
                            var connectedRuntimeNode = tempPedestrianBinding[connectedNode];

                            sourceRuntimeNode.Next = connectedRuntimeNode;
                            connectedRuntimeNode.Previous = sourceRuntimeNode;
                        }
                    });
                }
            }

            return runtimeSegment;
        }

        /// <summary>
        /// After all the runtime segment data has been created, neighboring traffic nodes must be merged.
        /// </summary>
        private void ConnectRoads(List<Connector> newRoads)
        {
            var tempConnectedSegments = new List<RuntimeSegmentCustom>();

            foreach (var sceneRoad in newRoads)
            {
                var sourceSegment = sceneBinding[sceneRoad.Current];

                sourceSegment.IterateAllNodes(sourceRuntimeNode =>
                {
                    if (sourceRuntimeNode.ConnectedExternalNode != null) return;

                    tempConnectedSegments.Clear();

                    foreach (var connectedRoad in sceneRoad.ConnectedRoads)
                    {
                        var connectedRuntimeRoad = sceneBinding[connectedRoad];
                        tempConnectedSegments.Add(connectedRuntimeRoad);
                    }

                    // Find the closest node to merge with from connected roads
                    var connectedNode =
                        RuntimeSegmentCustomExtension.GetClosestNode(tempConnectedSegments, sourceRuntimeNode.Position);

                    if (connectedNode != null)
                    {
                        // Neighbor merged node
                        sourceRuntimeNode.ConnectedExternalNode = connectedNode;
                        connectedNode.ConnectedExternalNode = sourceRuntimeNode;

#if UNITY_EDITOR
                        sourceRuntimeNode.ConnectedExternalSegment = connectedNode.Owner;
                        connectedNode.ConnectedExternalSegment = sourceSegment;
#endif
                    }
                });
            }
        }

        private void RegisterTrafficLights(List<Connector> sceneRoads)
        {
            foreach (var road in sceneRoads)
            {
                var segment = sceneBinding[road.Current];

                if (!segment.TrafficLightEnabled) continue;

                var createdLights = road.Current.createdLights;

                for (var i = 0; i < createdLights?.Count; i++)
                {
                    var light = createdLights[i];
                    light.ConnectedId = segment.CrossroadID;
                    light.GetComponent<TrafficLightObjectAuthoring>().RegisterFrames();
                }
            }
        }
    }
}