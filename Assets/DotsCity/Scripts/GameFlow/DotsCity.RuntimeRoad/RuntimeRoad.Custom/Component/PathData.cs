using Spirit604.CityEditor.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad.Custom
{
#if UNITY_EDITOR
    [System.Serializable]
#endif
    public class PathData
    {
        public List<Vector3> Waypoints = new List<Vector3>();

        /// <summary> Connected lane index, if value -1 it will be detected automatically.</summary>
        public int ConnectedLaneIndex = -1;

        /// <summary> Source lane index, if value -1 it will be detected automatically.</summary>
        public int SourceLaneIndex = -1;

        /// <summary> Connected traffic node index, if value is -1 it will be detected automatically.</summary>
        public int ConnectedNodeIndex = -1;

        /// <summary> Flag for external connection to another segment.</summary>
        public bool External;

        /// <summary> Self local index in runtime segment custom.</summary>
        public int LocalPathIndex;

        public int GlobalPathIndex;

        /// <summary> Speed limit in metres per second.</summary>
        public float SpeedLimit;

        /// <summary> Order of crossing intersected paths (vehicle with the higher priority gets through first). If value is 10, default value is generated.</summary>
        public int Priority = RoadSceneUtils.PathData.EmptyPriority;

        public Vector3 StartPosition
        {
            get
            {
                return Waypoints[0];
            }
            set
            {
                Waypoints[0] = value;
            }
        }

        public Vector3 EndPosition
        {
            get
            {
                return Waypoints[Waypoints.Count - 1];
            }
            set
            {
                Waypoints[Waypoints.Count - 1] = value;
            }
        }

        public bool DefaultPriority => Priority == RoadSceneUtils.PathData.EmptyPriority;

        public PathData(RoadSceneUtils.PathData pathData, int localPathIndex)
        {
            Waypoints = pathData.Waypoints;
            SourceLaneIndex = pathData.ConnectedLaneIndex;
            ConnectedLaneIndex = pathData.ConnectedLaneIndex;
            ConnectedNodeIndex = pathData.ConnectedNodeIndex;
            LocalPathIndex = localPathIndex;
            SpeedLimit = pathData.SpeedLimit;
            Priority = pathData.Priority;
        }
    }
}
