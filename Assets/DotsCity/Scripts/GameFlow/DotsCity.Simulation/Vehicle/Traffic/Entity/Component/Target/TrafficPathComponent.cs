using Spirit604.Gameplay.Road;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct TrafficPathComponent : IComponentData
    {
        public float3 DestinationWayPoint;
        public float3 PreviousDestination;
        public int LocalPathNodeIndex;
        public int CurrentGlobalPathIndex;
        public PathForwardType PathDirection;
        public int Priority;

        public int SourceLocalNodeIndex => math.clamp(LocalPathNodeIndex - 1, 0, int.MaxValue);
    }
}