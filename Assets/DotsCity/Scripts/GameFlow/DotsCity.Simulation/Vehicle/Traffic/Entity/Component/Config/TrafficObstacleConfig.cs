using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public enum IntersectCalculationMethod { Distance, Bounds }

    public struct TrafficObstacleConfig
    {
        public float MaxDistanceToObstacle;
        public float MinDistanceToStartApproach;
        public float MinDistanceToStartApproachSoft;
        public float MinDistanceToCheckNextPath;
        public float ShortPathLength;
        public IntersectCalculationMethod IntersectCalculation;
        public float CalculateDistanceToIntersectPoint;
        public float SizeOffsetToIntersectPoint;
        public float StopDistanceBeforeIntersection;
        public float StopDistanceForSameTargetNode;
        public float CloseDistanceToChangeLanePoint;
        public float MaxDistanceToObstacleChangeLane;
        public float InFrontOfViewDot;
        public float NeighboringDistanceSQ;
        public bool AvoidCrossroadJam;
        public bool PriorityToRight;
    }

    public struct TrafficObstacleConfigReference : IComponentData
    {
        public BlobAssetReference<TrafficObstacleConfig> Config;
    }
}
