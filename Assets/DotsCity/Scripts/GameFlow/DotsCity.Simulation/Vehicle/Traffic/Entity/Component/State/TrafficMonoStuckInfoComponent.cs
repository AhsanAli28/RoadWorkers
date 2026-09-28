using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct TrafficMonoStuckInfoComponent : IComponentData
    {
        public float SavedTimestamp;
        public float SavedRemainDistance;
        public float NextTime;
        public bool Activated;
    }
}