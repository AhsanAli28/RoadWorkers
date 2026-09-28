using Spirit604.Extensions;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car.Custom.Authoring
{
    public class VehicleAuthoringBaker : Baker<VehicleAuthoring>
    {
        public override void Bake(VehicleAuthoring authoring)
        {
            var vehicleEntity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

            AddComponent(vehicleEntity, new ComponentTypeSet(
                typeof(VehicleOutput)
                ));

            var wheelBuffer = AddBuffer<VehicleWheel>(vehicleEntity);

            var torque = AnimationCurveBlob.Build(authoring.Torque, 128, Allocator.Persistent);

            AddBlobAsset(ref torque, out var hash);

            AddComponent(vehicleEntity, new VehicleInput()
            {
            });

            AddComponent(vehicleEntity, new VehicleEngine
            {
                Torque = torque,
                TransmissionRate = authoring.TransmissionRate
            });

            NativeList<Entity> steeringWheels = new NativeList<Entity>(Allocator.Temp);
            NativeList<VehicleWheelBaking> allWheels = new NativeList<VehicleWheelBaking>(Allocator.Temp);

            for (int i = 0; i < authoring.SteeringWheels.Count; i++)
            {
                if (!authoring.SteeringWheels[i])
                    continue;

                var steeringEntity = GetEntity(authoring.SteeringWheels[i], TransformUsageFlags.Dynamic);
                steeringWheels.Add(steeringEntity);
            }

            for (int i = 0; i < authoring.AllWheels.Count; i++)
            {
                var wheelData = authoring.AllWheels[i];

                if (!wheelData.Wheel)
                    continue;

                var wheelEntity = GetEntity(wheelData.Wheel, TransformUsageFlags.Dynamic);

                var localPoint = authoring.gameObject.transform.InverseTransformPoint(wheelData.Wheel.transform.position);
                var localRotation = Quaternion.Euler(0, wheelData.Wheel.transform.localRotation.eulerAngles.y, 0);

                var origin = new RigidTransform(localRotation, localPoint);
                var wheelOffset = localPoint - wheelData.Wheel.transform.localPosition;

                origin.pos += (float3)authoring.GetSuspensionOffset();

                allWheels.Add(new VehicleWheelBaking()
                {
                    Entity = wheelEntity,
                    DriveRate = wheelData.DrivingValue,
                    BrakeRate = wheelData.BrakeValue,
                    HandbrakeRate = wheelData.HandBrakeValue,
                    Origin = origin,
                    WheelOffset = wheelOffset,
                    InversionValue = Mathf.Abs(wheelData.Wheel.transform.localRotation.eulerAngles.y) < 1f ? 1 : -1
                });

                wheelBuffer.Add(new VehicleWheel()
                {
                    WheelEntity = wheelEntity
                });
            }

            var longitudinal = AnimationCurveBlob.Build(authoring.Longitudinal, 128, Allocator.Persistent);
            var lateral = AnimationCurveBlob.Build(authoring.Lateral, 128, Allocator.Persistent);
            var steeringLimitCurve = AnimationCurveBlob.Build(authoring.SteeringLimitCurve, 128, Allocator.Persistent);

            var castFilter = new CollisionFilter()
            {
                BelongsTo = authoring.CastLayer.Value,
                CollidesWith = authoring.CastLayer.Value,
            };

            var wheelCollider = CylinderCollider.Create(new CylinderGeometry
            {
                Center = float3.zero,
                Height = authoring.Width,
                Radius = authoring.Radius,
                BevelRadius = 0.1f,
                SideCount = 12,
                Orientation = quaternion.AxisAngle(math.up(), math.PI * 0.5f),
            }, castFilter);

            this.AddBlobAsset(ref longitudinal, out var hash1);
            this.AddBlobAsset(ref lateral, out var hash2);
            this.AddBlobAsset(ref steeringLimitCurve, out var hash3);
            this.AddBlobAsset(ref wheelCollider, out var wheelHash);

            AddComponent(vehicleEntity, new CustomVehicleData()
            {
                Drag = authoring.Drag,

                UseForwardTransientForce = authoring.UseForwardTransientForce,
                MinTransientForwardSpeed = authoring.MinTransientForwardSpeed,
                MaxForwardFriction = authoring.MaxForwardFrictionRate * authoring.ForwardFriction,
                ForwardRelaxMultiplier = authoring.ForwardRelaxMultiplier,

                UseLateralTransientForce = authoring.UseLateralTransientForce,
                MinTransientLateralSpeed = authoring.MinTransientLateralSpeed,
                MaxLateralFriction = authoring.MaxLateralFrictionRate * authoring.LateralFriction,
                LateralRelaxMultiplier = authoring.LateralRelaxMultiplier,
                CustomSteeringLimit = authoring.CustomSteeringLimit,
                SteeringLimitCurve = steeringLimitCurve,
            });

            AddComponent(vehicleEntity, new CustomSteeringData(authoring.MaxSteeringAngle));

            AddComponent(vehicleEntity, new VehicleBakingData
            {
                VehicleEntity = vehicleEntity,
                SteeringWheels = steeringWheels.ToArray(Allocator.Temp),
                AllWheels = allWheels.ToArray(Allocator.Temp),
                WheelMass = authoring.WheelMass,
                MaxSteeringAngle = authoring.MaxSteeringAngle,
                PowerSteering = authoring.PowerSteering,
                Radius = authoring.Radius,
                Width = authoring.Width,
                ApplyImpulseOffset = authoring.ApplyImpulseOffset,
                SuspensionLength = authoring.SuspensionLength,
                Inertia = authoring.WheelMass * authoring.Radius * authoring.Radius * 0.5f,
                ForwardFrictionValue = authoring.ForwardFriction,
                LateralFrictionValue = authoring.LateralFriction,
                BrakeFrictionValue = authoring.BrakeFriction,

                Stiffness = authoring.Stiffness,
                Damping = authoring.Damping,

                Longitudinal = longitudinal,
                Lateral = lateral,

                BrakeTorque = authoring.BrakeTorque,
                HandbrakeTorque = authoring.HandbrakeTorque,

                WheelCollider = wheelCollider,
                CastType = authoring.CastType,
                CastFilter = castFilter,
                ShowDebug = authoring.ShowDebug
            });

            steeringWheels.Dispose();
            allWheels.Dispose();
        }
    }
}