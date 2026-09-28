using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.Extensions;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic.Obstacle
{
    public static class TrafficObstacleUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObstacleResult CheckPathForObstacle(
            in NativeParallelMultiHashMap<int, CarHashData> carHashMap,
            in BlobAssetReference<TrafficObstacleConfig> configRef,
            Entity currentEntity,
            int pathIndex,
            float3 currentPosition,
            bool raycastMode,
            ref ApproachData approachData,
            float distanceToEnd = 0,
            float customDistanceToObstacle = -1,
            bool backwardPointCheckObstacle = true,
            bool currentCarChangingLane = false,
            bool ignoreCycleObstacle = false)
        {
            if (carHashMap.TryGetFirstValue(pathIndex, out var targetCarHash, out var nativeMultiHashMapIterator))
            {
                do
                {
                    var targetEntity = targetCarHash.Entity;

                    if (currentEntity == targetEntity)
                        continue;

                    if (currentCarChangingLane && targetCarHash.HasState(State.ChangingLane))
                        continue;

                    if (ignoreCycleObstacle && targetCarHash.ObstacleEntity != Entity.Null)
                        continue;

                    float3 targetCarPosition;

                    if (backwardPointCheckObstacle)
                    {
                        targetCarPosition = targetCarHash.BackwardPoint;
                    }
                    else
                    {
                        targetCarPosition = targetCarHash.ForwardPoint;
                    }

                    var obstacleData = HasObstacle(
                        in configRef,
                        currentPosition,
                        targetCarPosition,
                        in targetCarHash,
                        raycastMode,
                        targetEntity,
                        ref approachData,
                        distanceToEnd,
                        customDistanceToObstacle);

                    if (obstacleData.HasObstacle)
                        return obstacleData;

                } while (carHashMap.TryGetNextValue(out targetCarHash, ref nativeMultiHashMapIterator));
            }

            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObstacleResult HasObstacle(
            in BlobAssetReference<TrafficObstacleConfig> configRef,
            float3 currentCarPosition,
            float3 targetCarPosition,
            in CarHashData targetCarHash,
            bool raycastMode,
            Entity targetCarEntity,
            ref ApproachData approachData,
            float distanceToEnd = 0,
            float customDistanceToObstacle = -1)
        {
            float distanceToTargetCar = math.distance(currentCarPosition, targetCarPosition);

            float targetCarDistanceToFinish = targetCarHash.DistanceToEnd;

            if (distanceToEnd > targetCarDistanceToFinish || distanceToEnd == 0)
            {
                CheckApproachState(in targetCarHash, distanceToTargetCar, in configRef, ref approachData);
            }

            var obstacleResult = new ObstacleResult();

            if (!raycastMode)
            {
                float maxDistanceToObstacle = customDistanceToObstacle == -1 ? configRef.Value.MaxDistanceToObstacle : customDistanceToObstacle;

                if (distanceToTargetCar < maxDistanceToObstacle)
                {
                    if (distanceToEnd != 0)
                    {
                        if (distanceToEnd > targetCarDistanceToFinish)
                        {
                            obstacleResult.ObstacleEntity = targetCarEntity;
                            obstacleResult.ObstacleType = ObstacleType.DefaultPath;
                        }
                    }
                    else
                    {
                        obstacleResult.ObstacleEntity = targetCarEntity;
                        obstacleResult.ObstacleType = ObstacleType.DefaultPath;
                    }
                }
            }

            return obstacleResult;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckApproachState(
            in CarHashData targetCarHash,
            float distanceToTargetCar,
            in BlobAssetReference<TrafficObstacleConfig> configRef,
            ref ApproachData approachData)
        {
            var approachType = ApproachType.None;

            if (distanceToTargetCar < configRef.Value.MinDistanceToStartApproach)
            {
                approachType = ApproachType.Default;
            }
            else if (distanceToTargetCar < configRef.Value.MinDistanceToStartApproachSoft)
            {
                approachType = ApproachType.Soft;
            }

            if (approachType != ApproachType.None && approachData.Distance > distanceToTargetCar)
            {
                approachData.ApproachType = approachType;
                approachData.Speed = targetCarHash.Speed;
                approachData.Distance = distanceToTargetCar;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObstacleResult CheckNextPaths(
            Entity entity,
            in NativeParallelMultiHashMap<int, CarHashData> carHashMap,
            in PathGraphSystem.Singleton graph,
            in BlobAssetReference<TrafficObstacleConfig> configRef,
            in CarHashData currentCarHash,
            int pathIndex,
            bool avoidCrossroadJam,
            bool raycastMode,
            ref ApproachData approachData,
            int counter = 0,
            bool closeToEnd = false)
        {
            if (counter == 2)
                return default;

            counter++;

            ref readonly var currentPath = ref graph.GetPathData(pathIndex);

            var nextPathIndexes = graph.GetConnectedPaths(in currentPath);

            for (int i = 0; i < nextPathIndexes.Length; i++)
            {
                var nextPathIndex = nextPathIndexes[i];

                // Circle case
                if (nextPathIndex == currentCarHash.PathIndex)
                    continue;

                ref readonly var nextPath = ref graph.GetPathData(nextPathIndex);

                // Ignore potential parking places
                if (counter == 2 && nextPath.PathConnectionType == Gameplay.Road.PathConnectionType.PathPoint)
                    continue;

                var obstacleData = CheckPathForObstacle(
                    in carHashMap,
                    in configRef,
                    entity,
                    nextPathIndex,
                    currentCarHash.Position,
                    raycastMode,
                    ref approachData);

                if (obstacleData.HasObstacle)
                    return obstacleData;

                var hasIntersections = currentPath.IntersectedCount > 0;

                var checkForJam =
                    avoidCrossroadJam &&
                    currentCarHash.HasState(State.InRangeOfSemaphore) &&
                    currentPath.HasOption(PathOptions.EnterOfCrossroad) &&
                    hasIntersections;

                if (checkForJam)
                {
                    obstacleData = CheckForRoadJam(
                        in carHashMap,
                        in graph,
                        nextPathIndex,
                        in currentCarHash,
                        true);

                    if (obstacleData.HasObstacle)
                        return obstacleData;
                }

                if (nextPath.PathLength <= configRef.Value.ShortPathLength && (checkForJam || closeToEnd))
                {
                    obstacleData = CheckNextPaths(
                        entity,
                        in carHashMap,
                        in graph,
                        in configRef,
                        in currentCarHash,
                        nextPathIndex,
                        false,
                        raycastMode,
                        ref approachData,
                        counter,
                        closeToEnd);

                    if (obstacleData.HasObstacle)
                        return obstacleData;

                    if (checkForJam)
                    {
                        var nextNextPathIndexes = graph.GetConnectedPaths(nextPathIndex);

                        for (int j = 0; j < nextNextPathIndexes.Length; j++)
                        {
                            var nextNextPathIndex = nextNextPathIndexes[j];

                            obstacleData = CheckForRoadJam(
                                in carHashMap,
                                in graph,
                                nextNextPathIndex,
                                in currentCarHash,
                                true);

                            if (obstacleData.HasObstacle)
                                return obstacleData;
                        }
                    }
                }
            }

            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObstacleResult CheckForRoadJam(
            in NativeParallelMultiHashMap<int, CarHashData> carHashMap,
            in PathGraphSystem.Singleton graph,
            int pathIndex,
            in CarHashData currentCarHash,
            bool startPathEdge)
        {
            if (carHashMap.TryGetFirstValue(pathIndex, out var targetCarHash, out var nativeMultiHashMapIterator))
            {
                do
                {
                    var targetEntity = targetCarHash.Entity;

                    if (currentCarHash.Entity == targetEntity)
                        continue;

                    bool processTarget = (targetCarHash.HasState(State.HasObstacle) || targetCarHash.HasState(State.IsIdle)) && !targetCarHash.HasState(State.ParkingCar);

                    if (!processTarget)
                        continue;

                    float distanceToPathEdge;

                    if (startPathEdge)
                    {
                        distanceToPathEdge = math.distance(graph.GetStartPosition(pathIndex), targetCarHash.Position);
                    }
                    else
                    {
                        distanceToPathEdge = math.distance(graph.GetEndPosition(pathIndex), targetCarHash.Position);
                    }

                    if (distanceToPathEdge <= targetCarHash.Bounds.Size.z)
                    {
                        return new ObstacleResult(targetEntity, ObstacleType.JamCase_1);
                    }

                } while (carHashMap.TryGetNextValue(out targetCarHash, ref nativeMultiHashMapIterator));
            }

            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObstacleResult CheckForNeighbourPath(
            in NativeParallelMultiHashMap<int, CarHashData> carHashMap,
            in PathGraphSystem.Singleton graph,
            in BlobAssetReference<TrafficObstacleConfig> configRef,
            in CarHashData currentCarHash)
        {
            ref readonly var currentPath = ref graph.GetPathData(currentCarHash.PathIndex);

            if (currentPath.NeighbourCount == 0)
                return default;

            var neighbourPathInfoList = graph.GetNeighbourPaths(in currentPath);

            var startPosition = graph.GetStartPosition(in currentPath);
            var distanceFromStart = math.distancesq(startPosition, currentCarHash.Position);

            float3 dstPoint = default;
            float3 dstPointLeft = default;
            float3 dstPointRight = default;
            VectorExtensions.Line routeLineLeft = default;
            VectorExtensions.Line routeLineRight = default;

            for (int i = 0; i < neighbourPathInfoList.Length; i++)
            {
                if (carHashMap.TryGetFirstValue(neighbourPathInfoList[i], out var targetCarHash, out var nativeMultiHashMapIterator))
                {
                    do
                    {
                        if (targetCarHash.Entity == currentCarHash.Entity)
                            continue;

                        if (targetCarHash.HasState(State.ParkingCar))
                            continue;

                        float distanceToTarget = math.distance(currentCarHash.Position, targetCarHash.Position);

                        bool targetCloseToCurrentCar = distanceToTarget < configRef.Value.MaxDistanceToObstacle;

                        float targetFromStartDistance = math.distancesq(startPosition, targetCarHash.Position);

                        bool targetCloserToEndOfPath = distanceFromStart < targetFromStartDistance;

                        if (targetCloseToCurrentCar && targetCloserToEndOfPath)
                        {
                            float3 directionToTargetCar = math.normalize(targetCarHash.Position - currentCarHash.Position);

                            float dot = math.dot(directionToTargetCar, currentCarHash.Forward);

                            bool inFrontOfView = dot > configRef.Value.InFrontOfViewDot;

                            if (inFrontOfView)
                            {
                                if (dstPointLeft.Equals(float3.zero))
                                {
                                    var right = math.cross(currentCarHash.Forward, math.up());

                                    dstPoint = GetWaypointPoint(in graph, in currentCarHash, configRef.Value.NeighboringDistanceSQ);

                                    const float sizeMult = 1.1f;
                                    dstPointLeft = dstPoint - right * currentCarHash.Bounds.Extents.x * sizeMult;
                                    dstPointRight = dstPoint + right * currentCarHash.Bounds.Extents.x * sizeMult;

                                    var sourcePoint1 = currentCarHash.Position.Flat() - right * currentCarHash.Bounds.Extents.x * sizeMult + currentCarHash.Forward * currentCarHash.Bounds.Extents.z;
                                    var sourcePoint2 = currentCarHash.Position.Flat() + right * currentCarHash.Bounds.Extents.x * sizeMult + currentCarHash.Forward * currentCarHash.Bounds.Extents.z;
                                    routeLineLeft = new VectorExtensions.Line(sourcePoint1, dstPointLeft.Flat());
                                    routeLineRight = new VectorExtensions.Line(sourcePoint2, dstPointRight.Flat());
                                }

                                var obstacleSquare = new ObstacleSquare(targetCarHash.Position.Flat(), targetCarHash.Rotation, targetCarHash.Bounds.Extents);

                                Vector3 intersectPoint = VectorExtensions.LineWithSquareIntersect(routeLineLeft, obstacleSquare.Square, true);

                                var isIntersect = intersectPoint != Vector3.zero;

                                if (isIntersect)
                                    return new ObstacleResult(targetCarHash.Entity, ObstacleType.NeighborPath);

                                intersectPoint = VectorExtensions.LineWithSquareIntersect(routeLineRight, obstacleSquare.Square, true);

                                isIntersect = intersectPoint != Vector3.zero;

                                if (isIntersect)
                                    return new ObstacleResult(targetCarHash.Entity, ObstacleType.NeighborPath);

                                var routeLine = new VectorExtensions.Line(currentCarHash.Position.Flat(), dstPoint);

                                intersectPoint = VectorExtensions.LineWithSquareIntersect(routeLine, obstacleSquare.Square, true);

                                isIntersect = intersectPoint != Vector3.zero;

                                if (isIntersect)
                                    return new ObstacleResult(targetCarHash.Entity, ObstacleType.NeighborPath);
                            }
                        }

                    } while (carHashMap.TryGetNextValue(out targetCarHash, ref nativeMultiHashMapIterator));
                }
            }

            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObstacleResult CheckForIntersectionPath(
            in NativeParallelMultiHashMap<int, CarHashData> carHashMapLocal,
            in PathGraphSystem.Singleton graph,
            in BlobAssetReference<TrafficObstacleConfig> configRef,
            in TrafficAvoidanceConfigReference avoidanceConfigReference,
            in TrafficDestinationComponent destinationComponent,
            in CarHashData currentCarHash,
            bool avoidCrossroadJam)
        {
            ref readonly var currentPathData = ref graph.GetPathData(currentCarHash.PathIndex);

            if (currentPathData.IntersectedCount == 0)
                return default;

            var intersectPathInfoList = graph.GetIntersectedPaths(in currentPathData);

            var currentPosition = currentCarHash.Position;
            var boundsComponent = currentCarHash.Bounds;
            var entity = currentCarHash.Entity;
            var myPriority = currentCarHash.Priority;
            var notAvoidJam = !avoidCrossroadJam || !currentCarHash.HasState(State.InRangeOfSemaphore);

            ObstacleLayout currentBounds = default;
            bool currentBoundsFound = false;

            for (int i = 0; i < intersectPathInfoList.Length; i++)
            {
                var intersectPathInfo = intersectPathInfoList[i];

                bool hasCars = carHashMapLocal.ContainsKey(intersectPathInfo.IntersectedPathIndex);

                if (!hasCars)
                    continue;

                byte intersectionIndex = intersectPathInfo.LocalNodeIndex;

                if (currentCarHash.LocalPathNodeIndex - 1 > intersectionIndex)
                {
                    // Intersection passed
                    continue;
                }

                if (currentCarHash.LocalPathNodeIndex - 1 == intersectionIndex || intersectionIndex == byte.MaxValue)
                {
                    var intersectToDest = math.distance(currentCarHash.Destination, intersectPathInfo.IntersectPosition);

                    if (currentCarHash.DistanceToEnd < intersectToDest)
                    {
                        // Intersection passed
                        continue;
                    }
                }

                float currentCarDistanceToIntersectPoint = math.distance(currentPosition, intersectPathInfo.IntersectPosition);

                if (notAvoidJam)
                {
                    bool currentCarIsCloseEnoughToIntersectPointForCalculate = currentCarDistanceToIntersectPoint < configRef.Value.CalculateDistanceToIntersectPoint;

                    if (!currentCarIsCloseEnoughToIntersectPointForCalculate)
                        continue;
                }

                bool currentCarNearIntersectPoint = false;
                ObstacleLayout currentBoundsNearIntersectPoint = default;

                switch (configRef.Value.IntersectCalculation)
                {
                    case IntersectCalculationMethod.Distance:
                        {
                            float intersectDistance = boundsComponent.Extents.z + boundsComponent.Extents.x + configRef.Value.SizeOffsetToIntersectPoint;
                            currentCarNearIntersectPoint = currentCarDistanceToIntersectPoint < intersectDistance;
                            break;
                        }
                    case IntersectCalculationMethod.Bounds:
                        {
                            float3 dir = default;

                            bool found = false;

                            if (currentPathData.NodeCount - 1 > intersectionIndex)
                            {
                                ref readonly var a1Node = ref graph.GetPathNodeData(in currentPathData, intersectionIndex);
                                ref readonly var a2Node = ref graph.GetPathNodeData(in currentPathData, intersectionIndex + 1);

                                dir = math.normalize(a2Node.Position - a1Node.Position);
                                found = true;
                            }

                            quaternion rotationAtPoint = default;

                            if (found)
                            {
                                rotationAtPoint = quaternion.LookRotationSafe(dir, math.up());
                            }
                            else
                            {
                                rotationAtPoint = currentCarHash.Rotation;
                            }

                            currentBoundsNearIntersectPoint = ObstacleLayoutHelper.GetLayout(intersectPathInfo.IntersectPosition.Flat(), rotationAtPoint, currentCarHash.Bounds.Extents, calcLimits: true);
                            break;
                        }
                }

                if (carHashMapLocal.TryGetFirstValue(intersectPathInfo.IntersectedPathIndex, out var targetCarHash, out var nativeMultiHashMapIterator))
                {
                    do
                    {
                        var targetEntity = targetCarHash.Entity;

                        bool sameEntity = entity == targetEntity;

                        if (sameEntity)
                            continue;

                        float targetCarDistanceToEnd = 0;
                        float targetIntersectToEnd = 0;

                        float targetCarDistanceToIntersectPoint = math.distance(targetCarHash.Position, intersectPathInfo.IntersectPosition);

                        var targetPathEnd = graph.GetEndPosition(intersectPathInfo.IntersectedPathIndex);

                        bool targetCarIsIdle = targetCarHash.HasState(State.IsIdle);
                        bool targetCarAnyIdle = targetCarIsIdle;
                        bool targetParking = targetCarHash.HasState(State.ParkingCar);
                        bool sameTarget = !targetParking && ((destinationComponent.CurrentNode != Entity.Null && destinationComponent.CurrentNode.Index == targetCarHash.CurrentNodeIndex) || destinationComponent.DestinationNode.Index == targetCarHash.CurrentNodeIndex || destinationComponent.DestinationNode.Index == targetCarHash.DestinationNodeIndex);

                        if (!sameTarget)
                        {
                            if (notAvoidJam || targetParking)
                            {
                                bool currentCarAtStopDistance = currentCarDistanceToIntersectPoint < configRef.Value.StopDistanceBeforeIntersection;

                                if (!currentCarAtStopDistance)
                                    continue;
                            }
                        }
                        else
                        {
                            bool currentCarAtStopDistance = currentCarDistanceToIntersectPoint < configRef.Value.StopDistanceForSameTargetNode;

                            if (!currentCarAtStopDistance)
                                continue;
                        }

                        bool targetTooCloseToIntersectPoint = false;

                        switch (configRef.Value.IntersectCalculation)
                        {
                            case IntersectCalculationMethod.Distance:
                                {
                                    float intersectDistance = targetCarHash.Bounds.Extents.z + boundsComponent.Extents.x;

                                    if (!targetCarAnyIdle && sameTarget)
                                    {
                                        intersectDistance += configRef.Value.SizeOffsetToIntersectPoint;
                                    }

                                    targetTooCloseToIntersectPoint = targetCarDistanceToIntersectPoint < intersectDistance;
                                    break;
                                }
                            case IntersectCalculationMethod.Bounds:
                                {
                                    float targetBoundsOffset = 0;

                                    if (!targetCarAnyIdle)
                                    {
                                        targetBoundsOffset = configRef.Value.SizeOffsetToIntersectPoint;
                                    }

                                    ObstacleLayout targetBoundsCurrent = default;

                                    var atParkingPlace = false;

                                    if (targetParking)
                                    {
                                        ref readonly var pathNode = ref graph.GetPathNodeData(targetCarHash.PathIndex, 0);

                                        var dist = math.distancesq(pathNode.Position.Flat(), targetCarHash.Position.Flat());

                                        atParkingPlace = dist < 1f;
                                    }

                                    if (!atParkingPlace)
                                    {
                                        if (targetCarDistanceToIntersectPoint < targetCarHash.Bounds.Extents.z + targetBoundsOffset)
                                        {
                                            targetBoundsCurrent = ObstacleLayoutHelper.GetLayout(targetCarHash.Position.Flat(), targetCarHash.Rotation, targetCarHash.Bounds.Extents, targetBoundsOffset, calcLimits: true);
                                            targetTooCloseToIntersectPoint = currentBoundsNearIntersectPoint.Intersects(targetBoundsCurrent, targetParking);
                                        }
                                    }

                                    if (!targetCarAnyIdle)
                                    {
                                        if (!currentBoundsFound)
                                        {
                                            currentBoundsFound = true;
                                            currentBounds = ObstacleLayoutHelper.GetLayout(currentCarHash.Position.Flat(), currentCarHash.Rotation, currentCarHash.Bounds.Extents, configRef.Value.SizeOffsetToIntersectPoint, calcLimits: true);
                                        }
                                    }
                                    else
                                    {
                                        currentBounds = ObstacleLayoutHelper.GetLayout(currentCarHash.Position.Flat(), currentCarHash.Rotation, currentCarHash.Bounds.Extents, calcLimits: true);
                                    }

                                    var targetBoundsNearIntersectPoint = ObstacleLayoutHelper.GetLayout(intersectPathInfo.IntersectPosition.Flat(), targetCarHash.Rotation, targetCarHash.Bounds.Extents, calcLimits: true);
                                    currentCarNearIntersectPoint = currentBounds.Intersects(targetBoundsNearIntersectPoint);

                                    if (sameTarget && !currentCarNearIntersectPoint && !targetTooCloseToIntersectPoint)
                                    {
                                        targetCarDistanceToEnd = math.distance(targetCarHash.Position, targetPathEnd);
                                        targetIntersectToEnd = math.distance(targetPathEnd, intersectPathInfo.IntersectPosition);

                                        var distanceToIntersect = targetCarDistanceToEnd - targetIntersectToEnd;

                                        bool targetNotPassedIntersectPoint = targetCarDistanceToEnd > targetIntersectToEnd;
                                        bool atStopPosition = distanceToIntersect < configRef.Value.StopDistanceBeforeIntersection;
                                        bool isIdle = targetCarHash.HasState(State.IsIdle) || targetCarHash.HasState(State.HasCalculatedObstacle);

                                        if (targetNotPassedIntersectPoint && atStopPosition && !isIdle)
                                        {
                                            var projectPoint = UnityMathematicsExtension.NearestPointOnLine(targetCarHash.Position.Flat(), intersectPathInfo.IntersectPosition.Flat(), currentCarHash.Position.Flat());
                                            var projectBounds = ObstacleLayoutHelper.GetLayout(projectPoint, targetCarHash.Rotation, targetCarHash.Bounds.Extents, calcLimits: true);

                                            currentCarNearIntersectPoint = currentBounds.Intersects(projectBounds);

                                            var projectPoint2 = UnityMathematicsExtension.NearestPointOnLine(currentCarHash.Position.Flat(), intersectPathInfo.IntersectPosition.Flat(), targetCarHash.Position.Flat());
                                            var projectBounds2 = ObstacleLayoutHelper.GetLayout(projectPoint2, currentCarHash.Rotation, currentCarHash.Bounds.Extents, calcLimits: true);
                                            targetTooCloseToIntersectPoint = targetBoundsCurrent.Intersects(projectBounds2);

                                            if (currentCarNearIntersectPoint && targetTooCloseToIntersectPoint)
                                            {
                                                if (myPriority < targetCarHash.Priority)
                                                {
                                                    currentCarNearIntersectPoint = false;
                                                }
                                                else if (myPriority == targetCarHash.Priority)
                                                {
                                                    currentCarNearIntersectPoint = currentCarDistanceToIntersectPoint < targetCarDistanceToIntersectPoint;

                                                    if (currentCarNearIntersectPoint)
                                                    {
                                                        targetTooCloseToIntersectPoint = false;
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    break;
                                }
                        }

                        if (targetTooCloseToIntersectPoint)
                        {
                            if (currentCarNearIntersectPoint)
                            {
                                if (targetCarDistanceToIntersectPoint < currentCarDistanceToIntersectPoint || avoidanceConfigReference.Config.Value.ResolveCyclicObstacle)
                                {
                                    return new ObstacleResult(targetEntity, ObstacleType.Intersect_1_TargetCarCloseToIntersectPoint);
                                }
                            }
                            else
                            {
                                return new ObstacleResult(targetEntity, ObstacleType.Intersect_2_TargetCarCloseToIntersectPoint);
                            }
                        }

                        if (!currentCarNearIntersectPoint && !targetCarIsIdle && targetCarDistanceToIntersectPoint < configRef.Value.CalculateDistanceToIntersectPoint)
                        {
                            if (targetCarDistanceToEnd == 0)
                            {
                                targetCarDistanceToEnd = math.distance(targetCarHash.Position, targetPathEnd);
                                targetIntersectToEnd = math.distance(targetPathEnd, intersectPathInfo.IntersectPosition);
                            }

                            bool targetNotPassedIntersectPoint = targetCarDistanceToEnd > targetIntersectToEnd;

                            if (!targetNotPassedIntersectPoint)
                                continue;

                            var otherHasPriority = myPriority < targetCarHash.Priority;

                            if (otherHasPriority)
                            {
                                return new ObstacleResult(targetEntity, ObstacleType.Intersect_3_OtherHasPriority);
                            }
                            else if (myPriority == targetCarHash.Priority)
                            {
                                if (configRef.Value.PriorityToRight)
                                {
                                    if (!targetCarHash.HasState(State.HasCalculatedObstacle))
                                    {
                                        var dirToCar = math.normalize(targetCarHash.Position - currentCarHash.Position);

                                        var angle = Vector3.SignedAngle(currentCarHash.Forward, dirToCar, Vector3.up);

                                        // Obstacle car is right of current car
                                        if (angle > 0)
                                        {
                                            return new ObstacleResult(targetEntity, ObstacleType.Intersect_5_PriorityToRight);
                                        }
                                    }
                                }
                                else
                                {
                                    if (!targetCarHash.HasState(State.InRangeOfSemaphore) || !targetCarHash.HasState(State.HasCalculatedObstacle))
                                    {
                                        bool targetCarIsCloserToIntersectPoint = targetCarDistanceToIntersectPoint < currentCarDistanceToIntersectPoint;

                                        if (targetCarIsCloserToIntersectPoint)
                                        {
                                            return new ObstacleResult(targetEntity, ObstacleType.Intersect_4_SamePriority);
                                        }
                                    }
                                }
                            }
                        }

                    } while (carHashMapLocal.TryGetNextValue(out targetCarHash, ref nativeMultiHashMapIterator));
                }
            }

            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float3 GetWaypointPoint(in PathGraphSystem.Singleton graph, in CarHashData currentCarHash, float distanceSQ)
        {
            var routeNodes = graph.GetRouteNodes(currentCarHash.PathIndex);

            float3 dstPoint = default;
            bool found = false;

            if (routeNodes.Length > 2)
            {
                for (int i = currentCarHash.LocalPathNodeIndex; i < routeNodes.Length; i++)
                {
                    var point = routeNodes[i].Position;
                    var dstPointDistance = math.distancesq(currentCarHash.ForwardPoint, point);

                    if (dstPointDistance >= distanceSQ)
                    {
                        dstPoint = point;
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                dstPoint = routeNodes[currentCarHash.LocalPathNodeIndex].Position;
            }

            return dstPoint;
        }
    }
}
