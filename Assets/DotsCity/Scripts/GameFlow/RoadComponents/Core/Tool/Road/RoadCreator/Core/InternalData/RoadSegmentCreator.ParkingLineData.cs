using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public partial class RoadSegmentCreator : MonoBehaviour
    {
        [Serializable]
        public class ParkingLineData
        {
            public ParkingLineSettings ParkingLineSettings;
            public TrafficNode SourceTrafficNode;
            public TrafficNode TargetTrafficNode;
            public Path SourcePath;
            public TempParkingPathData SavedEnterPath;
            public TempParkingPathData SavedExitPath;
            public List<TempParkingPathOffsetData> EnterPathOffsets;
            public List<TempParkingPathOffsetData> ExitPathOffsets;
            public List<TrafficNode> LineData = new List<TrafficNode>();
        }
    }
}