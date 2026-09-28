using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Car
{
    [InternalBufferCapacity(0)]
    public struct VehicleWheel : IBufferElementData
    {
        public Entity WheelEntity;
    }
}
