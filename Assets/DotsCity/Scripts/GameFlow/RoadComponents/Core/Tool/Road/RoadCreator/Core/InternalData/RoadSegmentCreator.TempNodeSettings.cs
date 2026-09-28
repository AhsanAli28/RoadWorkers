using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public partial class RoadSegmentCreator : MonoBehaviour
    {
        [Serializable]
        public class TempNodeSettings
        {
            [Range(1, 20)] public int LaneCount = 1;

            [Range(0.1f, 20f)] public float LaneWidth = 4f;

            public TrafficNodeType TrafficNodeType = TrafficNodeType.Default;
            public bool IsOneWay;
            public bool IsEndOfOneWay;
            public bool HasPedestrianNodes = true;
            public bool HasCrosswalk = true;
            public bool LockAutoPathCreation;

            public TempNodeSettings(TrafficNode trafficNode, bool hasPedestrianNodes = true)
            {
                LaneCount = trafficNode.LaneCount;
                LaneWidth = trafficNode.LaneWidth;
                TrafficNodeType = trafficNode.TrafficNodeType;
                IsOneWay = trafficNode.IsOneWay;
                IsEndOfOneWay = trafficNode.IsEndOfOneWay;
                HasPedestrianNodes = hasPedestrianNodes;
                HasCrosswalk = trafficNode.HasCrosswalk;
                LockAutoPathCreation = trafficNode.LockPathAutoCreation;
            }

            public void InstallSettings(TrafficNode trafficNode)
            {
                trafficNode.LaneCount = LaneCount;
                trafficNode.LaneWidth = LaneWidth;
                trafficNode.TrafficNodeType = TrafficNodeType;
                trafficNode.IsOneWay = IsOneWay;
                trafficNode.IsEndOfOneWay = IsEndOfOneWay;
                trafficNode.HasCrosswalk = HasCrosswalk;
                trafficNode.LockPathAutoCreation = LockAutoPathCreation;
                EditorSaver.SetObjectDirty(trafficNode);
            }
        }
    }
}