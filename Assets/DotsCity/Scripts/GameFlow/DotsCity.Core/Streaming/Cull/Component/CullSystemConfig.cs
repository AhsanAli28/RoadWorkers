using Unity.Entities;

namespace Spirit604.DotsCity.Core
{
    public struct CullSystemConfig
    {
        public bool HasCull;
        public bool IgnoreY;
        public CullMethod CullMethod;
        public float MaxDistance;
        public float VisibleDistance;
        public float PreinitDistance;
        public float MaxDistanceSQ;
        public float VisibleDistanceSQ;
        public float PreinitDistanceSQ;

        public float ViewPortOffset;
        public float BehindMaxDistanceSQ;
        public float BehindVisibleDistanceSQ;
        public float BehindPreinitDistanceSQ;
    }

    public struct CullSystemConfigReference : IComponentData
    {
        public BlobAssetReference<CullSystemConfig> Config;
    }
}
