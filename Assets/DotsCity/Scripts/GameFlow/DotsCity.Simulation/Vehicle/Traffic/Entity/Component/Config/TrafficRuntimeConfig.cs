using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic.Authoring
{
    public struct TrafficRuntimeConfig : IComponentData
    {
        public EntityType EntityType;
    }
}