using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Npc
{
    public struct RagdollComponent : IComponentData
    {
        public float3 Position;
        public quaternion Rotation;
        public float3 ForceDirection;
        public float ForceMultiplier;
        public bool Activated;
        public int DamageDealerID;
    }
}