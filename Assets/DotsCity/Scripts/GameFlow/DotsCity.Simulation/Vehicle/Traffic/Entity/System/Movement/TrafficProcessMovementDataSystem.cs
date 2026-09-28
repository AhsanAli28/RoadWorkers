using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.Extensions;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(TrafficFixedUpdateGroup))]
    [BurstCompile]
    public partial struct TrafficProcessMovementDataSystem : ISystem
    {
        private const float MinSteeringFactor = 0.2f;
        private const float MaxSpeedDiff = 0.5f;

        private EntityQuery trafficGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            trafficGroup = SystemAPI.QueryBuilder()
                .WithNone<TrafficCustomLocomotion>()
                .WithDisabled<TrafficInitTag>()
                .WithAll<HasDriverTag, TrafficTag, AliveTag>()
                .Build();

            state.RequireForUpdate(trafficGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var job = new TrafficMovementJob
            {
                SpeedComponentType = SystemAPI.GetComponentTypeHandle<SpeedComponent>(false),
                VelocityComponentType = SystemAPI.GetComponentTypeHandle<VelocityComponent>(false),
                TrafficMovementComponentType = SystemAPI.GetComponentTypeHandle<TrafficMovementComponent>(false),
                TransformType = SystemAPI.GetComponentTypeHandle<LocalTransform>(true),
                TrafficPathComponentType = SystemAPI.GetComponentTypeHandle<TrafficPathComponent>(true),
                TrafficSettingsComponentType = SystemAPI.GetComponentTypeHandle<TrafficSettingsComponent>(true),
                TrafficInputComponentType = SystemAPI.GetComponentTypeHandle<VehicleInputReader>(true),
                RotationSpeedComponentType = SystemAPI.GetComponentTypeHandle<TrafficRotationSpeedComponent>(true),
                VehicleOverrideTypeComponentType = SystemAPI.GetComponentTypeHandle<VehicleOverrideTypeComponent>(true),
                TrafficCustomMovementTagType = SystemAPI.GetComponentTypeHandle<TrafficCustomMovementTag>(true),
                TrafficMonoMovementDisabledType = SystemAPI.GetComponentTypeHandle<TrafficMonoMovementDisabled>(true),
                PhysicsWorldComponentType = SystemAPI.GetSharedComponentTypeHandle<PhysicsWorldIndex>(),
                TrafficCommonSettingsReference = SystemAPI.GetSingleton<TrafficCommonSettingsConfigBlobReference>(),
                DeltaTime = SystemAPI.Time.DeltaTime,
            };

            state.Dependency = job.ScheduleParallel(trafficGroup, state.Dependency);
        }

        [BurstCompile]
        public struct TrafficMovementJob : IJobChunk
        {
            public ComponentTypeHandle<SpeedComponent> SpeedComponentType;
            public ComponentTypeHandle<VelocityComponent> VelocityComponentType;
            public ComponentTypeHandle<TrafficMovementComponent> TrafficMovementComponentType;

            [ReadOnly] public ComponentTypeHandle<LocalTransform> TransformType;
            [ReadOnly] public ComponentTypeHandle<TrafficPathComponent> TrafficPathComponentType;
            [ReadOnly] public ComponentTypeHandle<TrafficSettingsComponent> TrafficSettingsComponentType;
            [ReadOnly] public ComponentTypeHandle<VehicleInputReader> TrafficInputComponentType;
            [ReadOnly] public ComponentTypeHandle<TrafficRotationSpeedComponent> RotationSpeedComponentType;
            [ReadOnly] public ComponentTypeHandle<VehicleOverrideTypeComponent> VehicleOverrideTypeComponentType;
            [ReadOnly] public ComponentTypeHandle<TrafficCustomMovementTag> TrafficCustomMovementTagType;
            [ReadOnly] public ComponentTypeHandle<TrafficMonoMovementDisabled> TrafficMonoMovementDisabledType;
            [ReadOnly] public SharedComponentTypeHandle<PhysicsWorldIndex> PhysicsWorldComponentType;

            [ReadOnly] public TrafficCommonSettingsConfigBlobReference TrafficCommonSettingsReference;
            [ReadOnly] public float DeltaTime;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var transforms = chunk.GetNativeArray(ref TransformType);
                var speedComponents = chunk.GetNativeArray(ref SpeedComponentType);
                var velocityComponents = chunk.GetNativeArray(ref VelocityComponentType);
                var trafficPathComponents = chunk.GetNativeArray(ref TrafficPathComponentType);
                var trafficSettingsComponents = chunk.GetNativeArray(ref TrafficSettingsComponentType);
                var trafficInputComponents = chunk.GetNativeArray(ref TrafficInputComponentType);
                var rotationSpeedComponents = chunk.GetNativeArray(ref RotationSpeedComponentType);
                var trafficMovementComponents = chunk.GetNativeArray(ref TrafficMovementComponentType);
                var physicsWorldChunkIndex = chunk.GetSharedComponentIndex(PhysicsWorldComponentType);

                bool hasPhysicsVelocity = physicsWorldChunkIndex == 0;

                bool hasMono = chunk.Has(ref TrafficMonoMovementDisabledType);
                bool hasCustomPhysics = false;

                if (TrafficCommonSettingsReference.Reference.Value.PhysicsSimulation == PhysicsSimulationType.CustomDots)
                {
                    hasCustomPhysics = hasPhysicsVelocity;
                }

                for (var index = 0; index < chunk.Count; index++)
                {
                    bool customMovement = chunk.IsComponentEnabled(ref TrafficCustomMovementTagType, index);

                    if (hasMono)
                    {
                        if (!customMovement)
                        {
                            hasMono = !chunk.IsComponentEnabled(ref TrafficMonoMovementDisabledType, index);
                            hasCustomPhysics = hasMono;
                        }
                    }
                    else
                    {
                        if (hasPhysicsVelocity)
                            hasCustomPhysics = !customMovement;
                    }

                    var movementComponent = trafficMovementComponents[index];

                    float gasInput = trafficInputComponents[index].Throttle;
                    var trafficSettings = trafficSettingsComponents[index];
                    float maxSpeed = trafficSettings.MaxSpeed;

                    quaternion currentRotation = transforms[index].Rotation;

                    var desiredMovementDirectionSign = gasInput > -0.9899f ? 1 : -1;

                    float3 carForward = math.mul(currentRotation, math.forward());
                    float3 desiredMovementForward = carForward * desiredMovementDirectionSign;

                    float dot = math.dot(carForward, math.normalize(velocityComponents[index].Value));

                    var movementDirection = dot > 0 ? 1 : -1;
                    movementComponent.CurrentMovementDirection = movementDirection;

                    float deltaTime = DeltaTime;

                    float targetSpeed = maxSpeed * gasInput;
                    float deaccelerationMultiplier = gasInput > 0 ? gasInput : 1;

                    var speedComponent = speedComponents[index];

                    targetSpeed = GetTargetSpeed(in speedComponent, gasInput, targetSpeed);

                    float newSpeed = 0;

                    if (!hasCustomPhysics)
                    {
                        var currentSpeed = velocityComponents[index].Value.magnitude;
                        currentSpeed = currentSpeed * movementDirection;
                        newSpeed = CalculateNewCurrentSpeed(in trafficSettings, gasInput, currentSpeed, deltaTime, targetSpeed, deaccelerationMultiplier);

                        speedComponent.Value = newSpeed;
                        speedComponents[index] = speedComponent;
                    }

                    float3 trafficPosition = transforms[index].Position;

                    if (trafficSettings.OffsetY != 0)
                    {
                        trafficPosition.y -= trafficSettings.OffsetY;
                    }

                    float3 targetWayPoint = trafficPathComponents[index].DestinationWayPoint;

                    Vector3 targetDirection = math.normalizesafe(targetWayPoint - trafficPosition);

                    float signedAngle = Vector3.SignedAngle(math.normalize(targetDirection.Flat()), math.normalize(desiredMovementForward.Flat()), Vector3.down) * desiredMovementDirectionSign;

                    signedAngle = math.radians(signedAngle);

                    var angle = math.abs(signedAngle);

                    bool maxSteering = false;

                    if (TrafficCommonSettingsReference.Reference.Value.MaxSpeedSteerDirectionAngle == 0 || TrafficCommonSettingsReference.Reference.Value.MaxSpeedSteerDirectionAngle > speedComponent.Value)
                    {
                        float maxSteerDirectionAngle = trafficSettings.MaxSteerDirectionAngle;

                        maxSteering = angle > maxSteerDirectionAngle;
                    }

                    if (maxSteering)
                    {
                        float sign = math.sign(signedAngle);
                        movementComponent.DesiredSteeringAngle = sign * trafficSettings.MaxSteerAngle;
                    }
                    else
                    {
                        movementComponent.DesiredSteeringAngle = math.clamp(signedAngle, -trafficSettings.MaxSteerAngle, trafficSettings.MaxSteerAngle);
                    }

                    movementComponent.SteeringAngle = math.lerp(movementComponent.SteeringAngle, movementComponent.DesiredSteeringAngle, trafficSettingsComponents[index].SteeringDamping);
                    movementComponent.TargetSpeed = targetSpeed;

                    if (!hasCustomPhysics)
                    {
                        quaternion currentCalculatedRotation;

                        bool hasRotationLerp = DotsEnumExtension.HasFlagUnsafe(trafficSettingsComponents[index].AdditionalSettings, TrafficAdditionalSettings.HasRotationLerp);

                        float rotationSpeed = 0;

                        if (hasRotationLerp)
                        {
                            switch (TrafficCommonSettingsReference.Reference.Value.SimplePhysicsType)
                            {
                                case SimplePhysicsSimulationType.CarInput:
                                    float steeringFactor = math.abs(movementComponent.SteeringAngle) / trafficSettings.MaxSteerAngle;
                                    steeringFactor = math.clamp(steeringFactor, MinSteeringFactor, 1);
                                    rotationSpeed = rotationSpeedComponents[index].Value * steeringFactor;
                                    break;
                                case SimplePhysicsSimulationType.FollowTarget:
                                    rotationSpeed = rotationSpeedComponents[index].Value;
                                    break;
                            }
                        }

                        bool calculateAngularVelocity = hasPhysicsVelocity && hasRotationLerp;
                        var angularVelocity = ProcessRotationLerp(rotationSpeed, deltaTime, desiredMovementForward, targetDirection, carForward, currentRotation, gasInput, hasRotationLerp, calculateAngularVelocity, out currentCalculatedRotation);

                        movementComponent.AngularVelocity = angularVelocity;
                        movementComponent.CurrentCalculatedRotation = currentCalculatedRotation;

                        Vector3 velocity = default;

                        switch (TrafficCommonSettingsReference.Reference.Value.SimplePhysicsType)
                        {
                            case SimplePhysicsSimulationType.CarInput:
                                {
                                    var dir = math.mul(quaternion.Euler(0, movementComponent.SteeringAngle * desiredMovementDirectionSign, 0), carForward);
                                    dir.y = targetDirection.y;
                                    velocity = dir * newSpeed;
                                    break;
                                }
                            case SimplePhysicsSimulationType.FollowTarget:
                                {
                                    velocity = targetDirection * math.abs(newSpeed);
                                    break;
                                }
                        }

                        velocityComponents[index] = new VelocityComponent { Value = velocity };
                        movementComponent.LinearVelocity = velocity;
                    }

                    movementComponent.TargetDirection = targetDirection;
                    trafficMovementComponents[index] = movementComponent;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static float CalculateNewCurrentSpeed(in TrafficSettingsComponent trafficSettingsComponent, float gasInput, float speed, float deltaTime, float targetSpeed, float deaccelerationMultiplier)
            {
                if (gasInput > 0 && speed < targetSpeed)
                {
                    float acceleration = speed < 0 ? trafficSettingsComponent.BrakePower : trafficSettingsComponent.Acceleration;
                    speed = math.min(speed + acceleration * deltaTime, targetSpeed);
                }
                else if (gasInput < 0 && speed > targetSpeed)
                {
                    float backAcceleration = speed > 0 ? trafficSettingsComponent.BrakePower : trafficSettingsComponent.BackwardAcceleration;
                    speed = math.max(speed - backAcceleration * deltaTime, targetSpeed);
                }
                else
                {
                    if (math.abs(speed) - math.abs(targetSpeed) < MaxSpeedDiff)
                    {
                        speed = targetSpeed;
                    }
                    else
                    {
                        var speedSign = math.sign(speed);
                        speed = math.max(speed - trafficSettingsComponent.BrakePower * deaccelerationMultiplier * deltaTime * speedSign, 0);
                    }
                }

                if (gasInput >= 0)
                {
                    speed = math.min(speed, trafficSettingsComponent.MaxSpeed);
                }
                else
                {
                    speed = math.max(speed, -trafficSettingsComponent.MaxSpeed);
                }

                return speed;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static float GetTargetSpeed(in SpeedComponent speedComponent, float gasInput, float targetSpeed)
            {
                if (gasInput >= 0)
                {
                    targetSpeed = math.clamp(targetSpeed, 0, speedComponent.CurrentLimit);
                }
                else
                {
                    targetSpeed = math.clamp(targetSpeed, -speedComponent.CurrentLimit, speedComponent.CurrentLimit);
                }

                return targetSpeed;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static float3 ProcessRotationLerp(float rotationSpeed, float deltaTime, Vector3 desiredForward, Vector3 targetDirection, Vector3 carForward, quaternion currentRotation, float gasInput, bool hasRotationLerp, bool calculateAngularVelocity, out quaternion calculatedCurrentRotation)
            {
                calculatedCurrentRotation = default;
                quaternion targetRotation = default;

                if (gasInput != 0 && !targetDirection.Equals(float3.zero))
                {
                    float currentDesiredDirectionDot = math.dot(carForward, desiredForward);
                    var desiredDirectionSign = math.sign(currentDesiredDirectionDot);

                    targetRotation = quaternion.LookRotationSafe(targetDirection * desiredDirectionSign, math.up());
                    bool hasLerp = false;

                    if (hasRotationLerp)
                    {
                        calculatedCurrentRotation = math.slerp(currentRotation, targetRotation, rotationSpeed * deltaTime);
                        hasLerp = true;
                    }

                    if (!hasLerp)
                    {
                        calculatedCurrentRotation = targetRotation;
                    }
                }
                else
                {
                    calculatedCurrentRotation = currentRotation;
                }

                var angularVelocity = float3.zero;

                if (calculateAngularVelocity)
                {
                    Quaternion deltaRotation = calculatedCurrentRotation * Quaternion.Inverse(currentRotation);

                    deltaRotation.ToAngleAxis(out var axisAngle, out var axis);

                    axisAngle *= Mathf.Deg2Rad;
                    angularVelocity = axisAngle * axis / deltaTime;
                }

                return angularVelocity;
            }
        }
    }
}