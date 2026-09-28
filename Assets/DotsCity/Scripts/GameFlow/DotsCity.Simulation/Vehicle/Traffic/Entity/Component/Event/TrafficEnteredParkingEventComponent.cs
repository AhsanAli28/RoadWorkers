using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct TrafficEnteringTriggerNodeTag : IComponentData, IEnableableComponent { }

    public struct TrafficEnteredTriggerNodeTag : IComponentData, IEnableableComponent { }

    public struct TrafficAccurateAligmentCustomMovementTag : IComponentData { }
}