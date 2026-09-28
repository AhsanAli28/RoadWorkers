using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Level.Streaming;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Road.Authoring
{
    [RequireMatchingQueriesForUpdate]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateAfter(typeof(TrafficLightConversionSystem))]
    [UpdateInGroup(typeof(BakingSystemGroup))]
    public partial class TrafficNodeConversionSystem : SimpleSystemBase
    {
        public struct PathBlobDataTemp
        {
            public int InstanceId;
            public List<int> ConnectedIndexes;
            public List<RouteNodeData> Nodes;

            public float PathLength;
            public int ConnectedPathInstanceId;
            public int ConnectedPathIndex;
            public int SourceLaneIndex;
            public int Priority;

            public PathOptions Options;
            public PathCurveType PathCurveType;
            public PathRoadType PathRoadType;
            public PathConnectionType PathConnectionType;
            public TrafficGroupType TrafficGroup;
        }

        private EntityQuery trafficRoadConfigReferenceQuery;
        private TrafficLightConversionSystem trafficLightConversionSystem;
        private int globalPathIndex = 0;
        private int crosswalkIndex = 0;
        private List<RouteNodeData> tempRouteNodes = new List<RouteNodeData>();
        private Dictionary<Entity, TrafficNodeDynamicConnection> tempConnectionCache = new Dictionary<Entity, TrafficNodeDynamicConnection>();

        public Dictionary<int, int> InstanceIdToGlobalIndexPathMap { get; private set; } = new Dictionary<int, int>();
        public List<PathBlobDataTemp> PathConnectingMap { get; private set; } = new List<PathBlobDataTemp>();
        public int NodeIndex { get; private set; }

        protected override void OnCreate()
        {
            base.OnCreate();

            trafficRoadConfigReferenceQuery = EntityManager.CreateEntityQuery(typeof(TrafficRoadConfigReference));
            trafficLightConversionSystem = World.GetOrCreateSystemManaged<TrafficLightConversionSystem>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Dispose();
        }

        protected override void OnStopRunning()
        {
            base.OnStopRunning();
            Dispose();
        }

        protected override void OnUpdate()
        {
            Dispose();

            trafficLightConversionSystem.GetDependency().Complete();

            InitTrafficNodeIndexes();

            InitPathIndexesAndConnection();
        }

        public void Dispose()
        {
            NodeIndex = 0;
            globalPathIndex = 0;
            crosswalkIndex = 0;
            PathConnectingMap.Clear();
            tempConnectionCache.Clear();
            InstanceIdToGlobalIndexPathMap.Clear();
        }

        private void InitTrafficNodeIndexes()
        {
            var trafficRoadConfigReference = trafficRoadConfigReferenceQuery.GetSingleton<TrafficRoadConfigReference>();

            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            var config = SystemAPI.GetSingleton<RoadStreamingConfigReference>().Config.Value;

            Entities
            .WithoutBurst()
            .ForEach((
                Entity entity,
                ref TrafficNodeScopeBakingData trafficNodeScopeData,
                in TrafficNodeCrossroadRef trafficNodeCrossroadRef) =>
            {
                trafficNodeScopeData.CrossWalkIndex = crosswalkIndex++;

                if (trafficNodeScopeData.RightLaneEntities.IsCreated)
                {
                    for (int laneIndex = 0; laneIndex < trafficNodeScopeData.RightLaneEntities.Length; laneIndex++)
                    {
                        var rightLaneEntity = trafficNodeScopeData.RightLaneEntities[laneIndex].Entity;
                        InitTrafficNode(
                            ref commandBuffer,
                            laneIndex,
                            ref trafficNodeScopeData,
                            rightLaneEntity,
                            in trafficNodeCrossroadRef,
                            in trafficRoadConfigReference,
                            in config);
                    }
                }

                if (trafficNodeScopeData.LeftLaneEntities.IsCreated)
                {
                    bool temp = false;

                    for (int laneIndex = 0; laneIndex < trafficNodeScopeData.LeftLaneEntities.Length; laneIndex++)
                    {
                        var leftLaneEntity = trafficNodeScopeData.LeftLaneEntities[laneIndex].Entity;

                        if (!InitTrafficNode(
                            ref commandBuffer,
                            laneIndex,
                            ref trafficNodeScopeData,
                            leftLaneEntity,
                            in trafficNodeCrossroadRef,
                            in trafficRoadConfigReference,
                            in config,
                            true))
                        {
                            temp = true;
                        }
                    }

                    if (temp)
                    {
                        trafficNodeScopeData.TempNode = true;
                        EntityManager.SetComponentData(entity, trafficNodeScopeData);
                    }
                }

            }).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }

        private bool InitTrafficNode(
            ref EntityCommandBuffer commandBuffer,
            int laneIndex,
            ref TrafficNodeScopeBakingData trafficNodeScopeData,
            Entity nodeLaneEntity,
            in TrafficNodeCrossroadRef trafficNodeCrossroadRef,
            in TrafficRoadConfigReference trafficRoadConfigReference,
            in RoadStreamingConfig config,
            bool leftLanes = false)
        {
            var mergeNode = leftLanes && trafficNodeScopeData.HasMerge;

            if (mergeNode)
            {
                var pathConnectionBuffer = EntityManager.GetBuffer<PathConnectionElement>(nodeLaneEntity);

                if (pathConnectionBuffer.Length > 0 && pathConnectionBuffer[0].ConnectedNodeEntity != Entity.Null)
                {
                    bool destroyLane = true;

                    if (config.StreamingIsEnabled)
                    {
                        if (trafficNodeCrossroadRef.CurrentRelatedCrossroad != Entity.Null)
                        {
                            var connectedScopeEntity = pathConnectionBuffer[0].ConnectedNodeEntity;

                            var connectedTrafficNodeScope = EntityManager.GetComponentData<TrafficNodeScopeBakingData>(connectedScopeEntity);

                            var crossroadEntity = BakerExtension.GetEntity(EntityManager, trafficNodeCrossroadRef.CurrentRelatedCrossroad);
                            var crossroadBakingData = EntityManager.GetComponentData<CrossroadBakingData>(crossroadEntity);

                            var connectedCrossing2 = EntityManager.GetComponentData<TrafficNodeCrossroadRef>(connectedScopeEntity);
                            var crossroadEntity2 = BakerExtension.GetEntity(EntityManager, connectedCrossing2.CurrentRelatedCrossroad);

                            var crossroadBakingData2 = EntityManager.GetComponentData<CrossroadBakingData>(crossroadEntity2);

                            if (crossroadBakingData2.SectionIndex != crossroadBakingData.SectionIndex)
                            {
                                destroyLane = false;
                            }
                        }
                    }

                    if (destroyLane)
                    {
                        commandBuffer.AddComponent<DestroyNodeEditorTag>(nodeLaneEntity);
                        return false;
                    }
                }
            }

            var trafficNodeComponent = EntityManager.GetComponentData<TrafficNodeComponent>(nodeLaneEntity);
            var trafficNodeSettingsComponent = EntityManager.GetComponentData<TrafficNodeSettingsComponent>(nodeLaneEntity);
            var trafficNodeCapacityComponent = EntityManager.GetComponentData<TrafficNodeCapacityComponent>(nodeLaneEntity);

            int crossRoadIndex = -1;

            Entity lightEntity = Entity.Null;

            if (trafficNodeComponent.LightEntity != Entity.Null)
            {
                lightEntity = BakerExtension.GetEntity(EntityManager, trafficNodeComponent.LightEntity);

                if (lightEntity != Entity.Null && EntityManager.HasComponent<LightHandlerComponent>(lightEntity))
                {
                    LightHandlerComponent lightComponent = EntityManager.GetComponentData<LightHandlerComponent>(lightEntity);
                    crossRoadIndex = lightComponent.CrossRoadIndex;
                }
                else
                {
                    UnityEngine.Debug.Log($"TrafficNodeConversionSystem. TrafficNode InstanceID {trafficNodeScopeData.TrafficNodeInstanceId} TrafficNodeEntity {nodeLaneEntity} linked TrafficLightHandler lightEntity {lightEntity} doesn't have LightHandlerComponent component. Make sure that TrafficLightHandler is enabled & assigned to TrafficLightCrossroad{TrafficObjectFinderMessage.GetMessage()}");
                }
            }

            trafficNodeComponent.CrossRoadIndex = crossRoadIndex;
            trafficNodeComponent.LightEntity = lightEntity;

            if (trafficNodeSettingsComponent.TrafficNodeType != TrafficNodeType.Default)
            {
                var trafficNodeTypeFlag = 1 << (int)trafficNodeSettingsComponent.TrafficNodeType;

                if (trafficNodeSettingsComponent.AllowedRouteRandomizeSpawning)
                {
                    trafficNodeSettingsComponent.AllowedRouteRandomizeSpawning = (trafficRoadConfigReference.Config.Value.IsAvailableForRouteRandomizeSpawningFlags & trafficNodeTypeFlag) != 0;
                }

                trafficNodeSettingsComponent.IsAvailableForSpawn = !mergeNode && (trafficRoadConfigReference.Config.Value.IsAvailableForSpawnFlags & trafficNodeTypeFlag) != 0;
                trafficNodeSettingsComponent.IsAvailableForSpawnTarget = (trafficRoadConfigReference.Config.Value.IsAvailableForSpawnTargetFlags & trafficNodeTypeFlag) != 0;
                trafficNodeCapacityComponent.Capacity = (trafficRoadConfigReference.Config.Value.LinkedNodeFlags & trafficNodeTypeFlag) != 0 ? 1 : -1;
            }

            EntityManager.SetComponentData(nodeLaneEntity, trafficNodeComponent);
            EntityManager.SetComponentData(nodeLaneEntity, trafficNodeSettingsComponent);
            EntityManager.SetComponentData(nodeLaneEntity, trafficNodeCapacityComponent);
            NodeIndex++;
            return true;
        }

        private void InitPathIndexesAndConnection()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            var config = SystemAPI.GetSingleton<RoadStreamingConfigReference>().Config.Value;
            var statEntity = SystemAPI.GetSingletonEntity<RoadStatConfig>();
            var roadStatConfig = SystemAPI.GetSingleton<RoadStatConfig>();
            var citySpawnConfig = SystemAPI.GetSingleton<CitySpawnConfigReference>();

            roadStatConfig.TrafficNodeTotal = 0;
            roadStatConfig.TrafficNodeDynamicStreaming = 0;
            roadStatConfig.TrafficNodeStreamingTotal = 0;
            roadStatConfig.TrafficNodePassiveConnection = 0;

            Entities
            .WithoutBurst()
            .ForEach((
                Entity entity,
                ref TrafficNodeScopeBakingData trafficNodeScopeData,
                in TrafficNodeCrossroadRef trafficNodeCrossroadRef) =>
            {
                ref var rightLaneEntities = ref trafficNodeScopeData.RightLaneEntities;

                for (int laneIndex = 0; laneIndex < rightLaneEntities.Length; laneIndex++)
                {
                    roadStatConfig.TrafficNodeTotal++;

                    InitPaths(
                        ref commandBuffer,
                        ref trafficNodeScopeData,
                        in trafficNodeCrossroadRef,
                        ref rightLaneEntities,
                        ref config,
                        ref roadStatConfig,
                        in citySpawnConfig,
                        laneIndex,
                        true);
                }

                ref var leftLaneEntities = ref trafficNodeScopeData.LeftLaneEntities;

                for (int laneIndex = 0; laneIndex < leftLaneEntities.Length; laneIndex++)
                {
                    if (InitPaths(
                        ref commandBuffer,
                        ref trafficNodeScopeData,
                        in trafficNodeCrossroadRef,
                        ref leftLaneEntities,
                        ref config,
                        ref roadStatConfig,
                        in citySpawnConfig,
                        laneIndex,
                        false))
                    {
                        roadStatConfig.TrafficNodeTotal++;
                    }
                }

            }).Run();

            foreach (var item in tempConnectionCache)
            {
                commandBuffer.AddComponent(item.Key, item.Value);
            }

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();

            CreateStatConfig(statEntity, roadStatConfig);
        }

        private bool InitPaths(
            ref EntityCommandBuffer commandBuffer,
            ref TrafficNodeScopeBakingData trafficNodeScopeData,
            in TrafficNodeCrossroadRef trafficNodeCrossroadRef,
            ref NativeArray<TrafficNodeTempData> laneEntities,
            ref RoadStreamingConfig config,
            ref RoadStatConfig roadStatConfig,
            in CitySpawnConfigReference citySpawnConfig,
            int laneIndex,
            bool isRightLaneDirection)
        {
            if (!laneEntities.IsCreated || laneIndex >= laneEntities.Length)
                return false;

            var laneEntity = laneEntities[laneIndex].Entity;

            if (!isRightLaneDirection)
            {
                if (EntityManager.HasComponent<DestroyNodeEditorTag>(laneEntity))
                    return false;
            }

            commandBuffer.AddComponent(laneEntity, CullComponentsExtension.GetComponentSet(citySpawnConfig.Config.Value.TrafficNodeStateList));

            var trafficNodeComponent = EntityManager.GetComponentData<TrafficNodeComponent>(laneEntity);

            var pathConnectionBuffer = EntityManager.GetBuffer<PathConnectionElement>(laneEntity);
            var minPathIndex = laneEntities[laneIndex].MinPathSettingsIndex;
            var maxPathIndex = laneEntities[laneIndex].MaxPathSettingsIndex;

            int sourceHash = -1;

            CrossroadBakingData crossroadBakingData = default;

            if (trafficNodeCrossroadRef.CurrentRelatedCrossroad != Entity.Null && config.StreamingIsEnabled)
            {
                var crossroadEntity = BakerExtension.GetEntity(EntityManager, trafficNodeCrossroadRef.CurrentRelatedCrossroad);
                crossroadBakingData = EntityManager.GetComponentData<CrossroadBakingData>(crossroadEntity);
                sourceHash = crossroadBakingData.PositionHash;

                commandBuffer.AddSharedComponent(laneEntity, new SceneSection()
                {
                    SceneGUID = crossroadBakingData.SceneHashCode,
                    Section = crossroadBakingData.SectionIndex
                });
            }

            for (int i = minPathIndex; i < maxPathIndex; i++)
            {
                var tempPathSettings = trafficNodeScopeData.PathTempSettingsDatas[i];
                var localBufferIndex = i - minPathIndex;

                var minSubNodeIndex = tempPathSettings.MinSubNodeIndex;
                var maxSubNodeIndex = tempPathSettings.MaxSubNodeIndex;

                var subNodeCount = maxSubNodeIndex - minSubNodeIndex;

                var pathConnection = pathConnectionBuffer[localBufferIndex];

                var connectedLaneNodeEntity = Entity.Null;
                TrafficNodeScopeBakingData connectedTrafficNodeScope = default;

                if (EntityManager.HasComponent<TrafficNodeScopeBakingData>(pathConnection.ConnectedNodeEntity))
                {
                    connectedLaneNodeEntity = pathConnection.ConnectedNodeEntity;
                    connectedTrafficNodeScope = EntityManager.GetComponentData<TrafficNodeScopeBakingData>(pathConnection.ConnectedNodeEntity);

                    var reversedConnection = tempPathSettings.ReversedConnection;

                    if (!isRightLaneDirection)
                    {
                        reversedConnection = !reversedConnection;
                    }

                    if (tempPathSettings.ConnectedLaneIndex != -1)
                    {
                        if (!connectedTrafficNodeScope.IsOneWay && (!connectedTrafficNodeScope.HasMerge || connectedTrafficNodeScope.HasMerge && !connectedTrafficNodeScope.TempNode))
                        {
                            if (!reversedConnection)
                            {
                                if (connectedTrafficNodeScope.LeftLaneEntities.Length > tempPathSettings.ConnectedLaneIndex)
                                {
                                    connectedLaneNodeEntity = connectedTrafficNodeScope.LeftLaneEntities[tempPathSettings.ConnectedLaneIndex].Entity;
                                }
                                else
                                {
#if UNITY_EDITOR
                                    UnityEngine.Debug.Log($"TrafficNodeConversionSystem. InitPaths. Source ReversedConnection {tempPathSettings.ReversedConnection}. Current ReversedConnection {reversedConnection}. ConnectedTrafficNodeScope LeftLane connectedLaneNodeEntity not found. LeftLaneEntities {connectedTrafficNodeScope.LeftLaneEntities.Length} > ConnectedLaneIndex {tempPathSettings.ConnectedLaneIndex}. Source TrafficNodeInstanceId {trafficNodeScopeData.TrafficNodeInstanceId}. Connected TrafficNodeInstanceId {connectedTrafficNodeScope.TrafficNodeInstanceId}. Path InstanceId {tempPathSettings.InstanceId}{TrafficObjectFinderMessage.GetMessage()}");
#endif
                                }
                            }
                            else
                            {
                                if (connectedTrafficNodeScope.RightLaneEntities.Length > tempPathSettings.ConnectedLaneIndex)
                                {
                                    connectedLaneNodeEntity = connectedTrafficNodeScope.RightLaneEntities[tempPathSettings.ConnectedLaneIndex].Entity;
                                }
                                else
                                {
#if UNITY_EDITOR
                                    UnityEngine.Debug.Log($"TrafficNodeConversionSystem. InitPaths. Source ReversedConnection {tempPathSettings.ReversedConnection}. Current ReversedConnection {reversedConnection}. ConnectedTrafficNodeScope RightLane connectedLaneNodeEntity not found. RightLaneEntities {connectedTrafficNodeScope.RightLaneEntities.Length} > ConnectedLaneIndex {tempPathSettings.ConnectedLaneIndex}. Source TrafficNodeInstanceId {trafficNodeScopeData.TrafficNodeInstanceId}. Connected TrafficNodeInstanceId {connectedTrafficNodeScope.TrafficNodeInstanceId}. Path InstanceId {tempPathSettings.InstanceId}{TrafficObjectFinderMessage.GetMessage()}");
#endif
                                }
                            }
                        }
                        else
                        {
                            bool assigned = false;

                            if (connectedTrafficNodeScope.HasMerge && connectedTrafficNodeScope.TempNode)
                            {
                                connectedTrafficNodeScope = EntityManager.GetComponentData<TrafficNodeScopeBakingData>(connectedTrafficNodeScope.MergeScopeEntity);
                                var scopeEntities = connectedTrafficNodeScope.GetMainLaneEntities();

                                if (tempPathSettings.ConnectedLaneIndex < scopeEntities.Length)
                                {
                                    connectedLaneNodeEntity = scopeEntities[tempPathSettings.ConnectedLaneIndex].Entity;
                                }
                                else
                                {
                                    UnityEngine.Debug.Log($"TrafficNodeConversionSystem. TrafficNodeInstanceId {connectedTrafficNodeScope.TrafficNodeInstanceId} corrupted. The direction of the merged traffic nodes may be wrong.");
                                }

                                assigned = true;
                            }

                            if (!assigned)
                            {
                                var scopeEntities = connectedTrafficNodeScope.GetMainLaneEntities();

                                if (scopeEntities.Length > tempPathSettings.ConnectedLaneIndex)
                                {
                                    connectedLaneNodeEntity = scopeEntities[tempPathSettings.ConnectedLaneIndex].Entity;
                                }
                                else
                                {
#if UNITY_EDITOR
                                    UnityEngine.Debug.Log($"TrafficNodeConversionSystem. InitPaths. ConnectedTrafficNodeScope Oneway connectedLaneNodeEntity not found. Entities {scopeEntities.Length} > ConnectedLaneIndex {tempPathSettings.ConnectedLaneIndex}. Source TrafficNodeInstanceId {trafficNodeScopeData.TrafficNodeInstanceId}. Connected TrafficNodeInstanceId {connectedTrafficNodeScope.TrafficNodeInstanceId}. Path InstanceId {tempPathSettings.InstanceId}{TrafficObjectFinderMessage.GetMessage()}");
#endif
                                }
                            }
                        }
                    }
                    else
                    {
#if UNITY_EDITOR
                        UnityEngine.Debug.Log($"Path InstanceId {tempPathSettings.InstanceId} ConnectedLaneIndex -1");
#endif
                    }
                }
                else
                {
#if UNITY_EDITOR
                    UnityEngine.Debug.Log($"Path InstanceId {tempPathSettings.InstanceId} connected entity {pathConnection.ConnectedNodeEntity.Index} doesn't have TrafficNodeScope. Make sure, that the connected TrafficNode InstanceId {tempPathSettings.ConnectedNodeInstanceId} is properly configured.{TrafficObjectFinderMessage.GetMessage()}");
#endif
                }

                pathConnection.GlobalPathIndex = globalPathIndex++;

                if (pathConnection.CustomLightEntity != Entity.Null)
                {
                    pathConnection.CustomLightEntity = BakerExtension.GetEntity(EntityManager, pathConnection.CustomLightEntity);
                }

                tempPathSettings.GlobalPathIndex = pathConnection.GlobalPathIndex;

                if (tempPathSettings.InstanceId != -1)
                {
                    if (!InstanceIdToGlobalIndexPathMap.ContainsKey(tempPathSettings.InstanceId))
                    {
                        InstanceIdToGlobalIndexPathMap.Add(tempPathSettings.InstanceId, tempPathSettings.GlobalPathIndex);
                    }
                    else
                    {
                        UnityEngine.Debug.Log($"TrafficNodeConversionSystem. InstanceIdToGlobalIndexPathMap duplicate. " +
                            $"Added InstanceId:GlobalPathIndex '{tempPathSettings.InstanceId}:{InstanceIdToGlobalIndexPathMap[tempPathSettings.InstanceId]}'. " +
                            $"Trying to add InstanceId:GlobalPathIndex '{tempPathSettings.InstanceId}:{tempPathSettings.GlobalPathIndex}'{TrafficObjectFinderMessage.GetMessage()}");
                    }
                }

                var connectedScopeEntity = pathConnection.ConnectedNodeEntity;
                pathConnection.ConnectedNodeEntity = connectedLaneNodeEntity;
                var currentConnectedLaneEntity = connectedLaneNodeEntity;

                if (connectedLaneNodeEntity != Entity.Null)
                {
                    var connectedNodePos = EntityManager.GetComponentData<LocalTransform>(connectedLaneNodeEntity).Position.Flat();
                    var connectedNodeHash = HashMapHelper.GetHashMapPosition(connectedNodePos, config.NodeCellSize);

                    int localSubNodeIndex = 0;

                    for (int subNodeIndex = minSubNodeIndex; subNodeIndex < maxSubNodeIndex; subNodeIndex++)
                    {
                        roadStatConfig.TrafficNodeTotal++;

                        var subNodeData = trafficNodeScopeData.SubNodes[subNodeIndex];
                        var subNodeEntity = subNodeData.Entity;
                        var subPathConnectionBuffer = EntityManager.GetBuffer<PathConnectionElement>(subNodeEntity);
                        var sourceSubHash = -1;

                        commandBuffer.AddComponent(subNodeEntity, CullComponentsExtension.GetComponentSet(citySpawnConfig.Config.Value.TrafficNodeStateList));

                        var subNodePos = EntityManager.GetComponentData<LocalTransform>(subNodeEntity).Position.Flat();
                        var sourceSubNodeHash = HashMapHelper.GetHashMapPosition(subNodePos, config.NodeCellSize);

                        bool streamingEnabled = config.StreamingIsEnabled;

                        if (streamingEnabled)
                        {
                            if (subNodeCount > 0)
                            {
                                streamingEnabled = crossroadBakingData.HasSubNodes;

                                if (!streamingEnabled)
                                {
                                    UnityEngine.Debug.Log($"TrafficNodeConversionSystem. Road streaming enabled, but sub nodes appear not to be baked for crossroad InstanceId '{crossroadBakingData.InstanceId}', make sure you have baked path data in the road parent.{TrafficObjectFinderMessage.GetMessage()}");
                                }
                            }
                        }

                        if (streamingEnabled)
                        {
                            var subCrossroadRef = EntityManager.GetComponentData<TrafficNodeCrossroadRef>(subNodeEntity);

                            if (subCrossroadRef.CurrentRelatedCrossroad != Entity.Null)
                            {
                                var crossroadEntity = BakerExtension.GetEntity(EntityManager, subCrossroadRef.CurrentRelatedCrossroad);
                                var subCrossroadBakingData = EntityManager.GetComponentData<CrossroadBakingData>(crossroadEntity);
                                sourceSubHash = subCrossroadBakingData.PositionHash;

                                commandBuffer.AddSharedComponent(subNodeEntity, new SceneSection()
                                {
                                    SceneGUID = subCrossroadBakingData.SceneHashCode,
                                    Section = subCrossroadBakingData.SectionIndex
                                });
                            }
                        }

                        Entity subConnectedNodeEntity = default;

                        bool last = false;

                        if (localSubNodeIndex + 1 < subNodeCount)
                        {
                            var nextSubNodeData = trafficNodeScopeData.SubNodes[subNodeIndex + 1];
                            subConnectedNodeEntity = nextSubNodeData.Entity;
                        }
                        else
                        {
                            last = true;
                            subConnectedNodeEntity = connectedLaneNodeEntity;
                        }

                        var subConnectedNodePos = EntityManager.GetComponentData<LocalTransform>(subConnectedNodeEntity).Position.Flat();
                        var subConnectedNodeHash = HashMapHelper.GetHashMapPosition(subConnectedNodePos, config.NodeCellSize);

                        if (last && connectedTrafficNodeScope.HasMerge && !connectedTrafficNodeScope.TempNode)
                        {
                            subConnectedNodeHash++;
                        }

                        if (localSubNodeIndex == 0)
                        {
                            var startEntity = trafficNodeScopeData.SubNodes[minSubNodeIndex].Entity;
                            var subConnectedNodePos2 = EntityManager.GetComponentData<LocalTransform>(startEntity).Position.Flat();
                            var subConnectedNodeHash2 = HashMapHelper.GetHashMapPosition(subConnectedNodePos2, config.NodeCellSize);

                            pathConnection.ConnectedSubNodeEntity = startEntity;
                            pathConnection.ConnectedSubHash = subConnectedNodeHash2;

                            currentConnectedLaneEntity = startEntity;

                            pathConnectionBuffer[localBufferIndex] = pathConnection;
                        }

                        if (streamingEnabled)
                        {
                            TrafficNodeCrossroadRef subConnectedTrafficNodeCrossroadRef;

                            if (!last)
                            {
                                subConnectedTrafficNodeCrossroadRef = EntityManager.GetComponentData<TrafficNodeCrossroadRef>(subConnectedNodeEntity);
                            }
                            else
                            {
                                // Child scope lane entities doesn't have TrafficNodeCrossroadRef
                                subConnectedTrafficNodeCrossroadRef = EntityManager.GetComponentData<TrafficNodeCrossroadRef>(connectedScopeEntity);
                            }

                            var subConnectedCrossroadEntity = BakerExtension.GetEntity(EntityManager, subConnectedTrafficNodeCrossroadRef.CurrentRelatedCrossroad);
                            var subConnectedCrossroad = EntityManager.GetComponentData<CrossroadBakingData>(subConnectedCrossroadEntity);

                            commandBuffer.AddSharedComponent(subConnectedNodeEntity, new SceneSection()
                            {
                                SceneGUID = subConnectedCrossroad.SceneHashCode,
                                Section = subConnectedCrossroad.SectionIndex
                            });

                            // Subnode to subnode connection
                            if (subConnectedCrossroad.PositionHash != sourceSubHash)
                            {
                                var connection = new TrafficNodeDynamicConnection()
                                {
                                    ConnectionType = ConnectionType.StreamingConnection,
                                    SegmentHash = crossroadBakingData.PositionHash,
                                    PositionHash = sourceSubNodeHash,
                                    SubNode = true
                                };

                                if (!tempConnectionCache.ContainsKey(subNodeEntity))
                                {
                                    tempConnectionCache.Add(subNodeEntity, connection);
                                    roadStatConfig.TrafficNodeDynamicStreaming++;
                                    roadStatConfig.TrafficNodeStreamingTotal++;
                                }
                                else
                                {
                                    var existConnected = tempConnectionCache[subNodeEntity];

                                    if (!existConnected.ConnectionType.HasFlag(ConnectionType.StreamingConnection))
                                        roadStatConfig.TrafficNodeDynamicStreaming++;

                                    existConnected.ConnectionType |= connection.ConnectionType;

                                    tempConnectionCache[subNodeEntity] = existConnected;
                                }

                                var flags = ConnectionType.ExternalStreamingConnection;

                                if (!last)
                                {
                                    flags |= ConnectionType.StreamingConnection;
                                }

                                var connectedData = new TrafficNodeDynamicConnection()
                                {
                                    ConnectionType = flags,
                                    SegmentHash = subConnectedCrossroad.PositionHash,
                                    PositionHash = subConnectedNodeHash,
                                    SubNode = !last
                                };

                                if (!tempConnectionCache.ContainsKey(subConnectedNodeEntity))
                                {
                                    roadStatConfig.TrafficNodePassiveConnection++;
                                    roadStatConfig.TrafficNodeStreamingTotal++;

                                    if (!last)
                                        roadStatConfig.TrafficNodeDynamicStreaming++;

                                    tempConnectionCache.Add(subConnectedNodeEntity, connectedData);
                                }
                                else
                                {
                                    var existConnected = tempConnectionCache[subConnectedNodeEntity];

                                    if (!existConnected.ConnectionType.HasFlag(ConnectionType.StreamingConnection))
                                        roadStatConfig.TrafficNodeDynamicStreaming++;

                                    if (!existConnected.ConnectionType.HasFlag(ConnectionType.ExternalStreamingConnection))
                                        roadStatConfig.TrafficNodePassiveConnection++;

                                    existConnected.ConnectionType |= connectedData.ConnectionType;

                                    tempConnectionCache[subConnectedNodeEntity] = existConnected;
                                }
                            }

                            var connectedTrafficNodeCrossroadRefLocal = EntityManager.GetComponentData<TrafficNodeCrossroadRef>(connectedScopeEntity);
                            var сnnectedCrossroadEntity = BakerExtension.GetEntity(EntityManager, connectedTrafficNodeCrossroadRefLocal.CurrentRelatedCrossroad);
                            var сonnectedCrossroad = EntityManager.GetComponentData<CrossroadBakingData>(сnnectedCrossroadEntity);

                            // Subnode to lane node connection
                            if (сonnectedCrossroad.PositionHash != sourceSubHash)
                            {
                                var connection = new TrafficNodeDynamicConnection()
                                {
                                    ConnectionType = ConnectionType.StreamingConnection,
                                    SegmentHash = crossroadBakingData.PositionHash,
                                    PositionHash = sourceSubNodeHash,
                                    SubNode = true
                                };

                                if (!tempConnectionCache.ContainsKey(subNodeEntity))
                                {
                                    tempConnectionCache.Add(subNodeEntity, connection);
                                    roadStatConfig.TrafficNodeDynamicStreaming++;
                                    roadStatConfig.TrafficNodeStreamingTotal++;
                                }
                                else
                                {
                                    var existConnected = tempConnectionCache[subNodeEntity];

                                    existConnected.ConnectionType |= connection.ConnectionType;

                                    tempConnectionCache[subNodeEntity] = existConnected;
                                }

                                var flags = ConnectionType.ExternalStreamingConnection;

                                var connectedData = new TrafficNodeDynamicConnection()
                                {
                                    ConnectionType = flags,
                                    SegmentHash = сonnectedCrossroad.PositionHash,
                                    PositionHash = connectedNodeHash,
                                    SubNode = false
                                };

                                if (!tempConnectionCache.ContainsKey(currentConnectedLaneEntity))
                                {
                                    roadStatConfig.TrafficNodePassiveConnection++;
                                    roadStatConfig.TrafficNodeStreamingTotal++;

                                    tempConnectionCache.Add(currentConnectedLaneEntity, connectedData);
                                }
                                else
                                {
                                    var existConnected = tempConnectionCache[currentConnectedLaneEntity];

                                    if (!existConnected.ConnectionType.HasFlag(ConnectionType.StreamingConnection))
                                        roadStatConfig.TrafficNodePassiveConnection++;

                                    existConnected.ConnectionType |= connectedData.ConnectionType;

                                    tempConnectionCache[currentConnectedLaneEntity] = existConnected;
                                }
                            }
                        }

                        subPathConnectionBuffer.Add(new PathConnectionElement()
                        {
                            GlobalPathIndex = pathConnection.GlobalPathIndex,
                            StartLocalNodeIndex = subNodeData.LocalWaypointIndex,
                            ConnectedNodeEntity = connectedLaneNodeEntity,
                            ConnectedSubNodeEntity = subConnectedNodeEntity,
                            ConnectedHash = connectedNodeHash,
                            ConnectedSubHash = subConnectedNodeHash,
                        });

                        localSubNodeIndex++;
                    }

                    TrafficNodeCrossroadRef connectedTrafficNodeCrossroadRef = default;

                    var currentConnectedHash = -1;
                    var connectedEntity = Entity.Null;

                    if (config.StreamingIsEnabled)
                    {
                        if (pathConnection.ConnectedSubNodeEntity != Entity.Null)
                        {
                            var subConnectedNodePos = EntityManager.GetComponentData<LocalTransform>(pathConnection.ConnectedSubNodeEntity).Position.Flat();
                            currentConnectedHash = HashMapHelper.GetHashMapPosition(subConnectedNodePos, config.NodeCellSize);
                            connectedEntity = pathConnection.ConnectedSubNodeEntity;

                            if (EntityManager.HasComponent<TrafficNodeCrossroadRef>(connectedEntity))
                            {
                                connectedTrafficNodeCrossroadRef = EntityManager.GetComponentData<TrafficNodeCrossroadRef>(connectedEntity);
                            }

                            if (connectedTrafficNodeCrossroadRef.CurrentRelatedCrossroad != Entity.Null)
                            {
                                var connectedCrossroadEntity = BakerExtension.GetEntity(EntityManager, connectedTrafficNodeCrossroadRef.CurrentRelatedCrossroad);
                                var connectedCrossroad = EntityManager.GetComponentData<CrossroadBakingData>(connectedCrossroadEntity);

                                if (connectedCrossroad.PositionHash != sourceHash)
                                {
                                    var nodePos = EntityManager.GetComponentData<LocalTransform>(laneEntity).Position.Flat();
                                    var nodeHash = HashMapHelper.GetHashMapPosition(nodePos, config.NodeCellSize);

                                    if (connectedTrafficNodeScope.HasMerge && !isRightLaneDirection)
                                    {
                                        nodeHash++;
                                    }

                                    var sourceNewData = new TrafficNodeDynamicConnection()
                                    {
                                        ConnectionType = ConnectionType.StreamingConnection,
                                        SegmentHash = crossroadBakingData.PositionHash,
                                        PositionHash = nodeHash
                                    };

                                    if (!tempConnectionCache.ContainsKey(laneEntity))
                                    {
                                        pathConnection.ConnectedHash = connectedNodeHash;

                                        pathConnectionBuffer[localBufferIndex] = pathConnection;

                                        roadStatConfig.TrafficNodeDynamicStreaming++;
                                        roadStatConfig.TrafficNodeStreamingTotal++;

                                        tempConnectionCache.Add(laneEntity, sourceNewData);
                                    }
                                    else
                                    {
                                        var sourceData = tempConnectionCache[laneEntity];

                                        if (!sourceData.ConnectionType.HasFlag(ConnectionType.StreamingConnection))
                                            roadStatConfig.TrafficNodeDynamicStreaming++;

                                        sourceNewData.ConnectionType |= sourceData.ConnectionType;

                                        tempConnectionCache[laneEntity] = sourceNewData;
                                    }

                                    var targetNewData = new TrafficNodeDynamicConnection()
                                    {
                                        ConnectionType = ConnectionType.ExternalStreamingConnection,
                                        SegmentHash = crossroadBakingData.PositionHash,
                                        PositionHash = currentConnectedHash
                                    };

                                    if (!tempConnectionCache.ContainsKey(connectedEntity))
                                    {
                                        tempConnectionCache.Add(connectedEntity, targetNewData);
                                        roadStatConfig.TrafficNodePassiveConnection++;
                                        roadStatConfig.TrafficNodeStreamingTotal++;
                                    }
                                    else
                                    {
                                        var targetData = tempConnectionCache[connectedEntity];

                                        if (!targetData.ConnectionType.HasFlag(ConnectionType.ExternalStreamingConnection))
                                            roadStatConfig.TrafficNodePassiveConnection++;

                                        targetNewData.ConnectionType |= targetData.ConnectionType;

                                        tempConnectionCache[connectedEntity] = targetNewData;
                                    }
                                }
                            }
                        }

                        connectedTrafficNodeCrossroadRef = default;

                        if (connectedScopeEntity != Entity.Null)
                        {
                            currentConnectedHash = connectedNodeHash;
                            connectedEntity = connectedLaneNodeEntity;

                            if (EntityManager.HasComponent<TrafficNodeCrossroadRef>(connectedScopeEntity))
                            {
                                connectedTrafficNodeCrossroadRef = EntityManager.GetComponentData<TrafficNodeCrossroadRef>(connectedScopeEntity);
                            }
                        }

                        if (connectedTrafficNodeCrossroadRef.CurrentRelatedCrossroad != Entity.Null)
                        {
                            var connectedCrossroadEntity = BakerExtension.GetEntity(EntityManager, connectedTrafficNodeCrossroadRef.CurrentRelatedCrossroad);
                            var connectedCrossroad = EntityManager.GetComponentData<CrossroadBakingData>(connectedCrossroadEntity);

                            if (connectedCrossroad.PositionHash != sourceHash)
                            {
                                var nodePos = EntityManager.GetComponentData<LocalTransform>(laneEntity).Position.Flat();
                                var nodeHash = HashMapHelper.GetHashMapPosition(nodePos, config.NodeCellSize);

                                var sourceNewData = new TrafficNodeDynamicConnection()
                                {
                                    ConnectionType = ConnectionType.StreamingConnection,
                                    SegmentHash = crossroadBakingData.PositionHash,
                                    PositionHash = nodeHash
                                };

                                if (!tempConnectionCache.ContainsKey(laneEntity))
                                {
                                    pathConnection.ConnectedHash = connectedNodeHash;

                                    pathConnectionBuffer[localBufferIndex] = pathConnection;

                                    roadStatConfig.TrafficNodeDynamicStreaming++;
                                    roadStatConfig.TrafficNodeStreamingTotal++;

                                    tempConnectionCache.Add(laneEntity, sourceNewData);
                                }
                                else
                                {
                                    var sourceData = tempConnectionCache[laneEntity];

                                    if (!sourceData.ConnectionType.HasFlag(ConnectionType.StreamingConnection))
                                        roadStatConfig.TrafficNodeDynamicStreaming++;

                                    sourceNewData.ConnectionType |= sourceData.ConnectionType;

                                    tempConnectionCache[laneEntity] = sourceNewData;
                                }

                                var targetNewData = new TrafficNodeDynamicConnection()
                                {
                                    ConnectionType = ConnectionType.ExternalStreamingConnection,
                                    SegmentHash = crossroadBakingData.PositionHash,
                                    PositionHash = currentConnectedHash
                                };

                                if (!tempConnectionCache.ContainsKey(connectedEntity))
                                {
                                    tempConnectionCache.Add(connectedEntity, targetNewData);
                                    roadStatConfig.TrafficNodePassiveConnection++;
                                    roadStatConfig.TrafficNodeStreamingTotal++;
                                }
                                else
                                {
                                    var targetData = tempConnectionCache[connectedEntity];

                                    if (!targetData.ConnectionType.HasFlag(ConnectionType.ExternalStreamingConnection))
                                        roadStatConfig.TrafficNodePassiveConnection++;

                                    targetNewData.ConnectionType |= targetData.ConnectionType;

                                    tempConnectionCache[connectedEntity] = targetNewData;
                                }
                            }
                        }
                    }
                }

                pathConnectionBuffer[localBufferIndex] = pathConnection;

                var minWaypointIndex = tempPathSettings.MinPathWaypointIndex;
                var maxWaypointIndex = tempPathSettings.MaxPathWaypointIndex;

                for (int j = minWaypointIndex; j < maxWaypointIndex; j++)
                {
                    var waypointInfo = trafficNodeScopeData.WayPoints[j];
                    tempRouteNodes.Add(waypointInfo);
                }

                PathBlobDataTemp pathBlobData = new()
                {
                    InstanceId = tempPathSettings.InstanceId,
                    ConnectedIndexes = new List<int>(),
                    Nodes = new List<RouteNodeData>(tempRouteNodes),
                    PathLength = tempPathSettings.PathLength,
                    SourceLaneIndex = laneIndex,
                    ConnectedPathInstanceId = tempPathSettings.ConnectedPathInstanceId,
                    ConnectedPathIndex = -1,
                    Priority = tempPathSettings.Priority,

                    Options = tempPathSettings.Options,
                    PathCurveType = tempPathSettings.PathCurveType,
                    PathRoadType = tempPathSettings.PathRoadType,
                    PathConnectionType = tempPathSettings.PathConnectionType,
                    TrafficGroup = tempPathSettings.TrafficGroup,
                };

                PathConnectingMap.Add(pathBlobData);
                tempRouteNodes.Clear();

                trafficNodeScopeData.PathTempSettingsDatas[i] = tempPathSettings;
            }

            return true;
        }

        private void CreateStatConfig(Entity statEntity, RoadStatConfig roadStatConfig)
        {
            EntityManager.SetComponentData(statEntity, roadStatConfig);
        }
    }
}
