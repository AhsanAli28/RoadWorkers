using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Navigation
{
    public struct PathAgentConfig
    {
        public int AgentCount;
        public int BufferSize;
    }

    public struct PedestrianPathAgentConfigReference : IComponentData
    {
        public BlobAssetReference<PathAgentConfig> Config;
    }

    public struct TrafficPathAgentConfigReference : IComponentData
    {
        public BlobAssetReference<PathAgentConfig> Config;
    }
}
