using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct DestinationConfig
    {
        public float AchieveDistanceSQ;
        public bool IgnorePreviousDst;
    }

    public struct DestinationConfigReference : IComponentData
    {
        public BlobAssetReference<DestinationConfig> Config;
    }
}