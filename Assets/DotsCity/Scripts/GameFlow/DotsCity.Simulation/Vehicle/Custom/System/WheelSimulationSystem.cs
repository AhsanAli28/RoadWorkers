using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Car.Custom
{
    [UpdateInGroup(typeof(PhysicsSimGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct WheelSimulationSystem : ISystem
    {
        // Min velocity to prevent lateral jittering of the vehicle during the hand braking
        private const float MinHandBrakeVelocity = 4f;

        private const float SwitchTransientMinSpeed = 1f;
        private const float SwitchTransientMinForce = 1000f;

        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<WheelHandlingTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var wheelSimulationJob = new WheelSimulationJob()
            {
                VehicleDataLookup = SystemAPI.GetComponentLookup<CustomVehicleData>(true),
                PhysicsVelocityLookup = SystemAPI.GetComponentLookup<PhysicsVelocity>(true),
                PhysicsMassLookup = SystemAPI.GetComponentLookup<PhysicsMass>(true),
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                SpeedLookup = SystemAPI.GetComponentLookup<SpeedComponent>(true),
                VehicleInputLookup = SystemAPI.GetComponentLookup<VehicleInput>(true),
                VehicleOutputLookup = SystemAPI.GetComponentLookup<VehicleOutput>(true),
                DeltaTime = SystemAPI.Time.DeltaTime,
                Time = (float)SystemAPI.Time.ElapsedTime,
            };

            wheelSimulationJob.ScheduleParallel();
        }

        [WithAll(typeof(WheelHandlingTag))]
        [BurstCompile]
        public partial struct WheelSimulationJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<CustomVehicleData> VehicleDataLookup;

            [ReadOnly]
            public ComponentLookup<PhysicsVelocity> PhysicsVelocityLookup;

            [ReadOnly]
            public ComponentLookup<PhysicsMass> PhysicsMassLookup;

            [ReadOnly]
            public ComponentLookup<LocalTransform> TransformLookup;

            [ReadOnly]
            public ComponentLookup<SpeedComponent> SpeedLookup;

            [ReadOnly]
            public ComponentLookup<VehicleInput> VehicleInputLookup;

            [ReadOnly]
            public ComponentLookup<VehicleOutput> VehicleOutputLookup;

            [ReadOnly]
            public float DeltaTime;

            [ReadOnly]
            public float Time;

            void Execute(
                ref WheelOutput output,
                ref WheelContactVelocity contactVelocity,
                in WheelContact contact,
                in Wheel wheel,
                in WheelInput input,
                in WheelSuspension suspension,
                in WheelFriction friction,
                in WheelBrakes brakes,
                in WheelControllable wheelControllable)
            {
                if (!PhysicsVelocityLookup.HasComponent(wheel.VehicleEntity))
                {
                    return;
                }

                var vehicleData = VehicleDataLookup[wheel.VehicleEntity];
                var speedComponent = SpeedLookup[wheel.VehicleEntity];
                var vehicleInput = VehicleInputLookup[wheel.VehicleEntity];
                var vehicleOutput = VehicleOutputLookup[wheel.VehicleEntity];

                if (contact.IsInContact)
                {
                    var physicsVelocity = PhysicsVelocityLookup[wheel.VehicleEntity];

                    var mass = PhysicsMassLookup[wheel.VehicleEntity];
                    var vehicleTransform = TransformLookup[wheel.VehicleEntity];

                    var velocity = physicsVelocity.GetLinearVelocity(mass, vehicleTransform.Position, vehicleTransform.Rotation, contact.Point);

                    contactVelocity = new WheelContactVelocity()
                    {
                        Value = velocity
                    };
                }

                output.FrictionImpulse = float3.zero;
                output.SuspensionImpulse = float3.zero;

                var brakeTorque = (brakes.BrakeTorque * input.Brake + brakes.HandbrakeTorque * input.Handbrake) *
                                  input.MassMultiplier;

                var engineTorque = input.Torque;
                var torqueAbs = math.abs(input.Torque);

                if (brakeTorque > 0)
                {
                    if (torqueAbs < brakeTorque)
                    {
                        brakeTorque -= torqueAbs;
                        engineTorque = 0.0f;
                    }
                    else
                    {
                        engineTorque -= brakeTorque * math.sign(engineTorque);
                        brakeTorque = 0.0f;
                    }
                }

                output.RotationSpeed += (engineTorque / wheel.Inertia) * DeltaTime;

                float speedLimit = speedComponent.CurrentLimit;

                if (speedLimit > 0)
                {
                    var maxRotationSpeedLimit = speedLimit / wheel.Radius;
                    output.RotationSpeed = math.clamp(output.RotationSpeed, -maxRotationSpeedLimit, maxRotationSpeedLimit);
                }

                var maxRotationSpeed = torqueAbs / wheel.Inertia;
                output.RotationSpeed = math.clamp(output.RotationSpeed, -maxRotationSpeed, maxRotationSpeed);

                if (!contact.IsInContact)
                {
                    ApplyBrakeTorque(ref output, wheel.Inertia, brakeTorque, DeltaTime);
                    output.Rotation += output.RotationSpeed * DeltaTime;
                    return;
                }

                #region Suspension

                // Suspension

                var currentSuspensionLength = contact.CurrentSuspensionLength;
                var suspensionDelta = wheel.SuspensionLength - currentSuspensionLength;
                var suspensionForceValue = suspensionDelta * suspension.Stiffness * input.MassMultiplier;

                if (output.LastLength == 0)
                {
                    output.LastLength = currentSuspensionLength;
                }

                var suspensionDamping = suspension.Damping * (output.LastLength - currentSuspensionLength) / DeltaTime;

                var suspensionForce = (suspensionForceValue + suspensionDamping) * contact.Normal;

                output.SuspensionImpulse = suspensionForce * DeltaTime;

                if (vehicleOutput.BlockTime > Time) // Dummy fix after adding physics velocity at runtime
                {
                    output.SuspensionImpulse.y = math.clamp(output.SuspensionImpulse.y, 0, 20f);
                }

                #endregion

                #region Friction

                // Friction

                if (suspensionForceValue > 0)
                {
                    var lateralDirection = math.rotate(input.LocalToWorld.rot, math.right());

                    if (wheel.PowerSteering > 1)
                    {
                        var angle = wheelControllable.MaxSteeringAngle * (wheel.PowerSteering - 1) * vehicleInput.Steering;

                        var q = quaternion.AxisAngle(math.up(), angle);
                        lateralDirection = math.mul(q, lateralDirection);
                    }

                    var longitudinalDirection = math.normalizesafe(math.cross(lateralDirection, contact.Normal));

                    var lateralSpeed = math.dot(lateralDirection, contactVelocity.Value);
                    var longitudinalSpeed = math.dot(longitudinalDirection, contactVelocity.Value);
                    var longitudinalSpeedSign = math.sign(longitudinalSpeed);

                    var wheelDeltaSpeed = longitudinalSpeed - output.RotationSpeed.RotationToLinearSpeed(wheel.Radius);
                    var longitudinalSpeedAbs = math.abs(longitudinalSpeed);

                    var wheelDeltaSpeedSign = math.sign(wheelDeltaSpeed);
                    var wheelDeltaSpeedAbs = math.abs(wheelDeltaSpeed);

                    var lateralSpeedSign = math.sign(lateralSpeed);
                    var lateralSpeedAbs = lateralSpeedSign * lateralSpeed;

                    var longitudinalTimeRange = friction.Longitudinal.Value.TimeRange;
                    var longitudinalSlipT = math.saturate(math.unlerp(longitudinalTimeRange.x,
                        longitudinalTimeRange.y, wheelDeltaSpeedAbs));

                    var lateralTimeRange = friction.Lateral.Value.TimeRange;
                    var lateralSlipT = math.saturate(math.unlerp(lateralTimeRange.x,
                        lateralTimeRange.y, lateralSpeedAbs));

                    var lateralFrictionRate = friction.Lateral.Value.Evaluate(
                        math.lerp(lateralTimeRange.x, lateralTimeRange.y, lateralSlipT));

                    var longitudinalFrictionRate = friction.Longitudinal.Value.Evaluate(
                        math.lerp(longitudinalTimeRange.x, longitudinalTimeRange.y, longitudinalSlipT));

                    var lateralForce = (-lateralSpeedSign * lateralFrictionRate * wheel.LateralFrictionValue
                                        * lateralSpeedAbs * lateralDirection);

                    if (vehicleData.UseLateralTransientForce)
                    {
                        var lateralStep = lateralSpeedAbs * DeltaTime * vehicleData.LateralRelaxMultiplier;

                        if (lateralSpeedAbs < vehicleData.MinTransientLateralSpeed)
                        {
                            var forceSign = output.LateralForce >= 0 ? -1 : 1;
                            output.LateralForce += (-lateralSpeedSign * vehicleData.MaxLateralFriction + forceSign * output.LateralForce) * lateralStep;
                        }
                        else
                        {
                            output.LateralForce = 0;
                        }

                        lateralForce += lateralDirection * output.LateralForce;
                    }

                    var longitudinalBias = Bias(math.saturate(wheelDeltaSpeedAbs), -1);

                    float forwardFrictionValue = wheel.ForwardFrictionValue;

                    if (speedLimit > 0 && longitudinalSpeedAbs >= speedLimit)
                    {
                        forwardFrictionValue = 0;
                    }

                    if (brakeTorque > 0)
                    {
                        var brakeFriction = (wheel.BrakeFrictionValue * input.Brake);
                        var handBrakeFriction = (wheel.BrakeFrictionValue * input.Handbrake);

                        var velocityAbs = math.abs(contactVelocity.Value.z);

                        if (velocityAbs < MinHandBrakeVelocity)
                        {
                            handBrakeFriction *= velocityAbs / MinHandBrakeVelocity;
                        }

                        forwardFrictionValue += (brakeFriction + handBrakeFriction);
                    }

                    float longitudinalFrictionForceValue = -wheelDeltaSpeedSign * longitudinalFrictionRate *
                                                         longitudinalBias * forwardFrictionValue;

                    if (vehicleData.UseForwardTransientForce)
                    {
                        var longTransientForceAbs = math.abs(output.LongTransientForce);
                        var longSign = math.sign(output.LongTransientForce);
                        var longStep = longitudinalSpeedAbs * DeltaTime * vehicleData.ForwardRelaxMultiplier;

                        if (longitudinalSpeedSign != -longSign && longitudinalSpeedAbs > SwitchTransientMinSpeed && longTransientForceAbs > SwitchTransientMinForce)
                        {
                            output.LongTransientForce = 0;
                        }

                        var forceSign = output.LongTransientForce >= 0 ? -1 : 1;

                        output.LongTransientForce += (-longitudinalSpeedSign * vehicleData.MaxForwardFriction + forceSign * output.LongTransientForce) * longStep;

                        if (longitudinalSpeedAbs < vehicleData.MinTransientForwardSpeed && brakeTorque > 0)
                        {
                            longitudinalFrictionForceValue += output.LongTransientForce;
                        }
                        else
                        {
                            output.LongTransientForce = 0;
                        }
                    }

                    var toNeutralForce =
                        (-wheelDeltaSpeed.LinearToRotationSpeed(wheel.Radius) * wheel.Inertia / DeltaTime)
                        .TorqueToForce(wheel.Radius);

                    var usedForceValue = math.abs(toNeutralForce) > math.abs(longitudinalFrictionForceValue)
                        ? longitudinalFrictionForceValue
                        : toNeutralForce;

                    var slowingTorque = usedForceValue.ForceToTorque(wheel.Radius) / wheel.Inertia * DeltaTime;

                    output.RotationSpeed -= slowingTorque;

                    ApplyBrakeTorque(ref output, wheel.Inertia, brakeTorque, DeltaTime);

                    output.Rotation += output.RotationSpeed * DeltaTime;

                    var longitudinalForce = longitudinalFrictionForceValue * longitudinalDirection;

                    var dragForceMagnitude = -math.pow(longitudinalSpeedAbs, 2) * math.sign(longitudinalSpeed) * vehicleData.Drag * longitudinalDirection;

                    output.FrictionImpulse = (lateralForce + longitudinalForce + dragForceMagnitude) * DeltaTime;
                }

                #endregion

                output.LastLength = currentSuspensionLength;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ApplyBrakeTorque(ref WheelOutput output, float wheelInertia, float brakeTorque, float deltaTime)
        {
            if (brakeTorque <= 0) return;
            var toZeroTorque = -output.RotationSpeed * wheelInertia / deltaTime;
            var toZeroTorqueAbs = math.abs(toZeroTorque);

            var usedBrakeTorque = toZeroTorqueAbs < brakeTorque ? toZeroTorqueAbs : brakeTorque;

            output.RotationSpeed += math.sign(toZeroTorque) * usedBrakeTorque / wheelInertia * deltaTime;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Bias(float x, float bias = -1)
        {
            var k = math.pow(1 - bias, 3);
            return (x * k) / (x * k - x + 1);
        }
    }
}