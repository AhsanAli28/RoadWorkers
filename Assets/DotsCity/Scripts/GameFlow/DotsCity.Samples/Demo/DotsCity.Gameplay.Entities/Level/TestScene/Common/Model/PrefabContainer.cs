using Spirit604.DotsCity.Simulation.Car;
using Unity.Entities;

namespace Spirit604.DotsCity.TestScene
{
    public struct PrefabContainer : IComponentData
    {
        public Entity Entity;
        public bool HasInput;
        public bool CleanSound;
        public bool AddPoolable;
    }

    public struct PrefabContainerSort : ISharedComponentData
    {
        public VehicleOwnerType OwnerType;
    }

    [TemporaryBakingType]
    public struct TrafficComponentCleanerTag : IComponentData
    {
        public bool CleanSound;
        public bool AddPoolable;
    }

    public struct SpawnPointTag : IComponentData
    {
    }

    public struct FinishPointTag : IComponentData
    {
    }

    public struct FirstRowVehicleTag : IComponentData
    {
    }

    public struct LastRowVehicleTag : IComponentData
    {
    }

    public struct SpawnPointSettings : IComponentData
    {
        public int Rows;
        public int CountPerRow;
        public float XOffset;
        public float ZOffset;
    }
}