using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.Traffic.Obstacle;
using Spirit604.Extensions;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public static class TrafficChangeLaneUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CalcTargetLaneObstacles(
            Entity entity,
            float3 destination,
            int targetPathKey,
            int targetPathNodeIndex,
            Entity targetPathNodeEntity,
            float defaultLaneSpeed,
            ref TrafficDestinationComponent destinationComponent,
            ref TrafficPathComponent trafficPathComponent,
            ref TrafficChangeLaneComponent trafficChangeLaneComponent,
            ref TrafficTargetDirectionComponent trafficTargetDirectionComponent,
            in LocalTransform transform,
            in NativeParallelMultiHashMap<int, CarHashData> carHashMapLocal,
            in BlobAssetReference<TrafficChangeLaneConfig> changeLaneRef)
        {
#if UNITY_EDITOR
            trafficChangeLaneComponent.DistanceToOtherCarsInNeighborLane = 9999f;
#endif

            bool shouldChangeLane = true;

            if (carHashMapLocal.TryGetFirstValue(targetPathKey, out var carHashEntity, out var nativeMultiHashMapIterator))
            {
                do
                {
                    if (carHashEntity.Entity == entity)
                    {
                        continue;
                    }

                    float distanceToCar = math.distance(destination, carHashEntity.Position);

#if UNITY_EDITOR
                    if (trafficChangeLaneComponent.DistanceToOtherCarsInNeighborLane > distanceToCar)
                    {
                        trafficChangeLaneComponent.DistanceToOtherCarsInNeighborLane = distanceToCar;
                    }
#endif

                    var directionToPoint = math.normalize(destination - carHashEntity.Position);
                    var dotToPoint = math.dot(directionToPoint, carHashEntity.Forward);
                    var pointInFrontOfCar = dotToPoint > 0;

                    float maxDistanceToOtherCarsInOtherLane;

                    if (pointInFrontOfCar)
                    {
                        float t = math.clamp(carHashEntity.Speed / defaultLaneSpeed, 0, 1);
                        maxDistanceToOtherCarsInOtherLane = math.lerp(changeLaneRef.Value.MinTargetLaneCarDistance, changeLaneRef.Value.MaxTargetLaneCarDistance, t);
                    }
                    else
                    {
                        maxDistanceToOtherCarsInOtherLane = changeLaneRef.Value.MinTargetLaneCarDistance;
                    }

                    if (distanceToCar < maxDistanceToOtherCarsInOtherLane)
                    {
                        shouldChangeLane = false;
                        break;
                    }

                } while (carHashMapLocal.TryGetNextValue(out carHashEntity, ref nativeMultiHashMapIterator));
            }

            if (shouldChangeLane)
            {
                destinationComponent.Destination = destination;
                trafficPathComponent.DestinationWayPoint = destination;

                trafficChangeLaneComponent.Destination = destination;
                trafficChangeLaneComponent.TargetPathGlobalIndex = targetPathKey;
                trafficChangeLaneComponent.TargetSourceLaneNodeEntity = targetPathNodeEntity;
                trafficChangeLaneComponent.TargetLocalNodeIndex = targetPathNodeIndex;

                Vector3 directionToTarget = math.normalize(destination.Flat() - transform.Position.Flat());
                float directionAngle = Vector3.SignedAngle(transform.Forward(), directionToTarget, Vector3.up);

                int direction = directionAngle < 0 ? -1 : 1;

                trafficTargetDirectionComponent = new TrafficTargetDirectionComponent()
                {
                    Direction = direction,
                };

                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetTargetLanePositionAndIndex(
            float vehicleSpeed,
            float3 vehiclePosition,
            ref PathGraphSystem.Singleton graph,
            ref TrafficChangeLaneConfigReference changeLaneRef,
            ref TrafficCommonSettingsConfigBlobReference TrafficCommonSettingsConfigBlobReference,
            int sourcePathGlobalIndex,
            int targetPathGlobalIndex,
            int sourceLocalNodeIndex,
            out int targetPathNodeIndex,
            out float3 destination)
        {
            bool found = false;

            if (sourceLocalNodeIndex < 0)
            {
                sourceLocalNodeIndex = 0;
            }

            var sourceRouteNodes = graph.GetRouteNodes(sourcePathGlobalIndex);
            var targetRouteNodes = graph.GetRouteNodes(targetPathGlobalIndex);

            float t = vehicleSpeed / TrafficCommonSettingsConfigBlobReference.Reference.Value.DefaultLaneSpeed;

            var changeOffset = math.lerp(changeLaneRef.Config.Value.MinChangeLaneOffset, changeLaneRef.Config.Value.MaxChangeLaneOffset, t);

            var sameRoutes = sourceRouteNodes.Length == targetRouteNodes.Length;

            float targetDistance = changeOffset;
            int startIndex = 0;

            if (sameRoutes)
            {
                var previousLocalNodePosition = sourceRouteNodes[sourceLocalNodeIndex].Position;
                float distanceFromPreviousNode = math.distance(previousLocalNodePosition, vehiclePosition);
                targetDistance += distanceFromPreviousNode;
                startIndex = sourceLocalNodeIndex;
            }
            else
            {
                for (int i = 0; i < sourceLocalNodeIndex; i++)
                {
                    var currentNodePosition = sourceRouteNodes[i].Position;
                    var nextNodePosition = sourceRouteNodes[i + 1].Position;

                    var nodeDistance = math.distance(currentNodePosition, nextNodePosition);
                    targetDistance += nodeDistance;
                }

                var distanceToVehicle = math.distance(sourceRouteNodes[sourceLocalNodeIndex].Position, vehiclePosition);

                targetDistance += distanceToVehicle;
            }

            float sumDistance = 0;
            targetPathNodeIndex = 1;
            destination = default;

            for (int index = startIndex; index < targetRouteNodes.Length - 1; index++)
            {
                var currentNodePosition = targetRouteNodes[index].Position;
                var nextNodePosition = targetRouteNodes[index + 1].Position;

                var nodeDistance = math.distance(currentNodePosition, nextNodePosition);

                var previousSum = sumDistance;
                sumDistance += nodeDistance;

                if (sumDistance >= targetDistance)
                {
                    float targetPositionOffset = targetDistance - previousSum;
                    destination = currentNodePosition + math.normalize(nextNodePosition - currentNodePosition) * targetPositionOffset;
                    targetPathNodeIndex = index + 1;
                    found = true;
                    break;
                }
            }

            return found;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SetWaitForChangeLane(
            ref EntityCommandBuffer commandBuffer,
            int sourceLaneGlobalPathIndex,
            int changeLaneGlobalPathIndex,
            int sourceNodeNodeIndex,
            Entity entity,
            ref TrafficPathComponent trafficPathComponent,
            ref TrafficDestinationComponent destinationComponent,
            ref TrafficStateComponent trafficStateComponent,
            ref EnabledRefRW<TrafficIdleTag> trafficIdleTagRW,
            in LocalTransform transform,
            Entity pathEntity,
            ref PathGraphSystem.Singleton graph,
            ref TrafficChangeLaneConfigReference trafficChangeLaneConfigReference,
            ref TrafficCommonSettingsConfigBlobReference trafficCommonSettingsConfigBlobReference,
            float vehicleSpeed = 0
        )
        {
            TrafficChangeLaneRequestedPositionComponent request = SetAndCreateChangeLaneRequest(
                sourceLaneGlobalPathIndex,
                changeLaneGlobalPathIndex,
                sourceNodeNodeIndex,
                ref trafficPathComponent,
                ref destinationComponent,
                in transform,
                pathEntity,
                ref graph,
                ref trafficChangeLaneConfigReference,
                ref trafficCommonSettingsConfigBlobReference,
                out var found,
                vehicleSpeed);

            if (found)
            {
                commandBuffer.AddComponent(entity, request);
                TrafficStateExtension.AddIdleState<TrafficWaitForChangeLaneTag>(ref commandBuffer, entity, ref trafficStateComponent, ref trafficIdleTagRW, TrafficIdleState.WaitForChangeLane);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SetWaitForChangeLane(
            ref EntityCommandBuffer.ParallelWriter commandBuffer,
            int sortKey,
            int sourceLaneGlobalPathIndex,
            int changeLaneGlobalPathIndex,
            int sourceNodeNodeIndex,
            Entity entity,
            ref TrafficPathComponent trafficPathComponent,
            ref TrafficDestinationComponent destinationComponent,
            ref TrafficStateComponent trafficStateComponent,
            ref EnabledRefRW<TrafficIdleTag> trafficIdleTagRW,
            in LocalTransform transform,
            Entity pathEntity,
            ref PathGraphSystem.Singleton graph,
            ref TrafficChangeLaneConfigReference trafficChangeLaneConfigReference,
            ref TrafficCommonSettingsConfigBlobReference trafficCommonSettingsConfigBlobReference,
            float vehicleSpeed = 0
        )
        {
            TrafficChangeLaneRequestedPositionComponent request = SetAndCreateChangeLaneRequest(
                sourceLaneGlobalPathIndex,
                changeLaneGlobalPathIndex,
                sourceNodeNodeIndex,
                ref trafficPathComponent,
                ref destinationComponent,
                in transform,
                pathEntity,
                ref graph,
                ref trafficChangeLaneConfigReference,
                ref trafficCommonSettingsConfigBlobReference,
                out var found,
                vehicleSpeed);

            if (found)
            {
                commandBuffer.AddComponent(sortKey, entity, request);
                TrafficStateExtension.AddIdleState<TrafficWaitForChangeLaneTag>(ref commandBuffer, sortKey, entity, ref trafficStateComponent, ref trafficIdleTagRW, TrafficIdleState.WaitForChangeLane);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TrafficChangeLaneRequestedPositionComponent SetAndCreateChangeLaneRequest(
            int sourceLaneGlobalPathIndex,
            int changeLaneGlobalPathIndex,
            int sourceNodeNodeIndex,
            ref TrafficPathComponent trafficPathComponent,
            ref TrafficDestinationComponent destinationComponent,
            in LocalTransform transform,
            Entity pathEntity,
            ref PathGraphSystem.Singleton graph,
            ref TrafficChangeLaneConfigReference trafficChangeLaneConfigReference,
            ref TrafficCommonSettingsConfigBlobReference trafficCommonSettingsConfigBlobReference,
            out bool found,
            float vehicleSpeed = 0)
        {
            float3 targetLanePosition;
            int targetPathNodeIndex;

            found = TrafficChangeLaneUtils.GetTargetLanePositionAndIndex(
                 vehicleSpeed,
                 transform.Position,
                 ref graph,
                 ref trafficChangeLaneConfigReference,
                 ref trafficCommonSettingsConfigBlobReference,
                 sourceLaneGlobalPathIndex,
                 changeLaneGlobalPathIndex,
                 sourceNodeNodeIndex,
                 out targetPathNodeIndex,
                 out targetLanePosition);

            if (found)
            {
                var request = new TrafficChangeLaneRequestedPositionComponent()
                {
                    TargetPathKey = changeLaneGlobalPathIndex,
                    TargetPathNodeIndex = targetPathNodeIndex,
                    TargetSourceLaneEntity = pathEntity,
                    Destination = targetLanePosition,
                };

                trafficPathComponent = new TrafficPathComponent
                {
                    CurrentGlobalPathIndex = sourceLaneGlobalPathIndex,
                    LocalPathNodeIndex = sourceNodeNodeIndex,
                    DestinationWayPoint = targetLanePosition,
                    PreviousDestination = trafficPathComponent.DestinationWayPoint,
                    Priority = -2,
                };

                destinationComponent.Destination = targetLanePosition;

                return request;
            }

            return default;
        }
    }
}
