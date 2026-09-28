using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct TrafficNodeLinkedComponent : ICleanupComponentData
    {
        public Entity LinkedPlace;
    }
}