using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.Samples.RuntimeCustomRoad
{
    /// <summary>
    /// An example class that contains data for all lanes in the current road segment.
    /// </summary>
    public class RoadBase : MonoBehaviour
    {
        [Serializable]
        public class NodeData
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public List<PathData> Paths = new();
            public RoadBase ConnectedRoad;
        }

        public float laneWidth = 4f; // Distance between lanes
        public int laneCount = 2;

        public List<NodeData> nodes = new();

        protected void DrawRoutes(List<PathData> lanes)
        {
            for (int i = 0; i < lanes.Count; i++)
            {
                DrawRoute(lanes[i]);
            }
        }

        protected void DrawRoute(PathData pathData)
        {
            for (int i = 0; i < pathData.Waypoints.Count - 1; i++)
            {
                var a1 = pathData.Waypoints[i];
                var a2 = pathData.Waypoints[i + 1];
                Gizmos.DrawLine(a1, a2);
            }
        }

        private void OnDrawGizmosSelected()
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                DrawRoutes(nodes[i].Paths);
            }
        }
    }
}