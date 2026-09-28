using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Road
{
    public struct TrafficRoadConfig
    {
        public int IsAvailableForRouteRandomizeSpawningFlags;
        public int IsAvailableForSpawnFlags;
        public int IsAvailableForSpawnTargetFlags;
        public int LinkedNodeFlags;
    }

    public struct TrafficRoadConfigReference : IComponentData
    {
        public BlobAssetReference<TrafficRoadConfig> Config;
    }
}