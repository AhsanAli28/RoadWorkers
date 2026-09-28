using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.Samples.RuntimeCustomRoad
{
    [Serializable]
    public class PathData
    {
        public List<Vector3> Waypoints = new();
        public float SpeedLimit;
    }
}