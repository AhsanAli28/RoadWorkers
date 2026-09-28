using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct TrafficStuckInfoComponent : IComponentData
    {
        public float SavedRemainDistance;
        public float SavedTimestamp;
    }
}