using Spirit604.DotsCity.Simulation.Car;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public static class TramRailMovementUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CalculateRailMovement(
            in TrafficPathComponent trafficPathComponent,
            in TrafficMovementComponent trafficMovementComponent,
            in TrafficSettingsComponent trafficSettingsComponent,
            in TrafficObstacleComponent trafficObstacleComponent,
            in SpeedComponent speedComponent,
            in LocalTransform transform,
            in TrafficRailConfigReference trafficRailConfig,
            float deltaTime,
            bool customDestination,
            out float3 movingSpeedVector,
            out quaternion newRotation)
        {
            newRotation = transform.Rotation;
            float3 movingForwardDirection;

            var previousPoint = trafficPathComponent.PreviousDestination;

            var isBackward = trafficPathComponent.PathDirection == PathForwardType.Backward;

            if (math.isnan(previousPoint.x) || previousPoint.Equals(float3.zero))
            {
                previousPoint = transform.Position;
            }

            if (!customDestination && !previousPoint.IsEqual(trafficPathComponent.DestinationWayPoint))
            {
                if (!isBackward)
                {
                    movingForwardDirection = math.normalizesafe(trafficPathComponent.DestinationWayPoint - previousPoint);
                }
                else
                {
                    movingForwardDirection = math.normalizesafe(previousPoint - trafficPathComponent.DestinationWayPoint);
                }
            }
            else
            {
                movingForwardDirection = math.mul(trafficMovementComponent.CurrentCalculatedRotation, new float3(0, 0, 1));
            }

            movingSpeedVector = movingForwardDirection * speedComponent.Value;

            var vehiclePosition = transform.Position;

            if (!trafficObstacleComponent.HasObstacle)
            {
                var speed = speedComponent.ValueAbs;

                if (speed > 0.1f)
                {
                    var railPoint = VectorExtensions.NearestPointOnLine(previousPoint, movingForwardDirection, vehiclePosition);

                    if (railPoint != default)
                    {
                        float distance = math.distance(railPoint, vehiclePosition);

                        if (distance > trafficRailConfig.Config.Value.MaxDistanceToRailLine)
                        {
                            var strafeDirection = (float3)railPoint - vehiclePosition;
                            var strafeDirectionNormalized = math.normalizesafe(strafeDirection);

                            var movingStrafeDirection = strafeDirectionNormalized * trafficRailConfig.Config.Value.LateralSpeed;

                            movingSpeedVector += movingStrafeDirection;
                        }
                    }
                }

                if (speed > 0.01f)
                {
                    var targetRot = quaternion.LookRotation(movingForwardDirection, new float3(0, 1, 0));

                    bool lerp = DotsEnumExtension.HasFlagUnsafe<TrafficAdditionalSettings>(trafficSettingsComponent.AdditionalSettings, TrafficAdditionalSettings.HasRailRotationLerp);

                    if (!lerp)
                    {
                        newRotation = targetRot;
                    }
                    else
                    {
                        newRotation = math.slerp(transform.Rotation, targetRot, trafficRailConfig.Config.Value.RotationLerpSpeed * deltaTime);
                    }
                }
            }
        }
    }
}