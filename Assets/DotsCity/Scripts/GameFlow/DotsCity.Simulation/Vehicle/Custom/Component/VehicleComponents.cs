using Spirit604.Extensions;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Car.Custom
{
    public struct CustomVehicleData : IComponentData
    {
        public float Drag;

        public bool UseForwardTransientForce;
        public float MinTransientForwardSpeed;
        public float MaxForwardFriction;
        public float ForwardRelaxMultiplier;

        public bool UseLateralTransientForce;
        public float MinTransientLateralSpeed;
        public float MaxLateralFriction;
        public float LateralRelaxMultiplier;

        public BlobAssetReference<AnimationCurveBlob> SteeringLimitCurve;
        public bool CustomSteeringLimit;
    }

    public struct CustomSteeringData : IComponentData
    {
        /// <summary> Max steering angle of the vehicle in the radians. </summary>
        public float MaxSteeringAngle;

        public CustomSteeringData(float angleDegree)
        {
            MaxSteeringAngle = math.radians(angleDegree);
        }
    }

    public struct VehicleInput : IComponentData
    {
        public float Steering;
        public float Throttle;
        public float Brake;
        public float Handbrake;
        public ThrottleMode ThrottleMode;
    }

    public enum CastType
    {
        Ray,
        Collider
    }

    public enum ThrottleMode
    {
        AccelerationForward,
        AccelerationBackward,
        Braking
    }

    public struct VehicleEngine : IComponentData
    {
        public BlobAssetReference<AnimationCurveBlob> Torque;
        public float TransmissionRate;
    }

    public struct VehicleOutput : IComponentData, IEnableableComponent
    {
        public float MaxWheelRotationSpeed;
        public float3 LocalVelocity;
        public float BlockTime;
    }
}