using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public partial class RoadSegmentCreator : MonoBehaviour
    {
        [Serializable]
        public class TempParkingPathData
        {
            public List<TempParkingPathNodeData> Nodes;
            public TrafficGroupMask trafficGroupMask;
        }
    }
}