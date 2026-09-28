using System;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public partial class RoadSegmentCreator : MonoBehaviour
    {
        [Serializable]
        public class CustomTurnData
        {
            public int LeftTurnCount = 1;
            public int RightTurnCount = 1;
            public int LaneLeftTurnConnectionCount = 1;
            public int LaneRightTurnConnectionCount = 1;

            public CustomTurnData()
            {
                LeftTurnCount = 1;
                RightTurnCount = 1;
                LaneLeftTurnConnectionCount = 1;
                LaneRightTurnConnectionCount = 1;
            }
        }
    }
}