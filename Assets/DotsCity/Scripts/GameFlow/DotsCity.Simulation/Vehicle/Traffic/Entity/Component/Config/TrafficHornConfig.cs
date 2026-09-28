using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic.Sound
{
    public struct TrafficHornConfig
    {
        public float ChanceToStart;
        public float IdleTimeToStart;
        public float MaxDelay;
        public float MinDelay;
        public float MaxHornDuration;
        public float MinHornDuration;
    }

    public struct TrafficHornConfigReference : IComponentData
    {
        public BlobAssetReference<TrafficHornConfig> Config;
    }
}
