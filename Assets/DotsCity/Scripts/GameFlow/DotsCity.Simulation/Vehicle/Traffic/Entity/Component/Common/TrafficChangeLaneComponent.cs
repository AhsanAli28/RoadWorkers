using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct TrafficChangeLaneComponent : IComponentData
    {
        public float CheckTimeStamp;
        public float RandomEventTimeStamp;
        public Entity TargetSourceLaneNodeEntity;
        public int TargetPathGlobalIndex;
        public int TargetLocalNodeIndex;
        public float3 Destination;

        public float DistanceToOtherCarsInNeighborLane;
        public float TargetCarDirection;

        public bool ReachedTarget;
    }

    public struct TrafficChangeLaneDebugInfoComponent : IComponentData
    {
        public float RemainDistanceToEndOfPath;
        public int CurrentLaneCarCount;
        public int NeighborLaneCarCount;
        public int ShouldChangeLane;
    }
}