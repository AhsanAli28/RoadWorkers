using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Level.Streaming
{
    public struct RoadStreamingConfig
    {
        public bool StreamingIsEnabled;
        public bool IgnoreY;
        public float DistanceForStreamingInSQ;
        public float DistanceForStreamingOutSQ;
        public float SectionCellSize;
        public float NodeCellSize;
    }

    public struct RoadStreamingConfigReference : IComponentData
    {
        public BlobAssetReference<RoadStreamingConfig> Config;
    }
}