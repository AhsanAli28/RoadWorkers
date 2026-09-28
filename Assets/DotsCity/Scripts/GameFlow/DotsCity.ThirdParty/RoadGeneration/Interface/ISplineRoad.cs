using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.RoadGeneration
{
    public interface ISplineRoad : IRoadObject
    {
        public float Width { get; }
        public int LaneCount { get; }
        public Component StartConnectionObject { get; }
        public Component EndConnectionObject { get; }
        public List<Vector3> Points { get; }
        public bool IsAvailable { get; }
    }
}
