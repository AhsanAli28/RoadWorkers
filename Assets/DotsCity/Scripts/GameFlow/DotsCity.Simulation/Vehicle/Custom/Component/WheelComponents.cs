using Spirit604.Extensions;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Collider = Unity.Physics.Collider;

namespace Spirit604.DotsCity.Simulation.Car.Custom
{
    public struct Wheel : IComponentData
    {
        public Entity VehicleEntity;
        public float Radius;
        public float Width;
        public float ApplyImpulseOffset;
        public float SuspensionLength;
        public float Inertia;
        public float ForwardFrictionValue;
        public float LateralFrictionValue;
        public float BrakeFrictionValue;
        public BlobAssetReference<Collider> Collider;
        public CastType CastType;
        public CollisionFilter CastFilter;
        public float PowerSteering;
    }

    public struct WheelOrigin : IComponentData
    {
        public RigidTransform Value;
        public float3 Offset;
        public int InversionValue; //1 identity local rotation, -1 flipped on the Y-axis
    }

    public struct WheelInput : IComponentData
    {
        public float3 Up;
        public RigidTransform LocalTransform;
        public RigidTransform LocalToWorld;
        public float MassMultiplier;
        public float Torque;
        public float Brake;
        public float Handbrake;
    }

    public struct WheelContact : IComponentData
    {
        public bool IsInContact;
        public float3 Point;
        public float3 Normal;
        public float CurrentSuspensionLength;
    }

    public struct WheelSuspension : IComponentData
    {
        public float Stiffness;
        public float Damping;
    }

    public struct WheelContactVelocity : IComponentData
    {
        public float3 Value;
    }

    public struct WheelOutput : IComponentData
    {
        public float3 SuspensionImpulse;
        public float3 FrictionImpulse;
        public float Rotation;
        public float RotationSpeed;
        public float LastLength;
        public float LateralForce;
        public float LongTransientForce;
    }

    public struct WheelFriction : IComponentData
    {
        public BlobAssetReference<AnimationCurveBlob> Longitudinal;
        public BlobAssetReference<AnimationCurveBlob> Lateral;
    }

    public struct WheelControllable : IComponentData
    {
        public float MaxSteeringAngle;
        public float DriveRate;
        public float BrakeRate;
        public float HandbrakeRate;
    }

    public struct WheelBrakes : IComponentData
    {
        public float BrakeTorque;
        public float HandbrakeTorque;
    }
}
