using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Road
{
    public struct TrafficRoadOverlapConfig
    {
        public TrafficNodeCalculateOverlapSystem.CalculateMethod CalculateMethod;
        public float SizeMultiplier;
    }

    public struct TrafficRoadOverlapConfigReference : IComponentData
    {
        public BlobAssetReference<TrafficRoadOverlapConfig> Config;
    }
}