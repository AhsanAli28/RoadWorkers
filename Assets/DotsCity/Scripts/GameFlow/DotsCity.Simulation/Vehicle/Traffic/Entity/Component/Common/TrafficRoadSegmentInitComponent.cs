using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    /// <summary>
    /// Used to link spawned vehicle & TrafficRoadDebugger.
    /// </summary>
    public struct TrafficRoadSegmentInitComponent : IComponentData
    {
        public int HashID;
    }
}