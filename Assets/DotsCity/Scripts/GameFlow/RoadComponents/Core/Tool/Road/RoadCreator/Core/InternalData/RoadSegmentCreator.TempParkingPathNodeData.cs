using Spirit604.Gameplay.Road;
using System;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public partial class RoadSegmentCreator
    {
        [Serializable]
        public class TempParkingPathNodeData
        {
            public Vector3 LocalPosition;
            public float SpeedLimit;
            public bool BackwardDirection;
            public TrafficGroupMask TrafficGroupMask;
        }
    }
}