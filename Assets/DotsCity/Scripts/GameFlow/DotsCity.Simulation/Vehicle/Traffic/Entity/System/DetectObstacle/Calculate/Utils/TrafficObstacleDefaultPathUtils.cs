using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Traffic.Obstacle
{
    public static class TrafficObstacleDefaultPathUtils
    {
        // Modified method from TrafficObstacleUtils.CheckPathForObstacle based on local waypoints

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObstacleResult CheckPathForObstacle(
            in NativeParallelMultiHashMap<int, CarHashData> carHashMap,
            in BlobAssetReference<TrafficObstacleConfig> configRef,
            in CarHashData currentCarHash,
            bool raycastMode,
            ref ApproachData approachData)
        {
            if (carHashMap.TryGetFirstValue(currentCarHash.PathIndex, out var targetCarHash, out var nativeMultiHashMapIterator))
            {
                do
                {
                    var targetEntity = targetCarHash.Entity;

                    if (currentCarHash.Entity == targetEntity)
                        continue;

                    float3 targetCarPosition = targetCarHash.BackwardPoint;

                    var obstacleData = HasObstacle(
                        in configRef,
                        in currentCarHash,
                        targetCarPosition,
                        in targetCarHash,
                        raycastMode,
                        ref approachData);

                    if (obstacleData.HasObstacle)
                    {
                        return obstacleData;
                    }

                } while (carHashMap.TryGetNextValue(out targetCarHash, ref nativeMultiHashMapIterator));
            }

            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObstacleResult HasObstacle(
            in BlobAssetReference<TrafficObstacleConfig> configRef,
            in CarHashData currentCarHash,
            float3 targetCarPosition,
            in CarHashData targetCarHash,
            bool raycastMode,
            ref ApproachData approachData)
        {
            var currentCarCloser = currentCarHash.LocalPathNodeIndex > targetCarHash.LocalPathNodeIndex ||
                currentCarHash.LocalPathNodeIndex == targetCarHash.LocalPathNodeIndex && currentCarHash.DistanceToEnd < targetCarHash.DistanceToEnd;

            if (currentCarCloser)
                return default;

            float distanceToTargetCar = math.distance(currentCarHash.Position, targetCarPosition);

            TrafficObstacleUtils.CheckApproachState(in targetCarHash, distanceToTargetCar, in configRef, ref approachData);

            var obstacleResult = new ObstacleResult();

            if (!raycastMode)
            {
                if (distanceToTargetCar < configRef.Value.MaxDistanceToObstacle)
                {
                    obstacleResult.ObstacleEntity = targetCarHash.Entity;
                    obstacleResult.ObstacleType = ObstacleType.DefaultPath;
                }
            }

            return obstacleResult;
        }
    }
}
