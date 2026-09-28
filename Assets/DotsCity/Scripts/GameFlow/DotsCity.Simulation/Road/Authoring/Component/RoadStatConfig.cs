using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Road
{
    public struct RoadStatConfig : IComponentData
    {
        public int TrafficNodeTotal;
        public int PedestrianNodeTotal;
        public int TrafficNodeStreamingTotal;
        public int TrafficNodeDynamicStreaming;
        public int TrafficNodePassiveConnection;
    }
}
