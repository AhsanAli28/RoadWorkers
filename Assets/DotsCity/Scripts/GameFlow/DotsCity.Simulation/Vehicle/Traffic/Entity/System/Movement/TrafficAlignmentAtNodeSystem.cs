using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.Gameplay.Road;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(TrafficFixedUpdateGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficAlignmentAtNodeSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<TrafficAccurateAligmentCustomMovementTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var hasStoppingEngine = false;

            if (SystemAPI.HasSingleton<CarStopEngineConfigReference>())
            {
                hasStoppingEngine = SystemAPI.GetSingleton<CarStopEngineConfigReference>().Config.Value.HasStopEngine;
            }

            var carAlignmentJob = new CarAlignmentJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                WorldTransformLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true),
                InViewOfCameraLookup = SystemAPI.GetComponentLookup<InViewOfCameraTag>(true),
                TrafficNodeSettingsLookup = SystemAPI.GetComponentLookup<TrafficNodeSettingsComponent>(true),
                PhysicsVelocityLookup = SystemAPI.GetComponentLookup<PhysicsVelocity>(true),
                TrafficParkingConfigReference = SystemAPI.GetSingleton<TrafficParkingConfigReference>(),
                TrafficRoadConfigReference = SystemAPI.GetSingleton<TrafficRoadConfigReference>(),
                DeltaTime = SystemAPI.Time.DeltaTime,
                HasStoppingEngine = hasStoppingEngine,
            };

            carAlignmentJob.Schedule();
        }

        [WithAll(typeof(TrafficAccurateAligmentCustomMovementTag))]
        [BurstCompile]
        public partial struct CarAlignmentJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public ComponentLookup<LocalToWorld> WorldTransformLookup;

            [ReadOnly]
            public ComponentLookup<TrafficNodeSettingsComponent> TrafficNodeSettingsLookup;

            [ReadOnly]
            public ComponentLookup<InViewOfCameraTag> InViewOfCameraLookup;

            [ReadOnly]
            public ComponentLookup<PhysicsVelocity> PhysicsVelocityLookup;

            [ReadOnly]
            public TrafficParkingConfigReference TrafficParkingConfigReference;

            [ReadOnly]
            public TrafficRoadConfigReference TrafficRoadConfigReference;

            [ReadOnly]
            public float DeltaTime;

            [ReadOnly]
            public bool HasStoppingEngine;

            void Execute(
                Entity entity,
                ref TrafficMovementComponent trafficMovementComponent,
                ref SpeedComponent speedComponent,
                ref VehicleInputReader trafficInputComponent,
                ref LocalTransform transform,
                in TrafficDestinationComponent destinationComponent)
            {
                var enteredNodeEntity = Entity.Null;

                if (TrafficNodeSettingsLookup.HasComponent(destinationComponent.CurrentNode))
                {
                    enteredNodeEntity = destinationComponent.CurrentNode;
                }
                else if (TrafficNodeSettingsLookup.HasComponent(destinationComponent.DestinationNode))
                {
                    enteredNodeEntity = destinationComponent.DestinationNode;
                }

                if (enteredNodeEntity == Entity.Null)
                {
                    return;
                }

                var settingsComponent = TrafficNodeSettingsLookup[enteredNodeEntity];

                bool isParkingNode = settingsComponent.TrafficNodeType == TrafficNodeType.Parking;

                bool isTriggerNode = (TrafficRoadConfigReference.Config.Value.LinkedNodeFlags & settingsComponent.TrafficNodeTypeFlag) != 0;

                bool aligmentComplete = true;

                if (isTriggerNode)
                {
                    bool positionComplete = true;
                    aligmentComplete = false;

                    trafficMovementComponent.AngularVelocity = default;
                    trafficMovementComponent.LinearVelocity = default;
                    trafficInputComponent = VehicleInputReader.GetBrake();
                    speedComponent.Value = 0;

                    var targetRotation = WorldTransformLookup[enteredNodeEntity].Rotation;
                    var targetPosition = WorldTransformLookup[enteredNodeEntity].Position;

                    var currentRotation = math.slerp(transform.Rotation, targetRotation, TrafficParkingConfigReference.Config.Value.RotationSpeed * DeltaTime);

                    if (TrafficParkingConfigReference.Config.Value.PrecisePosition)
                    {
                        var speed = TrafficParkingConfigReference.Config.Value.MovementSpeed * DeltaTime;
                        var currentPosition = math.lerp(transform.Position, targetPosition, speed);
                        transform.Position = currentPosition;

                        var distance = math.distancesq(currentPosition, targetPosition);

                        positionComplete = distance <= TrafficParkingConfigReference.Config.Value.AchieveDistanceSQ;

                        if (!positionComplete)
                        {
                            speedComponent.Value = speed;
                        }
                    }

                    transform.Rotation = currentRotation;
                    trafficMovementComponent.CurrentCalculatedRotation = currentRotation;

                    var angle = UnityEngine.Quaternion.Angle(targetRotation, currentRotation);

                    if (angle < TrafficParkingConfigReference.Config.Value.CompleteAngle && positionComplete)
                    {
                        aligmentComplete = true;
                    }
                }

                if (aligmentComplete)
                {
                    var inViewOfCamera = InViewOfCameraLookup.IsComponentEnabled(entity);

                    CommandBuffer.RemoveComponent<TrafficAccurateAligmentCustomMovementTag>(entity);

                    if (!HasStoppingEngine || !inViewOfCamera || !isParkingNode)
                    {
                        CommandBuffer.SetComponentEnabled<TrafficEnteredTriggerNodeTag>(entity, true);
                    }
                    else
                    {
                        InteractCarUtils.StopEngine(ref CommandBuffer, entity);
                    }

                    if (PhysicsVelocityLookup.HasComponent(entity))
                    {
                        CommandBuffer.SetComponent(entity, new PhysicsVelocity());
                    }

                    CommandBuffer.SetComponent(entity, new VelocityComponent());
                }
            }
        }
    }
}