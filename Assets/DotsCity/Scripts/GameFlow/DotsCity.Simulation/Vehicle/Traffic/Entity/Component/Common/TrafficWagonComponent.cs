using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct TrafficWagonComponent : IComponentData
    {
        public Entity OwnerEntity;
    }
}