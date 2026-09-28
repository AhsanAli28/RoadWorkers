using Spirit604.CityEditor.Utils;
using System;
using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad.Custom
{
#if UNITY_EDITOR
    [Serializable]
#endif
    public class TrafficNodeData : IEquatable<TrafficNodeData>
    {
        public RuntimeSegmentCustom Owner;
        public Vector3 Position;
        public Quaternion Rotation;
        public int LaneCount = 1;
        public float LaneWidth = 4f;
        public float Offset;
        public float DividerWidth;
        public bool IsOneWay;
        public bool IsEndOfOneWay;
        public bool AddPedestrianNodes = true;
        public bool AddCrosswalk = true;
        public Vector3 CrosswalkOffset;
        public int LightIndex = -1;

        public TrafficNodeData ConnectedExternalNode;

#if UNITY_EDITOR
        public RuntimeSegmentCustom ConnectedExternalSegment;
#endif

        public int LocalNodeIndex;

        /// <summary> Min local path index of the RuntimeSegmentCustom (included).</summary>
        public int MinLocalPathIndex;

        /// <summary> Max local path index of the RuntimeSegmentCustom (excluded).</summary>
        public int MaxLocalPathIndex;

        public int CrosswalkIndex = -1;
        public bool InverseCrosswalk;

        public TrafficNodeData(RuntimeSegmentCustom owner, int localNodeIndex, RoadSceneUtils.NodeSettings nodeSettings)
        {
            Owner = owner;
            Position = nodeSettings.Position;
            Rotation = nodeSettings.Rotation;
            LaneCount = nodeSettings.Settings.LaneCount;
            LaneWidth = nodeSettings.Settings.LaneWidth;
            DividerWidth = nodeSettings.Settings.DividerWidth;
            IsOneWay = nodeSettings.Settings.IsOneWay;
            IsEndOfOneWay = nodeSettings.Settings.IsEndOfOneWay;
            AddPedestrianNodes = nodeSettings.Settings.AddPedestrianNodes;
            AddCrosswalk = nodeSettings.Settings.AddCrosswalk;
            CrosswalkOffset = nodeSettings.Settings.CrosswalkOffset;
            LocalNodeIndex = localNodeIndex;
        }

        public Vector3 Forward => Rotation * Vector3.forward;

        public Vector3 Right => Rotation * Vector3.right;

        public bool Equals(TrafficNodeData other)
        {
            return this.Owner == other.Owner && this.LocalNodeIndex == other.LocalNodeIndex;
        }

        public override int GetHashCode()
        {
            return Owner.GetHashCode() + LocalNodeIndex;
        }
    }
}
