using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct TrafficPlayerControlTag : IComponentData { }

    public struct TrafficPlayerControlInitTag : IComponentData, IEnableableComponent { }
}