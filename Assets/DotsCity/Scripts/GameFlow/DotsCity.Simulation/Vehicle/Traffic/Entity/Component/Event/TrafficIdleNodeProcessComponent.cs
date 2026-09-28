using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct TrafficIdleNodeProcessComponent : IComponentData
    {
        public bool Activated;
        public float DeactivateTimestamp;
    }
}
