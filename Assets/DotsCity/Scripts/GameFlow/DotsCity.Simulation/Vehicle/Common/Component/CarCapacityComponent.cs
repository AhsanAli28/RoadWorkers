using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Car
{
    public struct CarCapacityComponent : IComponentData
    {
        public int MaxCapacity;
        public int AvailableCapacity;

        public int EnteredCount => MaxCapacity - AvailableCapacity;
    }

    [InternalBufferCapacity(0)]
    public struct VehicleEntryElement : IBufferElementData
    {
        public Entity EntryPointEntity;
        public bool RightSide;
    }
}