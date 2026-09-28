using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct TrafficPrefabData : IComponentData
    {
        public int ModelIndex;
        public Entity PrefabEntity;
        public float Weight;
    }

    public struct TrafficPrefabSort : ISharedComponentData
    {
        public EntityType TrafficEntityType;
    }

    [TemporaryBakingType]
    public struct CarModelBakingData : IComponentData
    {
        public Entity VehicleEntity;
        public int CarModel;
        public int LocalIndex;
    }

    [TemporaryBakingType]
    public struct CustomSettingsBakingData : IComponentData
    {
        public Entity VehicleEntity;
        public float MaxCarSpeed;
        public float MaxSteeringAngle;
    }
}
