using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct TrafficAvoidanceConfig
    {
        public float CustomAchieveDistance;
        public bool ResolveCyclicObstacle;
    }

    public struct TrafficAvoidanceConfigReference : IComponentData
    {
        public BlobAssetReference<TrafficAvoidanceConfig> Config;
    }
}
