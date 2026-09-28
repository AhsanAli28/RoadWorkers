using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.Extensions;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Level.Streaming
{
    [UpdateInGroup(typeof(StructuralInitGroup))]
    [BurstCompile]
    public partial struct TrafficNodeResolverSystem : ISystem, ISystemStartStop
    {
        public struct InitTag : IComponentData { }

        public struct RuntimePathDataRef : IComponentData
        {
            internal NativeHashMap<int, RuntimePathData> PathDataHashMap;

            public bool TryGetValue(int globalPathIndex, out Entity sourceTrafficNode, out Entity connectedTrafficNode)
            {
                sourceTrafficNode = default;
                connectedTrafficNode = default;

                if (PathDataHashMap.TryGetValue(globalPathIndex, out var sourcePathData))
                {
                    sourceTrafficNode = sourcePathData.SourceNode;
                    connectedTrafficNode = sourcePathData.ConnectedNode;
                    return true;
                }

                return false;
            }

            public int Count() => PathDataHashMap.Count;

            public Entity TryToGetSourceNode(int globalPathIndex)
            {
                if (PathDataHashMap.TryGetValue(globalPathIndex, out var sourcePathData))
                {
                    return sourcePathData.SourceNode;
                }

                return Entity.Null;
            }

            public Entity TryToGetConnectedNode(int globalPathIndex)
            {
                if (PathDataHashMap.TryGetValue(globalPathIndex, out var sourcePathData))
                {
                    return sourcePathData.ConnectedNode;
                }

                return Entity.Null;
            }

            public bool HasPath(int globalPathIndex) => PathDataHashMap.ContainsKey(globalPathIndex);

#if RUNTIME_ROAD
            public void AddPath(int globalPathIndex, Entity sourceTrafficNode, Entity connectedTrafficNode)
            {
                var pathData = new RuntimePathData()
                {
                    SourceNode = sourceTrafficNode,
                    ConnectedNode = connectedTrafficNode
                };

                if (!PathDataHashMap.ContainsKey(globalPathIndex))
                {
                    PathDataHashMap.Add(globalPathIndex, pathData);
                }
                else
                {
                    PathDataHashMap[globalPathIndex] = pathData;
                }
            }

            public void RemovePath(int globalPathIndex)
            {
                if (PathDataHashMap.ContainsKey(globalPathIndex))
                    PathDataHashMap.Remove(globalPathIndex);
            }

            public void Clear()
            {
                if (!PathDataHashMap.IsCreated)
                    return;

                PathDataHashMap.Clear();
            }
#endif
        }

        private NativeHashMap<int, RuntimePathData> pathDataHashMap;
        private EntityQuery updateQuery;

        // Awaiting position node hash / awaiting entity
        private NativeParallelMultiHashMap<int, AwaitingData> awaitingHashes;

        // Position node hash / node entity
        private NativeHashMap<int, Entity> availableHashes;

        // Connected hash / source entity
        private NativeParallelMultiHashMap<int, ConnectionData> connectedHashes;

        // Reverse hash / connected entity
        private NativeParallelMultiHashMap<int, ConnectionData> revertConnected;

        // Segment hash / node hash
        private NativeParallelMultiHashMap<int, int> segmentMapping;

        // Section index / crossroad entity
        private NativeParallelMultiHashMap<int, Entity> sectionMapping;

        private NativeParallelMultiHashMap<int, PedestrianNodeAwaitingData> awaitingPedNodes;

        private NativeHashMap<int, AvailablePedNodeData> availablePedNodes;

        public static NativeHashMap<int, RuntimePathData> PathDataHashMapStaticRef { get; private set; }

        internal static NativeParallelMultiHashMap<int, AwaitingData> AwatingHashesStaticRef { get; private set; }

        internal static NativeHashMap<int, Entity> AvailableHashesStaticRef { get; private set; }

        internal static NativeParallelMultiHashMap<int, ConnectionData> ConnectedHashesStaticRef { get; private set; }

        internal static NativeParallelMultiHashMap<int, ConnectionData> RevertConnectedHashesStaticRef { get; private set; }

        internal static NativeParallelMultiHashMap<int, int> SegmentMappingStaticRef { get; private set; }

        internal static NativeParallelMultiHashMap<int, Entity> SectionMappingStaticRef { get; private set; }

        internal static NativeParallelMultiHashMap<int, PedestrianNodeAwaitingData> AwaitingPedNodesStatifRef { get; private set; }

        internal static NativeHashMap<int, AvailablePedNodeData> AvailablePedNodesStaticRef { get; private set; }

        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<SegmentInitTag>()
                .Build();

            state.RequireForUpdate(updateQuery);

#if RUNTIME_ROAD
            pathDataHashMap = new NativeHashMap<int, RuntimePathData>(1000, Allocator.Persistent);

            var entity = state.EntityManager.CreateEntity();

            state.EntityManager.AddComponentData(entity, new RuntimePathDataRef()
            {
                PathDataHashMap = pathDataHashMap
            });

            PathDataHashMapStaticRef = pathDataHashMap;
#endif

            state.Enabled = false;
        }

        [BurstDiscard]
        void ISystem.OnDestroy(ref SystemState state)
        {
            if (awaitingHashes.IsCreated)
            {
                awaitingHashes.Dispose();
                availableHashes.Dispose();
                connectedHashes.Dispose();
                revertConnected.Dispose();
                segmentMapping.Dispose();
                sectionMapping.Dispose();
                awaitingPedNodes.Dispose();
                availablePedNodes.Dispose();

                AwatingHashesStaticRef = default;
                AvailableHashesStaticRef = default;
                ConnectedHashesStaticRef = default;
                RevertConnectedHashesStaticRef = default;
                SegmentMappingStaticRef = default;
                SectionMappingStaticRef = default;
                AwaitingPedNodesStatifRef = default;
                AvailablePedNodesStaticRef = default;
            }

            if (pathDataHashMap.IsCreated)
            {
                pathDataHashMap.Dispose();
                PathDataHashMapStaticRef = default;
            }
        }

        void ISystemStartStop.OnStartRunning(ref SystemState state)
        {
            if (!awaitingHashes.IsCreated)
            {
                var roadStat = SystemAPI.GetSingleton<RoadStatConfig>();

                awaitingHashes = new NativeParallelMultiHashMap<int, AwaitingData>(roadStat.TrafficNodeDynamicStreaming, Allocator.Persistent);
                availableHashes = new NativeHashMap<int, Entity>(roadStat.TrafficNodePassiveConnection, Allocator.Persistent);
                connectedHashes = new NativeParallelMultiHashMap<int, ConnectionData>(roadStat.TrafficNodeDynamicStreaming, Allocator.Persistent);
                revertConnected = new NativeParallelMultiHashMap<int, ConnectionData>(roadStat.TrafficNodeDynamicStreaming, Allocator.Persistent);
                segmentMapping = new NativeParallelMultiHashMap<int, int>(roadStat.TrafficNodeDynamicStreaming, Allocator.Persistent);
                sectionMapping = new NativeParallelMultiHashMap<int, Entity>(roadStat.TrafficNodeDynamicStreaming, Allocator.Persistent);
                awaitingPedNodes = new NativeParallelMultiHashMap<int, PedestrianNodeAwaitingData>(1000, Allocator.Persistent);
                availablePedNodes = new NativeHashMap<int, AvailablePedNodeData>(1000, Allocator.Persistent);

                AwatingHashesStaticRef = awaitingHashes;
                AvailableHashesStaticRef = availableHashes;
                ConnectedHashesStaticRef = connectedHashes;
                RevertConnectedHashesStaticRef = revertConnected;
                SegmentMappingStaticRef = segmentMapping;
                SectionMappingStaticRef = sectionMapping;
                AwaitingPedNodesStatifRef = awaitingPedNodes;
                AvailablePedNodesStaticRef = availablePedNodes;

                state.EntityManager.CreateEntity(typeof(InitTag));
            }

            Initialize(ref state);
        }

        void ISystemStartStop.OnStopRunning(ref SystemState state) { }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var nodeResolveJob = new NodeResolveJob()
            {
                AwaitingHashes = awaitingHashes,
                AvailableHashes = availableHashes,
                ConnectedHashes = connectedHashes,
                RevertConnectedHashes = revertConnected,
                SegmentMapping = segmentMapping,
                SectionMapping = sectionMapping,
                PathDataHashMap = pathDataHashMap,
                AwaitingPedNodes = awaitingPedNodes,
                AvailablePedNodes = availablePedNodes,
                PedestrianSectionLookup = SystemAPI.GetComponentLookup<PedestrianSectionData>(true),
                DynamicConnectionLookup = SystemAPI.GetComponentLookup<TrafficNodeDynamicConnection>(true),
                NodeSectionConnectionDataLookup = SystemAPI.GetBufferLookup<NodeSectionConnectionDataElement>(true),
                NodeConnectionDataLookup = SystemAPI.GetBufferLookup<NodeConnectionDataElement>(false),
                PathConnectionLookup = SystemAPI.GetBufferLookup<PathConnectionElement>(false),
            };

            nodeResolveJob.Run();
        }

        [WithAll(typeof(SegmentInitTag))]
        [BurstCompile]
        partial struct NodeResolveJob : IJobEntity
        {
            [NativeDisableContainerSafetyRestriction]
            public NativeParallelMultiHashMap<int, AwaitingData> AwaitingHashes;

            [NativeDisableContainerSafetyRestriction]
            public NativeHashMap<int, Entity> AvailableHashes;

            [NativeDisableContainerSafetyRestriction]
            public NativeParallelMultiHashMap<int, ConnectionData> ConnectedHashes;

            [NativeDisableContainerSafetyRestriction]
            public NativeParallelMultiHashMap<int, ConnectionData> RevertConnectedHashes;

            [NativeDisableContainerSafetyRestriction]
            public NativeParallelMultiHashMap<int, int> SegmentMapping;

            [NativeDisableContainerSafetyRestriction]
            public NativeParallelMultiHashMap<int, Entity> SectionMapping;

            [NativeDisableContainerSafetyRestriction]
            public NativeHashMap<int, RuntimePathData> PathDataHashMap;

            [NativeDisableContainerSafetyRestriction]
            public NativeParallelMultiHashMap<int, PedestrianNodeAwaitingData> AwaitingPedNodes;

            [NativeDisableContainerSafetyRestriction]
            public NativeHashMap<int, AvailablePedNodeData> AvailablePedNodes;

            [ReadOnly]
            public ComponentLookup<PedestrianSectionData> PedestrianSectionLookup;

            [ReadOnly]
            public ComponentLookup<TrafficNodeDynamicConnection> DynamicConnectionLookup;

            [ReadOnly]
            public BufferLookup<NodeSectionConnectionDataElement> NodeSectionConnectionDataLookup;

            public BufferLookup<NodeConnectionDataElement> NodeConnectionDataLookup;

            public BufferLookup<PathConnectionElement> PathConnectionLookup;

            void Execute(
                Entity entity,
                EnabledRefRW<SegmentInitTag> segmentInitTagRW,
                in DynamicBuffer<SegmentTrafficNodeData> trafficNodes,
                in DynamicBuffer<SegmentPedestrianNodeData> pedestrianNodes,
                in SegmentComponent crossroadComponent)
            {
                SectionMapping.Add(crossroadComponent.SectionIndex, entity);

                for (int i = 0; i < pedestrianNodes.Length; i++)
                {
                    var nodeEntity = pedestrianNodes[i].Entity;

                    if (!PedestrianSectionLookup.HasComponent(nodeEntity))
                    {
                        continue;
                    }

                    var section = PedestrianSectionLookup[nodeEntity];
                    var nodeHash = section.NodeHash;

                    if (AvailablePedNodes.ContainsKey(nodeHash))
                    {
#if UNITY_EDITOR
                        UnityEngine.Debug.Log($"TrafficNodeResolverSystem. Duplicate NodeHash {nodeHash} Entities {AvailablePedNodes[nodeHash].Entity.Index} {nodeEntity.Index}");
#endif
                    }

                    AvailablePedNodes.Add(nodeHash, new AvailablePedNodeData()
                    {
                        Entity = nodeEntity,
                    });

                    var sectionConnectionBuffer = NodeSectionConnectionDataLookup[nodeEntity];
                    var connectionBuffer = NodeConnectionDataLookup[nodeEntity];

                    for (int j = 0; j < sectionConnectionBuffer.Length; j++)
                    {
                        var data = sectionConnectionBuffer[j];

                        var connectedHash = data.ConnectedHash;

                        if (connectedHash != -1)
                        {
                            if (AvailablePedNodes.ContainsKey(connectedHash))
                            {
                                var connectionData = connectionBuffer[j];
                                connectionData.ConnectedEntity = AvailablePedNodes[connectedHash].Entity;
                                connectionBuffer[j] = connectionData;
                            }
                            else
                            {
                                var awaitingData = new PedestrianNodeAwaitingData()
                                {
                                    AwaitingEntity = nodeEntity,
                                    LocalConnectionIndex = j
                                };

                                AwaitingPedNodes.Add(connectedHash, awaitingData);
                            }
                        }
                    }

                    if (AwaitingPedNodes.TryGetFirstValue(nodeHash, out var item, out var iterator))
                    {
                        do
                        {
                            if (NodeConnectionDataLookup.HasBuffer(item.AwaitingEntity))
                            {
                                var currentConnectionBuffer = NodeConnectionDataLookup[item.AwaitingEntity];
                                var index = item.LocalConnectionIndex;
                                var connectionData = currentConnectionBuffer[index];
                                connectionData.ConnectedEntity = nodeEntity;
                                currentConnectionBuffer[index] = connectionData;
                            }

                        } while (AwaitingPedNodes.TryGetNextValue(out item, ref iterator));

                        AwaitingPedNodes.Remove(nodeHash);
                    }
                }

                for (int i = 0; i < trafficNodes.Length; i++)
                {
                    var nodeEntity = trafficNodes[i].Entity;

                    if (!PathConnectionLookup.HasBuffer(nodeEntity))
                    {
                        continue;
                    }

                    bool externalConnection = false;
                    bool hasConnection = DynamicConnectionLookup.HasComponent(nodeEntity);
                    TrafficNodeDynamicConnection currentConnection = default;

                    if (hasConnection)
                    {
                        currentConnection = DynamicConnectionLookup[nodeEntity];
                        externalConnection = !currentConnection.SubNode && DotsEnumExtension.HasFlagUnsafe(currentConnection.ConnectionType, ConnectionType.ExternalStreamingConnection);
                    }

                    if ((!hasConnection || externalConnection) && !currentConnection.SubNode)
                    {
                        var pathConnections = PathConnectionLookup[nodeEntity];

                        for (int j = 0; j < pathConnections.Length; j++)
                        {
                            var pathData = pathConnections[j];

                            PathDataHashMap[pathData.GlobalPathIndex] = new RuntimePathData()
                            {
                                ConnectedNode = pathData.ConnectedNodeEntity,
                                SourceNode = nodeEntity
                            };
                        }
                    }

                    if (hasConnection)
                    {
                        ResolveDynamic(nodeEntity, in crossroadComponent, in currentConnection);
                    }
                }

                segmentInitTagRW.ValueRW = false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void ResolveDynamic(Entity entity, in SegmentComponent crossroadComponent, in TrafficNodeDynamicConnection currentConnection)
            {
                SegmentMapping.Add(crossroadComponent.SegmentHash, currentConnection.PositionHash);

                if (DotsEnumExtension.HasFlagUnsafe(currentConnection.ConnectionType, ConnectionType.StreamingConnection))
                {
                    var currentBuffer = PathConnectionLookup[entity];

                    for (int i = 0; i < currentBuffer.Length; i++)
                    {
                        var buff = currentBuffer[i];

                        var pathData = PathDataHashMap[buff.GlobalPathIndex];

                        if (!currentConnection.SubNode)
                        {
                            pathData.SourceNode = entity;
                        }

                        ResolveConnection(entity, buff.ConnectedHash, false, in currentConnection, ref buff, ref pathData);

                        if (buff.HasSubNode && !buff.SameHash)
                        {
                            ResolveConnection(entity, buff.ConnectedSubHash, true, in currentConnection, ref buff, ref pathData);
                        }

                        currentBuffer[i] = buff;

                        PathDataHashMap[buff.GlobalPathIndex] = pathData;
                    }
                }

                if (DotsEnumExtension.HasFlagUnsafe(currentConnection.ConnectionType, ConnectionType.ExternalStreamingConnection))
                {
                    var hash = currentConnection.PositionHash;

                    if (!AvailableHashes.ContainsKey(hash))
                    {
                        AvailableHashes.Add(hash, entity);
                    }
                    else
                    {
                        AvailableHashes[hash] = entity;
                    }

                    if (AwaitingHashes.TryGetFirstValue(hash, out var awaitingData, out var nativeMultiHashMapIterator))
                    {
                        do
                        {
                            if (!PathConnectionLookup.HasBuffer(awaitingData.SourceEntity))
                            {
                                continue;
                            }

                            var sourceBuffer = PathConnectionLookup[awaitingData.SourceEntity];
                            var laneData = sourceBuffer[currentConnection.LaneIndex];

                            if (awaitingData.DefaultNode)
                            {
                                var pathData = PathDataHashMap[laneData.GlobalPathIndex];

                                pathData.SourceNode = awaitingData.SourceEntity;

                                if (awaitingData.DefaultConnection)
                                {
                                    pathData.ConnectedNode = entity;
                                }

                                PathDataHashMap[laneData.GlobalPathIndex] = pathData;
                            }

                            if (awaitingData.DefaultConnection)
                            {
                                laneData.ConnectedNodeEntity = entity;
                            }

                            if (awaitingData.SubConnection)
                            {
                                laneData.ConnectedSubNodeEntity = entity;
                            }

                            sourceBuffer[currentConnection.LaneIndex] = laneData;

                            var sourceConnection = DynamicConnectionLookup[awaitingData.SourceEntity];

                            ConnectedHashes.Add(sourceConnection.PositionHash, new ConnectionData()
                            {
                                ConnectedEntity = entity,
                                Hash = laneData.ConnectedHash,
                                ConnectionSettings = awaitingData.ConnectionSettings,
                            });

                            RevertConnectedHashes.Add(currentConnection.PositionHash, new ConnectionData()
                            {
                                ConnectedEntity = awaitingData.SourceEntity,
                                Hash = sourceConnection.PositionHash,
                                ConnectionSettings = awaitingData.ConnectionSettings,
                            });

                        } while (AwaitingHashes.TryGetNextValue(out awaitingData, ref nativeMultiHashMapIterator));

                        AwaitingHashes.Remove(hash);
                    }
                }
            }

            private void ResolveConnection(
                Entity entity,
                int connectedHash,
                bool subNodeConnection,
                in TrafficNodeDynamicConnection currentConnection,
                ref PathConnectionElement pathConnectionElement,
                ref RuntimePathData pathData)
            {
                var flags = ConnectionSettings.Default;

                if (currentConnection.SubNode)
                {
                    if (!subNodeConnection)
                    {
                        flags = ConnectionSettings.SubNode | ConnectionSettings.DefaultConnection;
                    }
                    else
                    {
                        flags = ConnectionSettings.SubNode | ConnectionSettings.SubConnection;
                    }
                }
                else
                {
                    if (!subNodeConnection)
                    {
                        flags = flags | ConnectionSettings.DefaultConnection;
                    }
                    else
                    {
                        flags = flags | ConnectionSettings.SubConnection;
                    }
                }

                if (pathConnectionElement.SameHash)
                {
                    if (subNodeConnection)
                    {
                        flags = flags | ConnectionSettings.DefaultConnection;
                    }
                    else
                    {
                        flags = flags | ConnectionSettings.SubConnection;
                    }
                }

                if (AvailableHashes.ContainsKey(connectedHash))
                {
                    var connectedNodeEntity = Entity.Null;

                    if (DotsEnumExtension.HasFlagUnsafe(flags, ConnectionSettings.DefaultConnection))
                    {
                        pathConnectionElement.ConnectedNodeEntity = AvailableHashes[connectedHash];
                        connectedNodeEntity = pathConnectionElement.ConnectedNodeEntity;
                    }

                    if (DotsEnumExtension.HasFlagUnsafe(flags, ConnectionSettings.SubConnection))
                    {
                        pathConnectionElement.ConnectedSubNodeEntity = AvailableHashes[connectedHash];
                        connectedNodeEntity = pathConnectionElement.ConnectedSubNodeEntity;
                    }

                    if (!currentConnection.SubNode && !subNodeConnection)
                    {
                        pathData.ConnectedNode = pathConnectionElement.ConnectedNodeEntity;
                    }

                    var newConnectedData = new ConnectionData()
                    {
                        ConnectedEntity = connectedNodeEntity,
                        Hash = connectedHash,
                        ConnectionSettings = flags
                    };

                    var newRevertedConnectedData = new ConnectionData()
                    {
                        ConnectedEntity = entity,
                        Hash = currentConnection.PositionHash,
                        ConnectionSettings = flags
                    };

                    ConnectedHashes.Add(currentConnection.PositionHash, newConnectedData);

                    var found = false;

                    if (RevertConnectedHashes.TryGetFirstValue(connectedHash, out var revertConnectionData, out var nativeMultiHashMapIterator2))
                    {
                        do
                        {
                            if (revertConnectionData.Hash == newRevertedConnectedData.Hash)
                            {
                                revertConnectionData.ConnectedEntity = newRevertedConnectedData.ConnectedEntity;
                                RevertConnectedHashes.SetValue(revertConnectionData, nativeMultiHashMapIterator2);
                                found = true;
                                break;
                            }

                        } while (RevertConnectedHashes.TryGetNextValue(out revertConnectionData, ref nativeMultiHashMapIterator2));
                    }

                    if (!found)
                    {
                        RevertConnectedHashes.Add(connectedHash, newRevertedConnectedData);
                    }
                }
                else
                {
                    var awaitingData = new AwaitingData()
                    {
                        SourceEntity = entity,
                        ConnectionSettings = flags
                    };

                    AwaitingHashes.Add(connectedHash, awaitingData);
                }
            }
        }

        public void Initialize(ref SystemState state)
        {
#if !RUNTIME_ROAD

            if (pathDataHashMap.IsCreated)
                return;

            var pathGraphQ = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PathGraphSystem.Singleton>());
            var graph = pathGraphQ.GetSingleton<PathGraphSystem.Singleton>();

            var pathCount = graph.Count();

            pathDataHashMap = new NativeHashMap<int, RuntimePathData>(pathCount, Allocator.Persistent);

            for (int i = 0; i < pathCount; i++)
            {
                pathDataHashMap.Add(i, new RuntimePathData());
            }

            var entity = state.EntityManager.CreateEntity();

            state.EntityManager.AddComponentData(entity, new RuntimePathDataRef()
            {
                PathDataHashMap = pathDataHashMap
            });

            PathDataHashMapStaticRef = pathDataHashMap;
#else

            if (pathDataHashMap.Count != 0) return;

            var pathGraphQ = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PathGraphSystem.Singleton>());
            var graph = pathGraphQ.GetSingleton<PathGraphSystem.Singleton>();

            var pathCount = graph.Count();

            for (int i = 0; i < pathCount; i++)
            {
                pathDataHashMap.Add(i, new RuntimePathData());
            }
#endif

            //state.EntityManager.DestroyEntity(pathGraphQ.GetSingletonEntity());
        }
    }
}