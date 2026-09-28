using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Player.Authoring
{
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial class PlayerCarEntityRuntimeBakingSystem : SystemBase
    {
        private EntityQuery bakingCarQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            bakingCarQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<PlayerTag, CarTypeComponent, Prefab>()
                .WithOptions(EntityQueryOptions.IncludePrefab)
                .Build(this);

            RequireForUpdate(bakingCarQuery);
            RequireForUpdate<RaycastConfigReference>();
            RequireForUpdate<TrafficCommonSettingsConfigBlobReference>();
        }

        protected override void OnUpdate()
        {
            var trafficGeneralSettingsData = SystemAPI.GetSingleton<TrafficGeneralSettingsReference>();

            if (trafficGeneralSettingsData.Config.Value.EntityBakingType != EntityBakingType.Runtime)
            {
                Enabled = false;
                return;
            }

            var raycastConfigReference = SystemAPI.GetSingleton<RaycastConfigReference>();
            var trafficCommonSettingsConfigBlobReference = SystemAPI.GetSingleton<TrafficCommonSettingsConfigBlobReference>();
            var trafficSettingsConfigBlobReference = SystemAPI.GetSingleton<TrafficSettingsConfigBlobReference>();
            var generalCoreSettingsDataReference = SystemAPI.GetSingleton<GeneralCoreSettingsDataReference>();
            var entityManager = EntityManager;
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            Entities
            .WithoutBurst()
            .WithEntityQueryOptions(EntityQueryOptions.IncludePrefab)
            .WithAll<PlayerTag, CarTypeComponent, Prefab>()
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

            Enabled = false;
        }
    }
}