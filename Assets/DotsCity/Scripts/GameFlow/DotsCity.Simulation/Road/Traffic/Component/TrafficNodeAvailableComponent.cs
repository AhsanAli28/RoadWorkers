using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Road
{
    public struct TrafficNodeAvailableComponent : IComponentData
    {
        public bool IsAvailable;
    }
}