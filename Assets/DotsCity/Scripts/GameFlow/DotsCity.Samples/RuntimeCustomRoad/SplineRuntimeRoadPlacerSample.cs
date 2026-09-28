using Spirit604.CityEditor.Utils;
using Spirit604.DotsCity.RuntimeRoad;
using Spirit604.DotsCity.RuntimeRoad.Custom;
using Spirit604.Gameplay.Config.Road;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.Samples.RuntimeCustomRoad
{
    /// <summary>
    /// Example class for placing runtime splines based on Unity.Spline
    /// </summary>
    public class SplineRuntimeRoadPlacerSample : MonoBehaviour
    {
        [SerializeField] private RuntimeRoadManagerCustom runtimeRoadManagerCustom;
        [SerializeField] private SharedLightStateContainer baseLightContainer;

        [SerializeField] private List<SplineRoad> splineRoads = new();
        [SerializeField] private List<IntersectionRoad> intersectionRoads = new();

#pragma warning disable CS0414

        [SerializeField] private bool autoAddRoadsAtStart = true;

#pragma warning restore CS0414

        /// <summary>
        /// Binding road scene components to the generated runtime data.
        /// </summary>
        private Dictionary<RoadBase, RuntimeSegmentCustom> sceneBinding = new();

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
            AddRoads(splineRoads, intersectionRoads);
        }

        public void AddRoads(List<SplineRoad> splineRoads, List<IntersectionRoad> intersectionRoads)
        {
            var newSegments = new List<RuntimeSegmentCustom>();

            ConvertRoads(splineRoads, intersectionRoads, newSegments);
            ConnectRoads(splineRoads, intersectionRoads);

#if RUNTIME_ROAD
            runtimeRoadManagerCustom.AddSegments(newSegments);
#endif
        }

        public void RemoveRoads(List<SplineRoad> splineRoads, List<IntersectionRoad> intersections)
        {
            var removeSegments = new List<RuntimeSegmentCustom>();

            foreach (var splineRoad in splineRoads)
            {
                var runtimeSegment = sceneBinding[splineRoad];

                removeSegments.Add(runtimeSegment);

                sceneBinding.Remove(splineRoad);
            }

            foreach (var intersection in intersections)
            {
                var runtimeSegment = sceneBinding[intersection];

                removeSegments.Add(runtimeSegment);

                sceneBinding.Remove(intersection);
            }

#if RUNTIME_ROAD
            runtimeRoadManagerCustom.RemoveSegments(removeSegments);
#endif
        }

        /// <summary>
        /// Convert all scene roads to run-time road data.
        /// </summary>
        private void ConvertRoads(List<SplineRoad> splineRoads, List<IntersectionRoad> intersections, List<RuntimeSegmentCustom> newSegments)
        {
            foreach (var sceneRoad in splineRoads)
            {
                RuntimeSegmentCustom newSegmentCustom = GetSegment(sceneRoad);

                newSegments.Add(newSegmentCustom);

                sceneBinding.Add(sceneRoad, newSegmentCustom);
            }

            foreach (var sceneRoad in intersections)
            {
                RuntimeSegmentCustom newSegmentCustom = GetSegment(sceneRoad);

                newSegments.Add(newSegmentCustom);

                sceneBinding.Add(sceneRoad, newSegmentCustom);
            }
        }

        /// <summary>
        /// Convert a scene road to a run-time road.
        /// </summary>
        private RuntimeSegmentCustom GetSegment(RoadBase sourceSegment)
        {
            var sourceNodes = sourceSegment.nodes;

            var runtimeSegment = sourceSegment.gameObject.AddComponent<RuntimeSegmentCustom>();

            runtimeSegment.Position = sourceSegment.transform.position;
            runtimeSegment.AddLightContainer(baseLightContainer);

            var runtimeTrafficNodes = new List<RoadSceneUtils.NodeSettings>();

            for (var index = 0; index < sourceNodes.Count; index++)
            {
                var node = sourceNodes[index];

                var runtimeNodeSettings = new RoadSceneUtils.NodeSettings()
                {
                    Position = node.Position,
                    Rotation = node.Rotation,
                    Settings = new RoadSceneUtils.BaseSettings()
                    {
                        LaneCount = sourceSegment.laneCount,
                        LaneWidth = sourceSegment.laneWidth
                    }
                };

                var newPaths = new List<RoadSceneUtils.PathData>();

                var paths = node.Paths;

                for (var i = 0; i < paths.Count; i++)
                {
                    var path = paths[i];
                    var newPath = new RoadSceneUtils.PathData();

                    // Populate path with waypoints
                    newPath.Waypoints.AddRange(path.Waypoints);

                    // Convert from km/h to m/s
                    newPath.SpeedLimit = path.SpeedLimit / ProjectConstants.KmhToMs_RATE;

                    // Straight road, recommended to set this explicitly to avoid extra calculations, for intersections path indexes are calculated automatically
                    if (sourceNodes.Count == 2)
                    {
                        newPath.StartLaneIndex = i;
                        newPath.ConnectedLaneIndex = i;
                        newPath.ConnectedNodeIndex = (index + 1) % 2;
                    }

                    newPaths.Add(newPath);
                }

                // Add runtime paths to created runtime traffic node
                runtimeNodeSettings.Paths = newPaths;

                runtimeTrafficNodes.Add(runtimeNodeSettings);
            }

            runtimeSegment.AddNodes(runtimeTrafficNodes);

            return runtimeSegment;
        }

        /// <summary>
        /// After all the runtime segment data has been created, neighboring traffic nodes must be merged.
        /// </summary>
        private void ConnectRoads(List<SplineRoad> splineRoads, List<IntersectionRoad> intersections)
        {
            foreach (var sceneRoad in splineRoads)
            {
                ConnectRoad(sceneRoad);
            }

            foreach (var sceneRoad in intersections)
            {
                ConnectRoad(sceneRoad);
            }
        }

        private void ConnectRoad(RoadBase sceneRoad)
        {
            var sourceSegment = sceneBinding[sceneRoad];

            int index = 0;

            sourceSegment.IterateAllNodes(sourceRuntimeNode =>
            {
                var node = sceneRoad.nodes[index];
                index++;

                if (sourceRuntimeNode.ConnectedExternalNode != null) return;
                if (node.ConnectedRoad == null) return;

                var connectedRuntimeRoad = sceneBinding[node.ConnectedRoad];

                // Find the closest node to merge with from connected roads
                var connectedNode = connectedRuntimeRoad.GetClosestNode(sourceRuntimeNode.Position);

                // Neighbor merged node
                sourceRuntimeNode.ConnectedExternalNode = connectedNode;
                connectedNode.ConnectedExternalNode = sourceRuntimeNode;

#if UNITY_EDITOR
                sourceRuntimeNode.ConnectedExternalSegment = connectedNode.Owner;
                connectedNode.ConnectedExternalSegment = sourceSegment;
#endif
            });
        }
    }
}