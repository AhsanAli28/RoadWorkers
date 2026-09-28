using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Car
{
    public struct VehicleLinkComponent : IComponentData
    {
        public Entity LinkedVehicle;
    }
}