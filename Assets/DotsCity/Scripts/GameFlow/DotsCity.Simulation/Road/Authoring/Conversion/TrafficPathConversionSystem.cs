using Spirit604.Gameplay.Road;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Road.Authoring
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateAfter(typeof(TrafficNodeConversionSystem))]
    [UpdateInGroup(typeof(BakingSystemGroup))]
    public partial class TrafficPathConversionSystem : SimpleSystemBase
    {
        private TrafficNodeConversionSystem trafficNodeConversionSystem;
        private EntityQuery crossRoadQuery;
        private Dictionary<int, List<IntersectPathInfo>> pathIntersectMap = new Dictionary<int, List<IntersectPathInfo>>();
        private Dictionary<int, List<int>> pathNeighbourMap = new Dictionary<int, List<int>>();
        private Dictionary<int, List<int>> pathParallelMap = new Dictionary<int, List<int>>();
        private bool updated = false;
        private int processed = 0;
        private uint version = 0;

        protected override void OnCreate()
        {
            base.OnCreate();

            trafficNodeConversionSystem = World.GetOrCreateSystemManaged<TrafficNodeConversionSystem>();
            RequireForUpdate<PathGraphReference>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Dispose();
        }

        private void Dispose()
        {
            pathIntersectMap.Clear();
            pathNeighbourMap.Clear();
            pathParallelMap.Clear();
        }

        protected override void OnUpdate()
        {
            updated = false;

            trafficNodeConversionSystem.GetDependency().Complete();

            CreatePathMap();

            CompleteInitilization();
            Dispose();
        }

        private void CompleteInitilization()
        {
            var pathGraphEntity = SystemAPI.GetSingletonEntity<PathGraphReference>();

            var blobAssetStore = EntityManager.World.GetExistingSystemManaged<BakingSystem>().BlobAssetStore;

            var graphRef = PathGraphFactory.Create(EntityManager, pathGraphEntity, trafficNodeConversionSystem.PathConnectingMap, pathIntersectMap, pathNeighbourMap, pathParallelMap);

            var hash = new Hash128(version, version, version, version);

            blobAssetStore.TryAdd(hash, ref graphRef);

            version++;
        }

        private void CreatePathMap()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            Entities
            .WithoutBurst()
            .WithNone<CrossroadSubSegmentTag>()
            .WithStructuralChanges()
            .ForEach((
                Entity crossRoadEntity,
                in CrossroadBakingData crossroadBakingData) =>
            {
                if (!updated)
                {
                    updated = true;
                }

                CreateNeighborMapLane(in crossroadBakingData.TrafficNodeScopes);
                CreateParallelMapLane(in crossroadBakingData.TrafficNodeScopes);
                CreateIntersectMapLane(in crossroadBakingData.TrafficNodeScopes);
                CreatePathConnectionMap(in crossroadBakingData.TrafficNodeScopes);

                if (!EntityManager.HasComponent<SegmentComponent>(crossRoadEntity))
                {
                    commandBuffer.DestroyEntity(crossRoadEntity);
                }
            }).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }

        private void CreatePathConnectionMap(in NativeArray<Entity> trafficNodeScopes)
        {
            for (int scopeIndex = 0; scopeIndex < trafficNodeScopes.Length; scopeIndex++)
            {
                var scopeEntity = trafficNodeScopes[scopeIndex];

                if (!EntityManager.HasComponent<TrafficNodeScopeBakingData>(scopeEntity)) continue;

                var trafficNodeScopeData = EntityManager.GetComponentData<TrafficNodeScopeBakingData>(scopeEntity);

                ref var rightLaneEntities = ref trafficNodeScopeData.RightLaneEntities;

                if (rightLaneEntities.IsCreated)
                {
                    for (int laneIndex = 0; laneIndex < rightLaneEntities.Length; laneIndex++)
                    {
                        IterateConnectionLanes(ref rightLaneEntities, laneIndex);
                    }
                }

                if (trafficNodeScopeData.IsCreated(true))
                {
                    ref var leftLaneEntities = ref trafficNodeScopeData.LeftLaneEntities;

                    for (int laneIndex = 0; laneIndex < leftLaneEntities.Length; laneIndex++)
                    {
                        IterateConnectionLanes(ref leftLaneEntities, laneIndex);
                    }
                }
            }
        }

        private void IterateConnectionLanes(ref NativeArray<TrafficNodeTempData> laneEntities, int laneIndex)
        {
            if (!laneEntities.IsCreated || laneEntities.Length <= laneIndex)
                return;

            var sourceEntity = laneEntities[laneIndex].Entity;
            var sourceBuffer = EntityManager.GetBuffer<PathConnectionElement>(sourceEntity);

            for (int i = 0; i < sourceBuffer.Length; i++)
            {
                int sourcePathIndex = sourceBuffer[i].GlobalPathIndex;

                var sourcePathData = trafficNodeConversionSystem.PathConnectingMap[sourcePathIndex];
                var connectedEntity = sourceBuffer[i].ConnectedNodeEntity;

                if (connectedEntity == Entity.Null)
                {
                    UnityEngine.Debug.Log($"SourcePathIndex {sourcePathIndex} connected entity is null");
                    return;
                }

                var targetBuffer = EntityManager.GetBuffer<PathConnectionElement>(connectedEntity);

                if (sourcePathData.PathConnectionType != PathConnectionType.PathPoint)
                {
                    AddConnectedIndexes(sourcePathIndex, targetBuffer);
                }
                else
                {
                    var sourceTempConnectionData = trafficNodeConversionSystem.PathConnectingMap[sourcePathIndex];
                    var connectedPathInstanceId = sourceTempConnectionData.ConnectedPathInstanceId;

                    if (pathIntersectMap.ContainsKey(sourcePathIndex) &&
                        pathIntersectMap[sourcePathIndex]?.Count > 0 &&
                        connectedPathInstanceId != -1)
                    {
                        int localIntesectedPathIndex = -1;

                        var connectedPathIndex = trafficNodeConversionSystem.InstanceIdToGlobalIndexPathMap[connectedPathInstanceId];

                        sourceTempConnectionData.ConnectedPathIndex = connectedPathIndex;
                        trafficNodeConversionSystem.PathConnectingMap[sourcePathIndex] = sourceTempConnectionData;

                        var intersects = pathIntersectMap[sourcePathIndex];

                        for (int n = 0; n < intersects.Count; n++)
                        {
                            var intersectedPathIndex = intersects[n].IntersectedPathIndex;

                            if (intersectedPathIndex == connectedPathIndex)
                            {
                                localIntesectedPathIndex = n;
                                break;
                            }
                        }

                        if (localIntesectedPathIndex < 0)
                        {
                            UnityEngine.Debug.Log($"Path Instance id '{sourceTempConnectionData.InstanceId}' Connected PathInstanceId '{connectedPathInstanceId}'. Path Point Connected path not found. Make sure that the path is connected to the correct lane index & last point aligned on connected path{TrafficObjectFinderMessage.GetMessage()}");
                            continue;
                        }

                        var intesectedPathIndex = pathIntersectMap[sourcePathIndex][localIntesectedPathIndex].IntersectedPathIndex;
                        var intesectedPathPosition = pathIntersectMap[sourcePathIndex][localIntesectedPathIndex].IntersectPosition;

                        for (int k = 0; k < targetBuffer.Length; k++)
                        {
                            if (targetBuffer[k].GlobalPathIndex == intesectedPathIndex)
                            {
                                var targetPathData = trafficNodeConversionSystem.PathConnectingMap[targetBuffer[k].GlobalPathIndex];

                                Entity newConnectedEntity = targetBuffer[k].ConnectedNodeEntity;

                                var targetPosition = targetPathData.Nodes.Last().Position;

                                float distance = math.distancesq(intesectedPathPosition, targetPosition);

                                const float closeDistanceToEndSQ = 25f;
                                if (distance < closeDistanceToEndSQ) // if intersect point close to end of connected path
                                {
                                    var newTargetBuffer = EntityManager.GetBuffer<PathConnectionElement>(newConnectedEntity);
                                    AddConnectedIndexes(sourcePathIndex, newTargetBuffer);
                                }
                            }
                        }
                    }
                    else
                    {
                        UnityEngine.Debug.Log($"Path Instance id '{sourceTempConnectionData.InstanceId}' ConnectedPathInstanceId {connectedPathInstanceId} Path Point Intersection not found{TrafficObjectFinderMessage.GetMessage()}");
                    }
                }
            }
        }

        private void AddConnectedIndexes(int sourcePathIndex, DynamicBuffer<PathConnectionElement> targetBuffer)
        {
            for (int i = 0; i < targetBuffer.Length; i++)
            {
                var connectedPathIndex = targetBuffer[i].GlobalPathIndex;

                trafficNodeConversionSystem.PathConnectingMap[sourcePathIndex].ConnectedIndexes.Add(connectedPathIndex);
            }
        }

        private void CreateNeighborMapLane(in NativeArray<Entity> trafficNodeScopes)
        {
            for (int i = 0; i < trafficNodeScopes.Length; i++)
            {
                var scopeEntity = trafficNodeScopes[i];

                if (!EntityManager.HasComponent<TrafficNodeScopeBakingData>(scopeEntity)) continue;

                var trafficNodeScopeData = EntityManager.GetComponentData<TrafficNodeScopeBakingData>(scopeEntity);

                if (trafficNodeScopeData.IsCreated(false))
                {
                    ref var rightEntities = ref trafficNodeScopeData.RightLaneEntities;
                    IterateNeighborLanes(trafficNodeScopeData, ref rightEntities, true);
                }

                if (trafficNodeScopeData.IsCreated(true))
                {
                    ref var leftEntities = ref trafficNodeScopeData.LeftLaneEntities;
                    IterateNeighborLanes(trafficNodeScopeData, ref leftEntities, false);
                }
            }
        }

        private void IterateNeighborLanes(TrafficNodeScopeBakingData trafficNodeScopeData, ref NativeArray<TrafficNodeTempData> laneEntities, bool isRightLanes)
        {
            if (!laneEntities.IsCreated)
                return;

            for (int laneIndex = 0; laneIndex < laneEntities.Length; laneIndex++)
            {
                var laneData = laneEntities[laneIndex];
                var pathsCount = trafficNodeScopeData.LanePathsCount(laneIndex, isRightLanes);

                if (pathsCount <= 1)
                    continue;

                var sourceBuffer = EntityManager.GetBuffer<PathConnectionElement>(laneData.Entity);

                var minPathIndex = laneData.MinPathSettingsIndex;
                var maxPathIndex = laneData.MaxPathSettingsIndex;

                for (int localPathIndex1 = minPathIndex; localPathIndex1 < maxPathIndex; localPathIndex1++)
                {
                    for (int localPathIndex2 = minPathIndex; localPathIndex2 < maxPathIndex; localPathIndex2++)
                    {
                        if (localPathIndex1 != localPathIndex2)
                        {
                            var localBuffeIndex1 = localPathIndex1 - minPathIndex;
                            var localBuffeIndex2 = localPathIndex2 - minPathIndex;
                            int globalIndex1 = sourceBuffer[localBuffeIndex1].GlobalPathIndex;
                            int globalIndex2 = sourceBuffer[localBuffeIndex2].GlobalPathIndex;

                            if (!pathNeighbourMap.ContainsKey(globalIndex1))
                            {
                                pathNeighbourMap.Add(globalIndex1, new List<int>());
                            }

                            pathNeighbourMap[globalIndex1].Add(globalIndex2);
                        }
                    }
                }
            }
        }

        private void CreateParallelMapLane(in NativeArray<Entity> trafficNodeScopes)
        {
            for (int i = 0; i < trafficNodeScopes.Length; i++)
            {
                var scopeEntity = trafficNodeScopes[i];

                if (!EntityManager.HasComponent<TrafficNodeScopeBakingData>(scopeEntity)) continue;

                var trafficNodeScopeData = EntityManager.GetComponentData<TrafficNodeScopeBakingData>(scopeEntity);

                ref var rightLaneEntities = ref trafficNodeScopeData.RightLaneEntities;
                IterateParallelLanes(ref rightLaneEntities);

                if (trafficNodeScopeData.IsCreated(true))
                {
                    ref var leftLaneEntities = ref trafficNodeScopeData.LeftLaneEntities;
                    IterateParallelLanes(ref leftLaneEntities);
                }
            }
        }

        private void IterateParallelLanes(ref NativeArray<TrafficNodeTempData> lanes)
        {
            if (!lanes.IsCreated || lanes.Length < 2)
                return;

            for (int laneIndex1 = 0; laneIndex1 < lanes.Length; laneIndex1++)
            {
                for (int laneIndex2 = 0; laneIndex2 < lanes.Length; laneIndex2++)
                {
                    int laneDiff = math.abs(laneIndex1 - laneIndex2);

                    bool isNeighborLane = laneDiff == 1;

                    if (isNeighborLane)
                    {
                        var entity1 = lanes[laneIndex1].Entity;
                        var entity2 = lanes[laneIndex2].Entity;

                        SetParallelPath(entity1, entity2);
                    }
                }
            }
        }

        private void SetParallelPath(Entity entity1, Entity entity2)
        {
            var buffer1 = EntityManager.GetBuffer<PathConnectionElement>(entity1);
            var buffer2 = EntityManager.GetBuffer<PathConnectionElement>(entity2);

            for (int pathIndex1 = 0; pathIndex1 < buffer1.Length; pathIndex1++)
            {
                for (int pathIndex2 = 0; pathIndex2 < buffer2.Length; pathIndex2++)
                {
                    int globalIndex1 = buffer1[pathIndex1].GlobalPathIndex;
                    int globalIndex2 = buffer2[pathIndex2].GlobalPathIndex;

                    if (trafficNodeConversionSystem.PathConnectingMap.Count <= globalIndex1 || globalIndex1 < 0)
                    {
                        UnityEngine.Debug.Log($"{trafficNodeConversionSystem.PathConnectingMap.Count} <= {globalIndex1}");
                    }

                    var sourcePathData = trafficNodeConversionSystem.PathConnectingMap[globalIndex1];
                    var targetPathData = trafficNodeConversionSystem.PathConnectingMap[globalIndex2];

                    if (sourcePathData.PathRoadType == PathRoadType.StraightRoad && targetPathData.PathRoadType == PathRoadType.StraightRoad)
                    {
                        if (!pathParallelMap.ContainsKey(globalIndex1))
                        {
                            pathParallelMap.Add(globalIndex1, new List<int>());
                        }

                        pathParallelMap[globalIndex1].Add(globalIndex2);
                    }
                }
            }
        }

        private int TryToGetTargetPathIndexByInstanceId(in NativeArray<Entity> trafficNodeScopes, int pathInstanceId, int ignoreScopeIndex = -1, bool includeLeftLanes = false)
        {
            for (int i = 0; i < trafficNodeScopes.Length; i++)
            {
                if (i == ignoreScopeIndex)
                    continue;

                var scopeEntity = trafficNodeScopes[i];
                var trafficNodeScopeData = EntityManager.GetComponentData<TrafficNodeScopeBakingData>(scopeEntity);

                for (int laneIndex = 0; laneIndex < trafficNodeScopeData.RightLaneEntities.Length; laneIndex++)
                {
                    var rightLaneData = trafficNodeScopeData.RightLaneEntities[laneIndex];

                    for (int pathIndex = rightLaneData.MinPathSettingsIndex; pathIndex < rightLaneData.MaxPathSettingsIndex; pathIndex++)
                    {
                        var tempPathSettingsData = trafficNodeScopeData.PathTempSettingsDatas[pathIndex];

                        if (tempPathSettingsData.InstanceId == pathInstanceId)
                        {
                            return tempPathSettingsData.GlobalPathIndex;
                        }
                    }

                    if (includeLeftLanes && trafficNodeScopeData.IsCreated(true) && trafficNodeScopeData.LeftLaneEntities.Length > laneIndex)
                    {
                        var leftLaneData = trafficNodeScopeData.LeftLaneEntities[laneIndex];

                        for (int pathIndex = leftLaneData.MinPathSettingsIndex; pathIndex < leftLaneData.MaxPathSettingsIndex; pathIndex++)
                        {
                            var tempPathSettingsData = trafficNodeScopeData.PathTempSettingsDatas[pathIndex];

                            if (tempPathSettingsData.InstanceId == pathInstanceId)
                            {
                                return tempPathSettingsData.GlobalPathIndex;
                            }
                        }
                    }
                }
            }

            return -1;
        }

        private void CreateIntersectMapLane(in NativeArray<Entity> trafficNodeScopes)
        {
            for (int scopeIndex = 0; scopeIndex < trafficNodeScopes.Length; scopeIndex++)
            {
                var scopeEntity = trafficNodeScopes[scopeIndex];

                if (!EntityManager.HasComponent<TrafficNodeScopeBakingData>(scopeEntity)) continue;

                var trafficNodeScopeData = EntityManager.GetComponentData<TrafficNodeScopeBakingData>(scopeEntity);

                for (int laneIndex = 0; laneIndex < trafficNodeScopeData.RightLaneEntities.Length; laneIndex++)
                {
                    ref var rightLaneData = ref trafficNodeScopeData.RightLaneEntities;
                    IterateIntersectionsLanes(ref trafficNodeScopeData, ref rightLaneData, laneIndex);
                }

                if (trafficNodeScopeData.IsCreated(true))
                {
                    for (int laneIndex = 0; laneIndex < trafficNodeScopeData.LeftLaneEntities.Length; laneIndex++)
                    {
                        ref var leftLaneData = ref trafficNodeScopeData.LeftLaneEntities;
                        IterateIntersectionsLanes(ref trafficNodeScopeData, ref leftLaneData, laneIndex);
                    }
                }
            }
        }

        private void IterateIntersectionsLanes(ref TrafficNodeScopeBakingData trafficNodeScopeData, ref NativeArray<TrafficNodeTempData> laneEntities, int laneIndex)
        {
            if (!laneEntities.IsCreated || laneEntities.Length <= laneIndex)
                return;

            var laneData = laneEntities[laneIndex];

            for (int pathIndex = laneData.MinPathSettingsIndex; pathIndex < laneData.MaxPathSettingsIndex; pathIndex++)
            {
                var tempPathSettingsData = trafficNodeScopeData.PathTempSettingsDatas[pathIndex];
                var sourcePathIndex = tempPathSettingsData.GlobalPathIndex;

                for (int intersectIndex = tempPathSettingsData.MinIntersectPointIndex; intersectIndex < tempPathSettingsData.MaxIntersectPointIndex; intersectIndex++)
                {
                    var intersectPointInfo = trafficNodeScopeData.IntersectPoints[intersectIndex];

                    if (!trafficNodeConversionSystem.InstanceIdToGlobalIndexPathMap.ContainsKey(intersectPointInfo.IntersectedPathInstanceId))
                    {
                        UnityEngine.Debug.Log($"InstanceIdToGlobalIndexPathMap {processed} NOT contains PathInstanceId {intersectPointInfo.IntersectedPathInstanceId}{TrafficObjectFinderMessage.GetMessage()}");
                    }
                    else
                    {
                        processed++;
                    }

                    var targetPathIndex = trafficNodeConversionSystem.InstanceIdToGlobalIndexPathMap[intersectPointInfo.IntersectedPathInstanceId];
                    var intersectPoint = intersectPointInfo.Point;

                    if (!pathIntersectMap.ContainsKey(sourcePathIndex))
                    {
                        pathIntersectMap.Add(sourcePathIndex, new List<IntersectPathInfo>());
                    }

                    var intersectPathInfo = new IntersectPathInfo()
                    {
                        IntersectedPathIndex = targetPathIndex,
                        IntersectPosition = intersectPoint,
                        LocalNodeIndex = intersectPointInfo.LocalNodeIndex,
                    };

                    bool matched = false;

                    for (int mapIndex = 0; mapIndex < pathIntersectMap[sourcePathIndex].Count; mapIndex++)
                    {
                        if (pathIntersectMap[sourcePathIndex][mapIndex].IntersectedPathIndex == targetPathIndex)
                        {
                            matched = true;
                            break;
                        }
                    }

                    if (!matched)
                        pathIntersectMap[sourcePathIndex].Add(intersectPathInfo);
                }
            }
        }
    }
}