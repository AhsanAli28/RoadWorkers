using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Scenes;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Level.Streaming
{
    [UpdateInGroup(typeof(LateInitGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficRoadStreamingSystem : ISystem, ISystemStartStop
    {
        private EntityQuery roadGroup;
        private EntityQuery cullPointGroup;
        private NativeParallelMultiHashMap<int, Entity> sectionMappingLocalRef;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            roadGroup = SystemAPI.QueryBuilder()
                .WithAll<RoadSectionData>()
                .Build();

            cullPointGroup = SystemAPI.QueryBuilder()
                .WithAll<CullPointTag, LocalToWorld>()
                .Build();

            state.RequireForUpdate(roadGroup);
            state.RequireForUpdate(cullPointGroup);
            state.RequireForUpdate<RoadStreamingConfigReference>();
            state.RequireForUpdate<TrafficNodeResolverSystem.InitTag>();
        }

        void ISystem.OnDestroy(ref SystemState state)
        {
            sectionMappingLocalRef = default;
        }

        void ISystemStartStop.OnStartRunning(ref SystemState state)
        {
            sectionMappingLocalRef = TrafficNodeResolverSystem.SectionMappingStaticRef;
        }

        void ISystemStartStop.OnStopRunning(ref SystemState state) { }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var streamingJob = new StreamingJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                SectionMapping = sectionMappingLocalRef,
                IsSectionLoadedLookup = SystemAPI.GetComponentLookup<IsSectionLoaded>(true),
                CullPoint = cullPointGroup.GetSingleton<LocalToWorld>(),
                RoadStreamingConfig = SystemAPI.GetSingleton<RoadStreamingConfigReference>(),
                DynamicConnectionLookup = SystemAPI.GetComponentLookup<TrafficNodeDynamicConnection>(true),
            };

            streamingJob.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct StreamingJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter CommandBuffer;

            [ReadOnly]
            public NativeParallelMultiHashMap<int, Entity> SectionMapping;

            [ReadOnly]
            public ComponentLookup<TrafficNodeDynamicConnection> DynamicConnectionLookup;

            [ReadOnly]
            public ComponentLookup<IsSectionLoaded> IsSectionLoadedLookup;

            [ReadOnly]
            public LocalToWorld CullPoint;

            [ReadOnly]
            public RoadStreamingConfigReference RoadStreamingConfig;

            void Execute(
                [ChunkIndexInQuery] int entityInQueryIndex,
                Entity sectionEntity,
                in RoadSectionData roadSectionData)
            {
                var loaded = IsSectionLoadedLookup.HasComponent(sectionEntity);

                var roadSectionPos = roadSectionData.Position;
                var cullPointPos = CullPoint.Position;

                if (RoadStreamingConfig.Config.Value.IgnoreY)
                {
                    roadSectionPos = roadSectionPos.Flat();
                    cullPointPos = cullPointPos.Flat();
                }

                var distance = math.distancesq(roadSectionPos, cullPointPos);

                if (loaded)
                {
                    var unload = distance > RoadStreamingConfig.Config.Value.DistanceForStreamingOutSQ;

                    if (unload)
                    {
                        var sectionIndex = roadSectionData.SectionIndex;

                        if (SectionMapping.TryGetFirstValue(sectionIndex, out var hashEntity, out var nativeMultiHashMapIterator))
                        {
                            do
                            {
                                CommandBuffer.SetComponentEnabled<SegmentUnloadTag>(entityInQueryIndex, hashEntity, true);

                            } while (SectionMapping.TryGetNextValue(out hashEntity, ref nativeMultiHashMapIterator));
                        }

                        CommandBuffer.RemoveComponent<RequestSceneLoaded>(entityInQueryIndex, sectionEntity);
                    }
                }
                else
                {
                    var load = distance < RoadStreamingConfig.Config.Value.DistanceForStreamingInSQ;

                    if (load)
                    {
                        CommandBuffer.AddComponent(entityInQueryIndex, sectionEntity, new RequestSceneLoaded()
                        {
                            LoadFlags = SceneLoadFlags.NewInstance
                        });
                    }
                }
            }
        }
    }
}