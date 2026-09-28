using Spirit604.Gameplay.Road;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct TrafficTypeComponent : IComponentData
    {
        public TrafficGroupType TrafficGroup;
    }
}