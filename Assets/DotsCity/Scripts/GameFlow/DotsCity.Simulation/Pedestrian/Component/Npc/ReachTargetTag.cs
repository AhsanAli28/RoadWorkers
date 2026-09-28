using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    /// <summary>
    /// Is activated when the pedestrian has reached the set target.
    /// </summary>
    public struct ReachTargetTag : IComponentData, IEnableableComponent { }
}
