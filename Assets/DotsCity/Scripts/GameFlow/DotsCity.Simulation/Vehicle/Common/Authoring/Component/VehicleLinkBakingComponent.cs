using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Car.Authoring
{
    [TemporaryBakingType]
    public struct VehicleLinkBakingComponent : IComponentData
    {
        public NativeArray<Entity> LinkedEntities;
    }
}