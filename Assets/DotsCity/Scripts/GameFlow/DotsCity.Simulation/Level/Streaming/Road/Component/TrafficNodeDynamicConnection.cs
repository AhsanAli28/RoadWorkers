using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Level.Streaming
{
    public struct TrafficNodeDynamicConnection : IComponentData
    {
        public ConnectionType ConnectionType;
        public int SegmentHash;
        public int PositionHash;
        public int LaneIndex;
        public bool SubNode;
    }
}
