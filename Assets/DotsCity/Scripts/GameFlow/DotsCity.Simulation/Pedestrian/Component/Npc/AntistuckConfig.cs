using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct AntistuckConfig
    {
        public bool AntistuckEnabled;
        public float TargetDirectionDot;
        public float AchieveDistanceSQ;
        public float TargetPointOffset;
    }

    public struct AntistuckConfigReference : IComponentData
    {
        public BlobAssetReference<AntistuckConfig> Config;
    }
}
