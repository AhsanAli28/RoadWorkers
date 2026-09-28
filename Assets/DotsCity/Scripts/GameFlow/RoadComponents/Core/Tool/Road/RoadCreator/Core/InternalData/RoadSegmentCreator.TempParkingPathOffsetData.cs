using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public partial class RoadSegmentCreator : MonoBehaviour
    {
        [Serializable]
        public class TempParkingPathOffsetData
        {
            public List<Vector3> Offsets;
        }
    }
}