using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct TrafficCustomDestinationConfig
    {
        public float DefaultSpeedLimit;
        public bool CheckSidePoint;
        public float SidePointSpeedLimit;
        public float SidePointDistance;
        public float DefaultAchieveDistance;
        public float DefaultDuration;
    }

    public struct TrafficCustomDestinationConfigReference : IComponentData
    {
        public BlobAssetReference<TrafficCustomDestinationConfig> Config;
    }
}
