using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct TrafficNavMeshLoaderConfig
    {
        public float SizeOffset;
        public bool LoadOnlyInView;
        public float LoadFrequency;
    }

    public struct TrafficNavMeshLoaderConfigReference : IComponentData
    {
        public BlobAssetReference<TrafficNavMeshLoaderConfig> Config;
    }
}
