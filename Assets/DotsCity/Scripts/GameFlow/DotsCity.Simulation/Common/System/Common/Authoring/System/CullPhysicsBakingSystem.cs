using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Config;
using Spirit604.DotsCity.Simulation.Level.Streaming;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Common.Authoring
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(PostBakingSystemGroup))]
    public partial class CullPhysicsBakingSystem : SystemBase
    {
        private EntityQuery bakingQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            bakingQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CullPhysicsTag, PhysicsWorldIndex>()
                .WithOptions(EntityQueryOptions.IncludePrefab)
                .Build(this);

            RequireForUpdate(bakingQuery);
            RequireForUpdate<StreamingLevelConfig>();
            RequireForUpdate<RoadStreamingConfigReference>();
            RequireForUpdate<CommonGeneralSettingsReference>();
        }

        protected override void OnUpdate()
        {
            var streamingLevelConfig = SystemAPI.GetSingleton<StreamingLevelConfig>();
            var roadStreamingConfigReference = SystemAPI.GetSingleton<RoadStreamingConfigReference>();
            var commonGeneralSettingsReference = SystemAPI.GetSingleton<GeneralCoreSettingsDataReference>();

            bool cullStaticPhysics = commonGeneralSettingsReference.Config.Value.CullStaticPhysics;

            bool hasCulling = streamingLevelConfig.StreamingIsEnabled ||
                roadStreamingConfigReference.Config.Value.StreamingIsEnabled ||
                commonGeneralSettingsReference.Config.Value.CullPhysics;

            if (!hasCulling)
            {
                return;
            }

            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            Entities
            .WithoutBurst()
            .WithEntityQueryOptions(EntityQueryOptions.IncludePrefab)
            .WithAll<CullPhysicsTag, PhysicsWorldIndex>()
            .ForEach((
                Entity entity) =>
            {
                bool isStaticPhysics = EntityManager.HasComponent<Static>(entity);

                if (!isStaticPhysics || isStaticPhysics && cullStaticPhysics)
                {
                    commandBuffer.SetSharedComponent<PhysicsWorldIndex>(entity, new PhysicsWorldIndex()
                    {
                        Value = ProjectConstants.NoPhysicsWorldIndex
                    });
                }

            }).Run();

            CompleteDependency();
            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }
}