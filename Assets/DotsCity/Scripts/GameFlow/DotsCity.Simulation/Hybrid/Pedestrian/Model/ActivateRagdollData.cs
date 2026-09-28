using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct ActivateRagdollData
    {
        public Entity Entity;
        public int SkinIndex;
        public float3 Position;
        public quaternion Rotation;
        public float3 ForceDirection;
        public float ForceMultiplier;
        public int DamageDealerID;

        // Current animation data
        public bool HybridSkin;
        public int AnimationHash;
        public int StateLayer;
        public float NormalizedTime;
        public int ParamHash;
        public AnimParamType ParamType;
        public float ParamValue;
    }
}