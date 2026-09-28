using Spirit604.Gameplay.Road;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Road
{
    public struct RouteNodeData
    {
        public float3 Position;
        public float SpeedLimit;
        public bool ForwardNodeDirection;
        public TrafficGroupType TrafficGroup;

        public PathForwardType ForwardNodeDirectionType => ForwardNodeDirection ? PathForwardType.Forward : PathForwardType.Backward;
    }
}