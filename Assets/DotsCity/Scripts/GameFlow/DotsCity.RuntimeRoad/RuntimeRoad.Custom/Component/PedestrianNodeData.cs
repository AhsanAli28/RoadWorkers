using Spirit604.Gameplay.Road;
using System;
using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad.Custom
{
#if UNITY_EDITOR
    [Serializable]
#endif
    public class PedestrianNodeData : IEquatable<PedestrianNodeData>
    {
        public int RuntimeID;
        public RuntimeSegmentCustom Owner;
        public PedestrianNodeData Previous;
        public PedestrianNodeData Next;
        public Vector3 Position;
        public int ConnectedNodeIndex = -1;
        public int LightIndex = -1;
        public PedestrianNodeType PedestrianNodeType = PedestrianNodeType.Default;
        public NodeShapeType NodeShapeType = NodeShapeType.Circle;
        public bool CanSpawnInView = false;
        public float ChanceToSpawn = 1f;
        public float Weight = 1f;
        public float CustomAchieveDistance = 0f;
        public float Width = 1f;
        public float Height = 2f;
        public bool HasMovementRandomOffset = true;

        public bool CrosswalkNode => ConnectedNodeIndex != -1;

        public bool Equals(PedestrianNodeData other) => this.RuntimeID == other.RuntimeID;

        public override int GetHashCode() => RuntimeID;
    }
}
