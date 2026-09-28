using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct TrafficCustomDestinationComponent : IComponentData
    {
        public float3 Destination;
        public float3 PreviousDestination;
        public float PreviousSpeedLimit;
        public bool Init;
        public bool CustomProcess;
        public bool Passed;
        public bool BackwardDirection;
        public float AchieveDistance;
        public float CustomDuration;
        public float DisableTimestamp;
        public VehicleBoundsPoint VehicleBoundsPoint;
    }

    public enum VehicleBoundsPoint { SourcePosition, ForwardPoint, BackwardPoint }
}