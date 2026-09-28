using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct TrafficAvoidanceComponent : IComponentData
    {
        public AvoidanceState State;
    }

    public enum AvoidanceState { Default, WaitingForBackwardDestination }
}