using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct TrafficChangingLaneEventTag : IComponentData, IEnableableComponent
    {
    }

    public struct TrafficChangeLaneRequestedPositionComponent : IComponentData
    {
        public float3 Destination;
        public int TargetPathKey;
        public int TargetPathNodeIndex;
        public Entity TargetSourceLaneEntity;
    }

    public struct TrafficWaitForChangeLaneTag : IComponentData { }
}