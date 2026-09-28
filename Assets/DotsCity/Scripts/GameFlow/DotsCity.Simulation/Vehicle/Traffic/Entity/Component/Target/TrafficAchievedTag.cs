using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    /// <summary>
    /// Activated when the traffic car reaches the destination node.
    /// </summary>
    public struct TrafficAchievedTag : IComponentData, IEnableableComponent { }

    public enum AchieveState { Default, Success, NoTarget, ChangeLane, Backward, Cull }
}
