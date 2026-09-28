using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct RaycastConfig
    {
        public float SideOffset;
        public float MaxRayLength;
        public float MaxRayLengthSQ;
        public float MinRayLength;
        public float BoxCastHeight;
        public float RayYaxisOffset;
        public float DotDirection;
        public float BoundsMultiplier;
        public float2 CastFrequency;
        public uint RaycastFilter;
    }

    public struct RaycastConfigReference : IComponentData
    {
        public BlobAssetReference<RaycastConfig> Config;
    }
}