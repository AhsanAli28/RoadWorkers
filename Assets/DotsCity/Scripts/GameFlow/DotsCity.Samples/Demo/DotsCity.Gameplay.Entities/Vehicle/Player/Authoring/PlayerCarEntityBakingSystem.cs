using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation;
using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Collections;
using Unity.Entities;
using static Spirit604.DotsCity.Gameplay.Player.Authoring.PlayerCarEntityAuthoring;

namespace Spirit604.DotsCity.Gameplay.Player.Authoring
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(PostBakingSystemGroup))]
    public partial class PlayerCarEntityBakingSystem : SystemBase
    {
        private EntityQuery bakingCarQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            bakingCarQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<PlayerCarEntityBakingTag>()
                .WithOptions(EntityQueryOptions.IncludePrefab)
                .Build(this);

            RequireForUpdate(bakingCarQuery);
            RequireForUpdate<RaycastConfigReference>();
            RequireForUpdate<TrafficCommonSettingsConfigBlobReference>();
        }

        protected override void OnUpdate()
        {
            var trafficGeneralSettingsData = SystemAPI.GetSingleton<TrafficGeneralSettingsReference>();

            if (trafficGeneralSettingsData.Config.Value.EntityBakingType != EntityBakingType.EditorSubscene)
                return;

            var raycastConfigReference = SystemAPI.GetSingleton<RaycastConfigReference>();
            var trafficCommonSettingsConfigBlobReference = SystemAPI.GetSingleton<TrafficCommonSettingsConfigBlobReference>();
            var trafficSettingsConfigBlobReference = SystemAPI.GetSingleton<TrafficSettingsConfigBlobReference>();
            var generalCoreSettingsDataReference = SystemAPI.GetSingleton<GeneralCoreSettingsDataReference>();
            var entityManager = EntityManager;
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            Entities
            .WithoutBurst()
            .WithEntityQueryOptions(EntityQueryOptions.IncludePrefab)
            .WithAll<PlayerCarEntityBakingTag>()
            .ForEach((
                Entity prefabEntity) =>
            {
                PlayerCarEntityBakingUtils.Bake(
                    prefabEntity,
                    in raycastConfigReference,
                    in trafficCommonSettingsConfigBlobReference,
                    in trafficSettingsConfigBlobReference,
                    in generalCoreSettingsDataReference,
                    ref entityManager,
                    ref commandBuffer);

            }).Run();

            commandBuffer.Playback(entityManager);
            commandBuffer.Dispose();
        }
    }
}