using Spirit604.Extensions;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;

namespace Spirit604.DotsCity.Simulation.Car.Custom.Authoring
{
    [TemporaryBakingType]
    public struct VehicleBakingData : IComponentData
    {
        public Entity VehicleEntity;
        public NativeArray<Entity> SteeringWheels;
        public NativeArray<VehicleWheelBaking> AllWheels;

        public float MaxSteeringAngle;
        public float PowerSteering;
        public float WheelMass;
        public float Radius;
        public float Width;
        public float ApplyImpulseOffset;
        public float SuspensionLength;
        public float Inertia;

        public float ForwardFrictionValue;
        public float LateralFrictionValue;
        public float BrakeFrictionValue;

        public float Stiffness;
        public float Damping;

        public BlobAssetReference<AnimationCurveBlob> Longitudinal;
        public BlobAssetReference<AnimationCurveBlob> Lateral;
        public float BrakeTorque;
        public float HandbrakeTorque;
        public BlobAssetReference<Unity.Physics.Collider> WheelCollider;
        public CastType CastType;
        public CollisionFilter CastFilter;
        public bool ShowDebug;
    }
}