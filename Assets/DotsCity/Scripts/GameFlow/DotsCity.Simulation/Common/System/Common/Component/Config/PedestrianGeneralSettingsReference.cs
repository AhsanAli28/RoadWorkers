using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct PedestrianGeneralSettingsData
    {
        public EntityBakingType EntityBakingType;
        public bool HasPedestrian;
        public bool NavigationSupport;
        public bool ParkingSupport;
        public bool TrafficPublicSupport;
        public bool TalkingSupport;
        public bool BenchSystemSupport;
        public bool TriggerSupport;
    }

    public struct PedestrianGeneralSettingsReference : IComponentData
    {
        public BlobAssetReference<PedestrianGeneralSettingsData> Config;
    }
}
