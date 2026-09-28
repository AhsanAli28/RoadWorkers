using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.DotsCity.Simulation.Road;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Level.Streaming
{
    [UpdateInGroup(typeof(InitGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficRoadUnloadSystem : ISystem, ISystemStartStop
    {
        private NativeHashMap<int, RuntimePathData> pathDataHashMapLocalRef;
        private NativeParallelMultiHashMap<int, AwaitingData> awatingHashesLocalRef;
        private NativeHashMap<int, Entity> availableHashesLocalRef;
        private NativeParallelMultiHashMap<int, ConnectionData> connectedHashesLocalRef;
        private NativeParallelMultiHashMap<int, ConnectionData> revertConnectedHashesLocalRef;
        private NativeParallelMultiHashMap<int, int> segmentMappingLocalRef;
        private NativeParallelMultiHashMap<int, Entity> sectionMappingLocalRef;
        private NativeParallelMultiHashMap<int, PedestrianNodeAwaitingData> awaitingPedNodesLocalRef;
        private NativeHashMap<int, AvailablePedNodeData> availablePedNodesLocalRef;

        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<SegmentUnloadTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
            state.RequireForUpdate<TrafficNodeResolverSystem.InitTag>();
        }

        void ISystem.OnDestroy(ref SystemState state)
        {
            pathDataHashMapLocalRef = default;
            awatingHashesLocalRef = default;
            availableHashesLocalRef = default;
            connectedHashesLocalRef = default;
            revertConnectedHashesLocalRef = default;
            segmentMappingLocalRef = default;
            sectionMappingLocalRef = default;
            awaitingPedNodesLocalRef = default;
            availablePedNodesLocalRef = default;
        }

        void ISystemStartStop.OnStartRunning(ref SystemState state)
        {
            pathDataHashMapLocalRef = TrafficNodeResolverSystem.PathDataHashMapStaticRef;
            awatingHashesLocalRef = TrafficNodeResolverSystem.AwatingHashesStaticRef;
            availableHashesLocalRef = TrafficNodeResolverSystem.AvailableHashesStaticRef;
            connectedHashesLocalRef = TrafficNodeResolverSystem.ConnectedHashesStaticRef;
            revertConnectedHashesLocalRef = TrafficNodeResolverSystem.RevertConnectedHashesStaticRef;
            segmentMappingLocalRef = TrafficNodeResolverSystem.SegmentMappingStaticRef;
            sectionMappingLocalRef = TrafficNodeResolverSystem.SectionMappingStaticRef;
            awaitingPedNodesLocalRef = TrafficNodeResolverSystem.AwaitingPedNodesStatifRef;
            availablePedNodesLocalRef = TrafficNodeResolverSystem.AvailablePedNodesStaticRef;
        }

        void ISystemStartStop.OnStopRunning(ref SystemState state) { }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var unloadJob = new UnloadJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                PathDataHashMap = pathDataHashMapLocalRef,
                AwatingHashes = awatingHashesLocalRef,
                AvailableHashes = availableHashesLocalRef,
                ConnectedHashes = connectedHashesLocalRef,
                RevertConnectedHashes = revertConnectedHashesLocalRef,
                SegmentMapping = segmentMappingLocalRef,
                SectionMapping = sectionMappingLocalRef,
                AwaitingPedNodes = awaitingPedNodesLocalRef,
                AvailablePedNodes = availablePedNodesLocalRef,
                PedestrianSectionLookup = SystemAPI.GetComponentLookup<PedestrianSectionData>(true),
                DynamicConnectionLookup = SystemAPI.GetComponentLookup<TrafficNodeDynamicConnection>(true),
                NodeSectionConnectionDataLookup = SystemAPI.GetBufferLookup<NodeSectionConnectionDataElement>(true),
                NodeConnectionDataLookup = SystemAPI.GetBufferLookup<NodeConnectionDataElement>(false),
                PathConnectionLookup = SystemAPI.GetBufferLookup<PathConnectionElement>(false),
            };

            unloadJob.Schedule();
        }

        [WithAll(typeof(SegmentUnloadTag))]
        [BurstCompile]
        public partial struct UnloadJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [NativeDisableContainerSafetyRestriction]
            public NativeParallelMultiHashMap<int, AwaitingData> AwatingHashes;

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
                in SegmentComponent crossroadComponent,
                in DynamicBuffer<SegmentTrafficNodeData> nodes,
                in DynamicBuffer<SegmentPedestrianNodeData> pedNodes)
            {
                for (int i = 0; i < pedNodes.Length; i++)
                {
                    var nodeEntity = pedNodes[i].Entity;

                    if (!NodeSectionConnectionDataLookup.HasBuffer(nodeEntity))
                    {
                        continue;
                    }

                    var nodeHash = PedestrianSectionLookup[nodeEntity].NodeHash;

                    if (AvailablePedNodes.ContainsKey(nodeHash))
                    {
                        AvailablePedNodes.Remove(nodeHash);
                    }

                    var sectionConnectionBuffer = NodeSectionConnectionDataLookup[nodeEntity];
                    var connectionBuffer = NodeConnectionDataLookup[nodeEntity];

                    for (int j = 0; j < sectionConnectionBuffer.Length; j++)
                    {
                        var data = sectionConnectionBuffer[j];

                        if (data.ConnectedHash == -1)
                        {
                            continue;
                        }

                        var connectedNode = connectionBuffer[j].ConnectedEntity;

                        if (!NodeConnectionDataLookup.HasBuffer(connectedNode))
                        {
                            continue;
                        }

                        var connectedBuffer = NodeConnectionDataLookup[connectedNode];

                        for (int n = 0; n < connectedBuffer.Length; n++)
                        {
                            var connectedDataBuffer = connectedBuffer[n];

                            if (connectedDataBuffer.ConnectedEntity == nodeEntity)
                            {
                                connectedDataBuffer.ConnectedEntity = Entity.Null;
                                connectedBuffer[n] = connectedDataBuffer;

                                AwaitingPedNodes.Add(nodeHash, new PedestrianNodeAwaitingData()
                                {
                                    AwaitingEntity = connectedNode,
                                    LocalConnectionIndex = n
                                });

                                break;
                            }
                        }
                    }
                }

                for (int i = 0; i < nodes.Length; i++)
                {
                    var nodeEntity = nodes[i].Entity;

                    if (!PathConnectionLookup.HasBuffer(nodeEntity))
                    {
                        continue;
                    }

                    if (!DynamicConnectionLookup.HasComponent(nodeEntity))
                    {
                        var pathBuffer = PathConnectionLookup[nodeEntity];

                        for (int j = 0; j < pathBuffer.Length; j++)
                        {
                            var pathData = pathBuffer[j];

                            PathDataHashMap[pathData.GlobalPathIndex] = new RuntimePathData()
                            {
                                ConnectedNode = Entity.Null,
                                SourceNode = Entity.Null
                            };
                        }
                    }
                    else
                    {
                        TrafficNodeDynamicConnection currentConnection = DynamicConnectionLookup[nodeEntity];
                        ResolveDynamic(in crossroadComponent, in currentConnection);
                    }
                }

                SectionMapping.Remove(crossroadComponent.SectionIndex);

                CommandBuffer.SetComponentEnabled<SegmentUnloadTag>(entity, false);
            }

            private void ResolveDynamic(in SegmentComponent crossroadComponent, in TrafficNodeDynamicConnection currentConnection)
            {
                var segmentHash = crossroadComponent.SegmentHash;

                if (SegmentMapping.TryGetFirstValue(segmentHash, out var unloadNodeHash, out var nativeMultiHashMapIterator))
                {
                    do
                    {
                        if (ConnectedHashes.TryGetFirstValue(unloadNodeHash, out var connectionData, out var nativeMultiHashMapIteratorConnect1))
                        {
                            do
                            {
                                var connectedNodeHash = connectionData.Hash;

                                if (RevertConnectedHashes.TryGetFirstValue(connectedNodeHash, out var revertConnectionData1, out var nativeMultiHashMapIteratorRevert1))
                                {
                                    do
                                    {
                                        if (revertConnectionData1.DefaultNode && revertConnectionData1.DefaultConnection)
                                        {
                                            var sourceConnectionEntity = revertConnectionData1.ConnectedEntity;
                                            var pathBuffer = PathConnectionLookup[sourceConnectionEntity];

                                            for (int i = 0; i < pathBuffer.Length; i++)
                                            {
                                                PathDataHashMap[pathBuffer[i].GlobalPathIndex] = new RuntimePathData()
                                                {
                                                    ConnectedNode = Entity.Null,
                                                    SourceNode = Entity.Null
                                                };
                                            }
                                        }

                                    } while (RevertConnectedHashes.TryGetNextValue(out revertConnectionData1, ref nativeMultiHashMapIteratorRevert1));

                                    RevertConnectedHashes.Remove(connectedNodeHash);
                                }

                            } while (ConnectedHashes.TryGetNextValue(out connectionData, ref nativeMultiHashMapIteratorConnect1));

                            ConnectedHashes.Remove(unloadNodeHash);
                        }

                        if (AwatingHashes.ContainsKey(unloadNodeHash))
                        {
                            AwatingHashes.Remove(unloadNodeHash);
                        }

                        if (RevertConnectedHashes.TryGetFirstValue(unloadNodeHash, out var revertConnectionData2, out var nativeMultiHashMapIterator3))
                        {
                            do
                            {
                                var connectedHash = revertConnectionData2.Hash;

                                if (ConnectedHashes.TryGetFirstValue(connectedHash, out var connectionData2, out var nativeMultiHashMapIteratorConnect2))
                                {
                                    var sourceEntity = revertConnectionData2.ConnectedEntity;

                                    do
                                    {
                                        var connectedEntity = connectionData2.ConnectedEntity;

                                        if (DynamicConnectionLookup.HasComponent(connectedEntity))
                                        {
                                            var dynamicConnection = DynamicConnectionLookup[connectedEntity];

                                            var laneIndex = dynamicConnection.LaneIndex;

                                            var pathConnections = PathConnectionLookup[sourceEntity];
                                            var pathConnection = pathConnections[laneIndex];

                                            if (revertConnectionData2.DefaultConnection)
                                            {
                                                pathConnection.ConnectedNodeEntity = Entity.Null;
                                            }

                                            if (revertConnectionData2.SubConnection)
                                            {
                                                pathConnection.ConnectedSubNodeEntity = Entity.Null;
                                            }

                                            pathConnections[laneIndex] = pathConnection;

                                            AwatingHashes.Add(unloadNodeHash, new AwaitingData()
                                            {
                                                SourceEntity = sourceEntity,
                                                ConnectionSettings = connectionData2.ConnectionSettings
                                            });
                                        }

                                        if (revertConnectionData2.DefaultNode && revertConnectionData2.DefaultConnection)
                                        {
                                            var pathBuffer = PathConnectionLookup[sourceEntity];

                                            for (int i = 0; i < pathBuffer.Length; i++)
                                            {
                                                PathDataHashMap[pathBuffer[i].GlobalPathIndex] = new RuntimePathData()
                                                {
                                                    SourceNode = sourceEntity,
                                                    ConnectedNode = Entity.Null,
                                                };
                                            }
                                        }

                                    } while (ConnectedHashes.TryGetNextValue(out connectionData2, ref nativeMultiHashMapIteratorConnect2));

                                    ConnectedHashes.Remove(connectedHash);
                                }

                            } while (RevertConnectedHashes.TryGetNextValue(out revertConnectionData2, ref nativeMultiHashMapIterator3));

                            RevertConnectedHashes.Remove(unloadNodeHash);
                        }

                        if (AvailableHashes.ContainsKey(unloadNodeHash))
                        {
                            AvailableHashes.Remove(unloadNodeHash);
                        }

                    } while (SegmentMapping.TryGetNextValue(out unloadNodeHash, ref nativeMultiHashMapIterator));

                    SegmentMapping.Remove(segmentHash);
                }

            }
        }
    }
}