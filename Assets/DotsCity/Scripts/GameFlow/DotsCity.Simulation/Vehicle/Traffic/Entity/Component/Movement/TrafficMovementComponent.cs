using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct TrafficMovementComponent : IComponentData
    {
        public float3 LinearVelocity;
        public float3 AngularVelocity;
        public float3 TargetDirection;
        public quaternion CurrentCalculatedRotation;
        public float SteeringAngle;
        public float DesiredSteeringAngle;
        public float TargetSpeed;
        public int CurrentMovementDirection;
    }
}