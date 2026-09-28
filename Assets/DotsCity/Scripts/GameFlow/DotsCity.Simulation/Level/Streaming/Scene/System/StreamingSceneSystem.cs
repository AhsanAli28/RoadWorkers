using Spirit604.DotsCity.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Hash128 = Unity.Entities.Hash128;

namespace Spirit604.DotsCity.Simulation.Level.Streaming
{
    [UpdateInGroup(typeof(InitGroup))]
    [BurstCompile]
    public partial struct StreamingSceneSystem : ISystem
    {
        private EntityQuery cullPointGroup;

        public void OnCreate(ref SystemState state)
        {
            cullPointGroup = state.GetEntityQuery(
                ComponentType.ReadOnly<LocalToWorld>(),
                ComponentType.ReadOnly<CullPointTag>());

            state.RequireForUpdate<RoadSceneData>();
            state.RequireForUpdate<StreamingLevelConfig>();
            state.RequireForUpdate(cullPointGroup);

            state.Enabled = false;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var streamingLevelConfig = SystemAPI.GetSingleton<StreamingLevelConfig>();

            if (!streamingLevelConfig.StreamingIsEnabled)
            {
                return;
            }

            var cullPointPosition = cullPointGroup.GetSingleton<LocalToWorld>().Position;

            var roadSceneData = SystemAPI.GetSingleton<RoadSceneData>();

            var streamingJob = new StreamingJob
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                CullPosition = cullPointPosition,
                MainSectionGUID = roadSceneData.Hash128,
                RequestSceneLoadedLookup = SystemAPI.GetComponentLookup<RequestSceneLoaded>(true),
                StreamingLevelConfig = streamingLevelConfig,
            };

            streamingJob.Schedule();
        }
    }

    [BurstCompile]
    public partial struct StreamingJob : IJobEntity
    {
        public EntityCommandBuffer CommandBuffer;

        public float3 CullPosition;
        public Hash128 MainSectionGUID;

        [ReadOnly]
        public ComponentLookup<RequestSceneLoaded> RequestSceneLoadedLookup;

        [ReadOnly]
        public StreamingLevelConfig StreamingLevelConfig;

        public void Execute(Entity entity, in SceneSectionData sceneData)
        {
            if (sceneData.SceneGUID == MainSectionGUID)
                return;

            AABB boundingVolume = sceneData.BoundingVolume;

            var distanceSQ = boundingVolume.DistanceSq(CullPosition);

            var loaded = RequestSceneLoadedLookup.HasComponent(entity);

            if (loaded)
            {
                if (distanceSQ > StreamingLevelConfig.DistanceForStreamingOutSQ)
                {
                    CommandBuffer.RemoveComponent<RequestSceneLoaded>(entity);
                }
            }
            else
            {
                if (distanceSQ < StreamingLevelConfig.DistanceForStreamingInSQ)
                {
                    CommandBuffer.AddComponent(entity, new RequestSceneLoaded { LoadFlags = SceneLoadFlags.LoadAdditive });
                }
            }
        }
    }
}