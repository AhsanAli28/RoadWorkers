using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Npc.Navigation;
using Spirit604.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(PedestrianFixedSimulationGroup))]
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct PedestrianMovementSystem : ISystem
    {
        private const float MinDistanceToTargetForNavAgent = 0.12f;
        private const float RotationSpeedMultiplier = 2.5f;
        private const float SideTargetDirectionDot = 0.2f;

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var moveJob = new MoveJob()
            {
                InViewOfCameraLookup = SystemAPI.GetComponentLookup<InViewOfCameraTag>(true),
                EnabledNavigationLookup = SystemAPI.GetComponentLookup<EnabledNavigationTag>(true),
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                PedestrianSettingsReference = SystemAPI.GetSingleton<PedestrianSettingsReference>(),
                DeltaTime = SystemAPI.Time.DeltaTime,
            };

            moveJob.ScheduleParallel();
        }

        [WithNone(typeof(CustomMovementTag), typeof(CustomLocomotionTag), typeof(HasCollisionTag))]
        [WithAll(typeof(AliveTag))]
        [BurstCompile]
        private partial struct MoveJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<InViewOfCameraTag> InViewOfCameraLookup;

            [NativeDisableContainerSafetyRestriction]
            [ReadOnly]
            public ComponentLookup<EnabledNavigationTag> EnabledNavigationLookup;

            [NativeDisableContainerSafetyRestriction]
            [ReadOnly]
            public ComponentLookup<LocalTransform> LocalTransformLookup;

            [ReadOnly]
            public PedestrianSettingsReference PedestrianSettingsReference;

            [ReadOnly]
            public float DeltaTime;

            void Execute(
                Entity entity,
                ref LocalTransform transform,
                in PedestrianMovementSettings movementSettings,
                in DestinationComponent destinationComponent,
                in DestinationDistanceComponent destinationDistanceComponent,
                in NavAgentSteeringComponent navAgentSteeringComponent,
                in NavAgentComponent navAgentComponent)
            {
                var navigationIsEnabled = EnabledNavigationLookup.HasComponent(entity) && EnabledNavigationLookup.IsComponentEnabled(entity);

                bool isAgent = navigationIsEnabled && navAgentSteeringComponent.SteeringTarget == 1;

                float3 target = !isAgent ? destinationComponent.Value : navAgentSteeringComponent.SteeringTargetValue;

                float3 position = transform.Position;

                float3 directionVector = math.normalizesafe(target - position);
                float3 directionVectorFlatted = directionVector.Flat();

                var currentRotation = quaternion.identity;
                var targetRotation = quaternion.LookRotationSafe(directionVectorFlatted, new float3(0, 1, 0));

                bool shouldLerpRotation = PedestrianSettingsReference.Config.Value.LerpRotation;

                if (shouldLerpRotation && PedestrianSettingsReference.Config.Value.LerpRotationInView)
                {
                    shouldLerpRotation = InViewOfCameraLookup.IsComponentEnabled(entity);
                }

                if (shouldLerpRotation)
                {
                    float rotationSpeed = movementSettings.RotationSpeed * DeltaTime;

                    // To avoid circles around the destination
                    if (destinationDistanceComponent.DestinationDistanceSQ < movementSettings.CurrentMovementSpeedSQ * 0.5f)
                    {
                        var dot = math.dot(transform.Forward(), directionVector);

                        if (dot >= -SideTargetDirectionDot && dot <= SideTargetDirectionDot)
                        {
                            rotationSpeed *= RotationSpeedMultiplier;
                        }
                    }

                    currentRotation = math.slerp(transform.Rotation, targetRotation, rotationSpeed);
                }
                else
                {
                    currentRotation = targetRotation;
                }

                transform.Rotation = currentRotation;

                if (movementSettings.CurrentMovementSpeed <= 0)
                {
                    return;
                }

                var moveSpeed = movementSettings.CurrentMovementSpeed * DeltaTime;

                float3 direction = !isAgent ? math.mul(currentRotation, new float3(0, 0, 1)) : directionVector;
                direction.y += directionVector.y;

                if (isAgent)
                {
                    var distance = math.distance(position.Flat(), target.Flat());

                    if (distance > MinDistanceToTargetForNavAgent)
                    {
                        if (distance < moveSpeed)
                        {
                            position = target;
                        }
                        else
                        {
                            position += direction * moveSpeed;
                        }
                    }
                    else
                    {
                        position = target;
                    }
                }
                else
                {
                    position += direction * moveSpeed;
                }

                if (PedestrianSettingsReference.Config.Value.AlignYAxis)
                {
                    var y = math.abs(destinationComponent.Value.y - destinationComponent.PreviousDestination.y);

                    if (y > 0.01f)
                    {
                        if (LocalTransformLookup.TryGetComponent(destinationComponent.PreviuosDestinationNode, out var sourceNode) && LocalTransformLookup.TryGetComponent(destinationComponent.DestinationNode, out var targetNode))
                        {
                            var source = sourceNode.Position;
                            var dir = math.normalize(targetNode.Position - source);
                            var point = VectorExtensions.NearestPointOnLine(source, dir, transform.Position);

                            position.y = math.lerp(position.y, point.y, DeltaTime * PedestrianSettingsReference.Config.Value.YAxisLerpSpeed);
                        }
                    }
                }

                transform.Position = position;
            }
        }
    }
}