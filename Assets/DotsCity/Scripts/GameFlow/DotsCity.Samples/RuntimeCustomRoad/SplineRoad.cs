using Spirit604.Attributes;
using Spirit604.Extensions;
using System.Linq;
using UnityEngine;

#if UNITY_SPLINE
using UnityEngine.Splines;
#endif

namespace Spirit604.DotsCity.Samples.RuntimeCustomRoad
{
    /// <summary>
    /// Sample class for generating lanes from Unity.Spline
    /// </summary>
    public class SplineRoad : RoadBase
    {
#if UNITY_SPLINE
        [Header("Generation Settings")]
        [Tooltip("Reference to the central spline")]
        public SplineContainer splineContainer;

        [Tooltip("Interval for waypoints along the spline")]
        public float waypointInterval = 1f;

        public float speedLimit = 60f;

        [Button]
        public void GenerateWaypoints()
        {
            nodes.Clear();

            nodes.Add(new NodeData()
            {
                Position = GetPosition(true),
                Rotation = GetRotation(true) * Quaternion.Euler(0, 180, 0) // Node rotation should be in opposite direction of segment rotation
            });

            nodes.Add(new NodeData()
            {
                Position = GetPosition(false),
                Rotation = GetRotation(false) // Last node in opposite rotation by default, so we don't have to flip it
            });

            for (int i = 0; i < laneCount; i++)
            {
                var offset = laneWidth / 2 + laneWidth * i;

                // Forward lane
                GenerateLaneWaypoints(offset, true);

                // Backward lane
                GenerateLaneWaypoints(-offset, false);
            }

            EditorSaver.SetObjectDirty(this);
        }

        private void GenerateLaneWaypoints(float offset, bool forward)
        {
            var length = SplineUtility.CalculateLength(splineContainer.Spline, transform.localToWorldMatrix);
            var step = waypointInterval / length;

            PathData pathData = new PathData();

            pathData.SpeedLimit = speedLimit;

            float lastT = 0;

            for (float t = 0; t <= 1; t += step)
            {
                AddWaypoint(offset, t, pathData);
                lastT = t;
            }

            if (lastT != 1)
            {
                AddWaypoint(offset, 1, pathData);
            }

            if (forward)
            {
                nodes[0].Paths.Add(pathData);
            }
            else
            {
                pathData.Waypoints.Reverse();
                nodes[1].Paths.Add(pathData);
            }
        }

        private void AddWaypoint(float offset, float t, PathData pathData)
        {
            // Evaluate position and rotation on spline
            SplineUtility.Evaluate(splineContainer.Spline, t, out var position, out var tangent, out var up);

            // Offset the position for the lane
            Vector3 lanePosition = (Vector3)position + (Quaternion.LookRotation(tangent) * Vector3.right * offset);

            // Convert from local space to world space
            lanePosition = LocalToWorld(lanePosition);
            pathData.Waypoints.Add(lanePosition);
        }

        private Vector3 LocalToWorld(Vector3 position) => transform.TransformPoint(position);

        private Vector3 GetPosition(bool start)
        {
            if (start)
                return LocalToWorld(splineContainer.Spline.Knots.ElementAt(0).Position);
            else
                return LocalToWorld(splineContainer.Spline.Knots.Last().Position);
        }

        private Quaternion GetRotation(bool start)
        {
            float t = start ? 0 : 1;
            return Quaternion.LookRotation(
                transform.TransformDirection((SplineUtility.EvaluateTangent(splineContainer.Spline, t))));
        }

        private void Reset()
        {
            splineContainer = GetComponent<SplineContainer>();
            EditorSaver.SetObjectDirty(this);
        }
#endif
    }
}