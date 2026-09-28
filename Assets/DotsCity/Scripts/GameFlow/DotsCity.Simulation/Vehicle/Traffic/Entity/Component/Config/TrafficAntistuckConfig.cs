using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct TrafficAntistuckConfig
    {
        public float ObstacleStuckTime;
        public float StuckDistanceDiff;
        public bool CullOutOfTheCameraOnly;
    }

    public struct TrafficAntistuckConfigReference : IComponentData
    {
        public BlobAssetReference<TrafficAntistuckConfig> Config;
    }
}