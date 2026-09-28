using Spirit604.Attributes;
using Spirit604.CityEditor.Utils;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_SPLINE
using UnityEngine.Splines;
#endif

namespace Spirit604.DotsCity.Samples.RuntimeCustomRoad
{
    /// <summary>
    /// Sample class to generate lanes for intersection from connected Unity.Spline s.
    /// </summary>
    public class IntersectionRoad : RoadBase
    {
#if UNITY_SPLINE
        [Header("Generation Data")]

        public List<SplineContainer> sceneContainers = new List<SplineContainer>();

        [Button]
        public void GenerateWaypoints()
        {
            nodes.Clear();

            var intersection = RoadSceneUtils.CreateIntersection(sceneContainers,
                segmentSettings: new RoadSceneUtils.SegmentSettings()
                {
                    LaneCount = laneCount,
                    LaneWidth = laneWidth
                });

            int index = 0;

            transform.position = intersection.transform.position;

            intersection.IterateAllNodes(trafficNode =>
            {
                var node = new NodeData();
                node.Position = trafficNode.transform.position;
                node.Rotation = trafficNode.transform.rotation;
                node.ConnectedRoad = sceneContainers[index].GetComponent<SplineRoad>();

                trafficNode.IterateAllPaths(path =>
                {
                    var pathData = new PathData();

                    path.IterateWaypoints(waypoint =>
                    {
                        pathData.Waypoints.Add(waypoint.transform.position);
                    });

                    pathData.SpeedLimit = path.PathSpeedLimit;
                    node.Paths.Add(pathData);
                });

                nodes.Add(node);

                index++;
            });

            DestroyImmediate(intersection.gameObject);

            EditorSaver.SetObjectDirty(this);
        }
#endif
    }
}