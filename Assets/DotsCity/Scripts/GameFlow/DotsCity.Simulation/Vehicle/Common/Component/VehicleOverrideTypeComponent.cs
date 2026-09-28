using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Car
{
    public struct VehicleOverrideTypeComponent : IComponentData
    {
        public EntityType EntityType;
    }
}