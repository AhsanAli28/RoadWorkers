using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public partial class TrafficSpawnerSystem : EndInitSystemBase
    {
        private partial struct SpawnJob : IJob
        {
            public struct CarSpawnInfo
            {
                public float3 Position;
                public float3 CarSize;
            }

            private const int ATTEMPT_FIND_SPAWNPOSITION_COUNT_ON_ROAD = 10;
            private const int FIND_NODE_ATTEMPT_COUNT = 20;

            public void AddSpawnedCar(int pathIndex, float3 spawnPosition, float3 carSize)
            {
                TempSpawnCarHashMap.Add(pathIndex, new CarSpawnInfo() { Position = spawnPosition, CarSize = carSize });
            }

            public Entity InitializeSpawnData(bool initialSpawn, ref TrafficSpawnParams trafficSpawnParams)
            {
                int attemptCount = 0;

                trafficSpawnParams.spawnPosition = float3.zero;
                trafficSpawnParams.spawnRotation = quaternion.identity;
                trafficSpawnParams.speedLimit = 0;
                trafficSpawnParams.targetNodeEntity = Entity.Null;

                while (true)
                {
                    attemptCount++;

                    if (attemptCount > FIND_NODE_ATTEMPT_COUNT)
                        break;

                    int tempLocalSpawnIndex = randomGen.NextInt(0, TrafficNodeAvailableEntities.Length);
                    var canSpawn = SourceNodeIsAvailable(tempLocalSpawnIndex);

                    if (!canSpawn)
                        continue;

                    var tempSpawnTrafficNodeEntity = TrafficNodeAvailableEntities[tempLocalSpawnIndex];
                    var pathConnections = PathConnectionLookup[tempSpawnTrafficNodeEntity];

                    if (pathConnections.Length == 0)
                        continue;

                    int localPathIndex = randomGen.NextInt(0, pathConnections.Length);

                    PathConnectionElement localPathSettings = pathConnections[localPathIndex];
                    int tempGlobalPathIndex = localPathSettings.GlobalPathIndex;
                    var targetNode = localPathSettings.ClosestConnectedNode;

                    if (!TrafficNodeSettingsLookup.HasComponent(targetNode))
                        continue;

                    ref readonly var tempPathData = ref Graph.GetPathData(tempGlobalPathIndex);

                    var entityData = GetPrefabEntityData(trafficSpawnParams.carModelIndex);
                    var vehicleTrafficGroup = entityData.TrafficGroup;

                    bool sourceNodeIsAvailable = SourceNodeIsAvailable(TrafficNodeSettingsLookup[tempSpawnTrafficNodeEntity], TrafficNodeCapacityLookup[tempSpawnTrafficNodeEntity], in tempPathData, vehicleTrafficGroup);

                    bool targetNodeIsPermitted = false;

                    if (sourceNodeIsAvailable)
                    {
                        targetNodeIsPermitted =
                            NodeAvailableForSpawn(TrafficNodeSettingsLookup[tempSpawnTrafficNodeEntity])
                            && NodeAvailableForTarget(TrafficNodeSettingsLookup[targetNode], TrafficNodeCapacityLookup[targetNode])
                            && TrafficNodeAvailableEntities.Contains(targetNode);
                    }

                    if (!sourceNodeIsAvailable || !targetNodeIsPermitted)
                        continue;

                    int globalPathIndex = localPathSettings.GlobalPathIndex;
                    ref readonly var pathData = ref Graph.GetPathData(globalPathIndex);

                    var allowedRouteRandomizeSpawning = true;
                    var sameLightCrossroad = false;

                    if (!localPathSettings.HasSubNode || localPathSettings.SameHash)
                    {
                        int currentCrossRoadIndex = TrafficNodeLookup[tempSpawnTrafficNodeEntity].CrossRoadIndex;
                        int targetCrossRoadIndex = TrafficNodeLookup[targetNode].CrossRoadIndex;
                        var sourceLightEntity = TrafficNodeLookup[tempSpawnTrafficNodeEntity].LightEntity;
                        var targetLightEntity = TrafficNodeLookup[targetNode].LightEntity;
                        sameLightCrossroad = currentCrossRoadIndex != -1 && currentCrossRoadIndex == targetCrossRoadIndex && (sourceLightEntity != Entity.Null || targetLightEntity != Entity.Null);
                        allowedRouteRandomizeSpawning = TrafficNodeSettingsLookup[tempSpawnTrafficNodeEntity].AllowedRouteRandomizeSpawning;
                    }

                    bool hasIntersects = false;

                    if (allowedRouteRandomizeSpawning)
                    {
                        var intersectedPaths = Graph.GetIntersectedPaths(globalPathIndex);

                        if (intersectedPaths.Length > 0)
                        {
                            var sourceTrafficNode = Entity.Null;
                            var connectedTrafficNode = Entity.Null;

                            RuntimePathDataRef.TryGetValue(globalPathIndex, out sourceTrafficNode, out connectedTrafficNode);

                            for (int i = 0; i < intersectedPaths.Length; i++)
                            {
                                var intersectedPathIndex = intersectedPaths[i].IntersectedPathIndex;

                                var intersectedSourceTrafficNode = Entity.Null;
                                var intersectedConnectedTrafficNode = Entity.Null;

                                RuntimePathDataRef.TryGetValue(intersectedPathIndex, out intersectedSourceTrafficNode, out intersectedConnectedTrafficNode);

                                if (sourceTrafficNode != intersectedSourceTrafficNode && sourceTrafficNode != intersectedConnectedTrafficNode && connectedTrafficNode != intersectedConnectedTrafficNode)
                                {
                                    hasIntersects = true;
                                    break;
                                }
                            }
                        }
                    }

                    var allowSpawnRandomPosition = !hasIntersects && allowedRouteRandomizeSpawning && !sameLightCrossroad;

                    int localPathNodeIndex;

                    var hasSpawnPoint = FindSpawnPositionOnRoad(
                        initialSpawn,
                        ref trafficSpawnParams,
                        ref pathConnections,
                        tempSpawnTrafficNodeEntity,
                        localPathIndex,
                        allowSpawnRandomPosition,
                        out localPathNodeIndex);

                    if (!hasSpawnPoint)
                        continue;

                    localPathNodeIndex++;

                    var spawnNode = tempSpawnTrafficNodeEntity;
                    trafficSpawnParams.previousNodeEntity = tempSpawnTrafficNodeEntity;
                    trafficSpawnParams.globalPathIndex = globalPathIndex;

                    ref readonly var targetPathNode = ref Graph.GetPathNodeData(in tempPathData, localPathNodeIndex);
                    var targetWayPoint = targetPathNode.Position;

                    var trafficPathComponent = new TrafficPathComponent()
                    {
                        CurrentGlobalPathIndex = globalPathIndex,
                        DestinationWayPoint = targetWayPoint,
                        LocalPathNodeIndex = localPathNodeIndex,
                    };

                    trafficSpawnParams.trafficPathComponent = trafficPathComponent;

                    if (allowedRouteRandomizeSpawning)
                    {
                        trafficSpawnParams.pathConnectionType = pathData.PathConnectionType;
                        trafficSpawnParams.targetNodeEntity = localPathSettings.ConnectedNodeEntity;
                    }
                    else // Made for custom traffic node init (parking, traffic area node, etc...)
                    {
                        trafficSpawnParams.pathConnectionType = PathConnectionType.TrafficNode;
                        trafficSpawnParams.targetNodeEntity = tempSpawnTrafficNodeEntity;

                        if (TrafficNodeSettingsLookup[tempSpawnTrafficNodeEntity].TrafficNodeType == TrafficNodeType.Parking)
                        {
                            currentParkingCarsCount++;
                        }
                    }

                    if (spawnNode != Entity.Null)
                        return spawnNode;
                }

                return Entity.Null;
            }

            private bool SourceNodeIsAvailable(in TrafficNodeSettingsComponent sourceNodeSettingsComponent, in TrafficNodeCapacityComponent trafficNodeCapacityComponent, in PathGraphSystem.PathData tempPathData, TrafficGroupType vehicleTrafficGroup)
            {
                return (tempPathData.IsAvailable(vehicleTrafficGroup) && !trafficNodeCapacityComponent.HasCar())
                    && (sourceNodeSettingsComponent.TrafficNodeType != TrafficNodeType.Parking ||
                   sourceNodeSettingsComponent.TrafficNodeType == TrafficNodeType.Parking && currentParkingCarsCount < TrafficSpawnerConfigBlobReference.Reference.Value.MaxParkingCarsCount);
            }

            private static bool NodeAvailableForSpawn(in TrafficNodeSettingsComponent trafficNodeSettingsComponent)
            {
                return trafficNodeSettingsComponent.IsAvailableForSpawn;
            }

            private static bool NodeAvailableForTarget(in TrafficNodeSettingsComponent trafficNodeSettingsComponent, in TrafficNodeCapacityComponent trafficNodeCapacityComponent)
            {
                return trafficNodeSettingsComponent.IsAvailableForSpawnTarget && !trafficNodeCapacityComponent.HasCar();
            }

            public bool SourceNodeIsAvailable(int tempNodeIndex)
            {
                return CanSpawn(tempNodeIndex);
            }

            private bool FindSpawnPositionOnRoad(
                bool initialSpawn,
                ref TrafficSpawnParams trafficSpawnParams,
                ref DynamicBuffer<PathConnectionElement> pathConnections,
                Entity tempSpawnEntity,
                int localPathIndex,
                bool allowSpawnRandomPosition,
                out int pathNodeIndex)
            {
                bool found = false;
                pathNodeIndex = -1;

                var pathIndex = pathConnections[localPathIndex].GlobalPathIndex;

                for (int i = 0; i < ATTEMPT_FIND_SPAWNPOSITION_COUNT_ON_ROAD; i++)
                {
                    var targetPosition = Graph.GetEndPosition(pathIndex);

                    bool tooCloseForOtherCars = false;

                    float3 spawnDirection;
                    float3 tempSpawnPosition;
                    quaternion spawnRotation = quaternion.identity;

                    if (allowSpawnRandomPosition)
                    {
                        GetSpawnData(pathConnections[localPathIndex], out tempSpawnPosition, out spawnDirection, out pathNodeIndex);
                    }
                    else
                    {
                        pathNodeIndex = 0;

                        var tempTransform = TransformLookup[tempSpawnEntity];
                        tempSpawnPosition = tempTransform.Position;
                        spawnRotation = tempTransform.Rotation;
                        spawnDirection = math.mul(spawnRotation, math.forward());
                    }

                    float distance = math.distance(tempSpawnPosition, targetPosition);

                    bool tooCloseToEndOfPath = distance < TrafficSpawnerConfigBlobReference.Reference.Value.MinSpawnCarDistance;

                    if (tooCloseToEndOfPath)
                    {
                        tooCloseForOtherCars = true;
                    }

                    if (!tooCloseForOtherCars)
                    {
                        tooCloseForOtherCars = NeighborPathsHasObstacle(ref pathConnections, tempSpawnPosition);
                    }

                    if (!tooCloseForOtherCars)
                    {
                        if (!initialSpawn)
                        {
                            tooCloseForOtherCars = IsForbiddenSpawnPlace(spawnDirection, tempSpawnPosition);
                        }
                        else
                        {
                            tooCloseForOtherCars = CheckForPlayerFar(tempSpawnPosition);
                        }
                    }

                    if (!tooCloseForOtherCars)
                    {
                        found = true;
                        trafficSpawnParams.spawnPosition = tempSpawnPosition;

                        if (allowSpawnRandomPosition)
                        {
                            trafficSpawnParams.spawnRotation = quaternion.LookRotationSafe(spawnDirection, math.up());
                        }
                        else
                        {
                            trafficSpawnParams.spawnRotation = spawnRotation;
                        }

                        ref readonly var pathNode = ref Graph.GetPathNodeData(pathConnections[localPathIndex].GlobalPathIndex, pathNodeIndex);
                        trafficSpawnParams.speedLimit = pathNode.SpeedLimit;
                        break;
                    }
                    else if (!allowSpawnRandomPosition)
                    {
                        break;
                    }
                }

                return found;
            }

            private bool CheckSpawnPositionForObstacles(ref TrafficSpawnParams trafficSpawnParams, ref DynamicBuffer<PathConnectionElement> pathConnections, Entity tempSpawnEntity)
            {
                var tempNodeTransform = TransformLookup[tempSpawnEntity];
                var spawnPosition = tempNodeTransform.Position;

                var tooClose = CheckForPlayerDistance(spawnPosition);

                if (tooClose)
                    return false;

                bool hasCloseCar = NeighborPathsHasObstacle(ref pathConnections, spawnPosition);

                if (!hasCloseCar)
                {
                    trafficSpawnParams.spawnPosition = tempNodeTransform.Position;
                    trafficSpawnParams.spawnRotation = tempNodeTransform.Rotation;
                }
                else
                {
                    return false;
                }

                return true;
            }

            private bool NeighborPathsHasObstacle(ref DynamicBuffer<PathConnectionElement> pathConnections, float3 spawnPosition)
            {
                for (int j = 0; j < pathConnections.Length; j++)
                {
                    var pathIndex = pathConnections[j].GlobalPathIndex;

                    bool canSpawn = CanSpawn(pathIndex, spawnPosition);

                    if (!canSpawn)
                        return true;
                }

                return false;
            }

            private bool CanSpawn(int selectedPathIndex, float3 spawnPosition)
            {
                if (TempSpawnCarHashMap.TryGetFirstValue(selectedPathIndex, out CarSpawnInfo carHashEntity, out var nativeMultiHashMapIterator))
                {
                    do
                    {
                        float distance = math.distance(carHashEntity.Position, spawnPosition);

                        var minDistance = carHashEntity.CarSize.z / 2 + TrafficSpawnerConfigBlobReference.Reference.Value.MinSpawnCarDistance;

                        if (distance < minDistance)
                            return false;

                    } while (TempSpawnCarHashMap.TryGetNextValue(out carHashEntity, ref nativeMultiHashMapIterator));
                }

                return true;
            }

            private void GetSpawnData(in PathConnectionElement pathConnections, out float3 spawnPosition, out float3 spawnDirection, out int pathNodeIndex)
            {
                int pathIndex = pathConnections.GlobalPathIndex;
                float pathLength = Graph.GetPathData(pathIndex).PathLength;

                float t = randomGen.NextFloat(0f, 1f);

                float targetPathLength = pathLength * t;

                Graph.GetPositionOnRoad(pathIndex, targetPathLength, out spawnPosition, out spawnDirection, out pathNodeIndex);
            }

            private bool CheckForPlayerDistance(float3 tempSpawnPosition)
            {
                if (CitySpawnConfigReference.Config.Value.TrafficSpawnStateNode != CullState.InViewOfCamera)
                {
                    var distanceToPlayer = math.distancesq(tempSpawnPosition, PlayerPosition);
                    bool closeToPlayer = distanceToPlayer <= CullSystemConfigReference.Config.Value.VisibleDistanceSQ || distanceToPlayer >= CullSystemConfigReference.Config.Value.MaxDistanceSQ;

                    return closeToPlayer;
                }

                return false;
            }

            private bool CheckForPlayerFar(float3 tempSpawnPosition)
            {
                var distanceToPlayer = math.distancesq(tempSpawnPosition, PlayerPosition);
                bool closeToPlayer = distanceToPlayer >= CullSystemConfigReference.Config.Value.MaxDistanceSQ;

                return closeToPlayer;
            }

            public bool IsForbiddenSpawnPlace(float3 spawnDirection, float3 tempSpawnPosition)
            {
                bool inView = CheckForPlayerDistance(tempSpawnPosition);

                if (inView)
                    return true;

                bool tooClose = false;

                if (CarHashMapSingleton.IsCreated && !CarHashMapSingleton.IsEmpty)
                {
                    const float minOffsetSize = 10f;
                    float offsetSize = math.min(minOffsetSize, TrafficSpawnerConfigBlobReference.Reference.Value.MinSpawnCarDistance);

                    var keys = HashMapHelper.GetHashMapPosition9Cells(tempSpawnPosition.Flat(), offset: offsetSize);

                    for (int i = 0; i < keys.Length; i++)
                    {
                        if (CarHashMapSingleton.CarHashMap.TryGetFirstValue(keys[i], out var carHashEntity, out var nativeMultiHashMapIterator))
                        {
                            do
                            {
                                float distance = math.distancesq(carHashEntity.Position, tempSpawnPosition);

                                if (distance < TrafficSpawnerConfigBlobReference.Reference.Value.MinSpawnCarDistanceSQ)
                                {
                                    tooClose = true;
                                    break;
                                }

                            } while (CarHashMapSingleton.CarHashMap.TryGetNextValue(out carHashEntity, ref nativeMultiHashMapIterator));
                        }

                        if (tooClose)
                            break;
                    }

                    keys.Dispose();
                }

                return tooClose;
            }

            private bool CanSpawn(int tempNodeLocalIndex)
            {
                if (TrafficNodeAvailableEntities.Length == 0)
                {
                    return false;
                }

                var entity = TrafficNodeAvailableEntities[tempNodeLocalIndex];

                return TrafficNodeSettingsLookup.HasComponent(entity) && UnityMathematicsExtension.ChanceDropped(TrafficNodeSettingsLookup[entity].ChanceToSpawn, randomGen);
            }
        }
    }
}