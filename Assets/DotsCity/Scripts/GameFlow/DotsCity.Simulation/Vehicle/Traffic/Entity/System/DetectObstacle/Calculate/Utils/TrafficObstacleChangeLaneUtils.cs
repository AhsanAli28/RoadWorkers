using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic.Obstacle
{
    public class TrafficObstacleChangeLaneUtils : MonoBehaviour
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObstacleResult CheckPathForChangeLaneObstacle(
            in NativeParallelMultiHashMap<int, CarHashData> carHashMapLocal,
            in BlobAssetReference<TrafficObstacleConfig> configRef,
            in CarHashData myCarHashPathEntity)
        {
            if (carHashMapLocal.TryGetFirstValue(myCarHashPathEntity.PathIndex, out var targetCarHashEntity, out var nativeMultiHashMapIterator))
            {
                do
                {
                    var targetEntity = targetCarHashEntity.Entity;

                    if (myCarHashPathEntity.Entity == targetEntity)
                        continue;

                    float3 targetCarPosition = targetCarHashEntity.Position;

                    if (!targetCarHashEntity.HasState(State.ChangingLane))
                        continue;

                    var directionToChangeLanePoint = math.normalize(targetCarHashEntity.Destination - myCarHashPathEntity.Position);
                    var forward = myCarHashPathEntity.Forward;

                    float dotToChangeLanePoint = math.dot(directionToChangeLanePoint, forward);

                    var changeLanePointIsBehindMyCar = dotToChangeLanePoint < 0;

                    if (changeLanePointIsBehindMyCar)
                        continue;

                    float distanceToChangeLanePosition = math.distance(targetCarHashEntity.Destination, targetCarPosition);
                    float distanceToChangeLane = math.distance(myCarHashPathEntity.Position, targetCarHashEntity.Destination);

                    bool targetTooCloseToChangeLanePosition = distanceToChangeLanePosition < configRef.Value.CloseDistanceToChangeLanePoint;
                    bool closeEnoughToStopBeforeLaneChangingPosition = distanceToChangeLane < configRef.Value.StopDistanceBeforeIntersection;

                    const float sizeMultiplier = 2.4f;

                    if (distanceToChangeLane < myCarHashPathEntity.Bounds.Extents.z * sizeMultiplier) // intersect position
                        continue;

                    if (targetTooCloseToChangeLanePosition && closeEnoughToStopBeforeLaneChangingPosition)
                    {
                        return new ObstacleResult(targetEntity, ObstacleType.ChangingLane);
                    }

                } while (carHashMapLocal.TryGetNextValue(out targetCarHashEntity, ref nativeMultiHashMapIterator));
            }

            return default;
        }
    }
}