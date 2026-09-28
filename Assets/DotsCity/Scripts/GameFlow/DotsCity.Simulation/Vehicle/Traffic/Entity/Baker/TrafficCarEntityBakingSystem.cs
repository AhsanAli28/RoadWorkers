using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Config;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic.Authoring
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(BakingSystemGroup))]
    public partial class TrafficEntityBakingSystem : SystemBase
    {
        private EntityQuery bakingCarQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            bakingCarQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<TrafficEntityBakingTag>()
                .WithOptions(EntityQueryOptions.IncludePrefab)
                .Build(this);

            RequireForUpdate(bakingCarQuery);
            RequireForUpdate<TrafficGeneralSettingsReference>();
        }

        protected override void OnUpdate()
        {
            var trafficGeneralSettingsData = SystemAPI.GetSingleton<TrafficGeneralSettingsReference>();

            if (trafficGeneralSettingsData.Config.Value.EntityBakingType != EntityBakingType.EditorSubscene)
                return;

            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            var trafficCommonSettingsConfigBlobReference = SystemAPI.GetSingleton<TrafficCommonSettingsConfigBlobReference>();
            var trafficSettingsConfigBlobReference = SystemAPI.GetSingleton<TrafficSettingsConfigBlobReference>();
            var trafficRailConfigReference = SystemAPI.GetSingleton<TrafficRailConfigReference>();
            var trafficCollisionConfigReference = SystemAPI.GetSingleton<TrafficCollisionConfigReference>();
            var pedestrianGeneralSettingsData = SystemAPI.GetSingleton<PedestrianGeneralSettingsReference>();
            var pedestrianSettingsReference = SystemAPI.GetSingleton<PedestrianSettingsReference>();
            var coreSettingsData = SystemAPI.GetSingleton<GeneralCoreSettingsDataReference>();
            var commonGeneralSettingsData = SystemAPI.GetSingleton<CommonGeneralSettingsReference>();

            var entityManager = EntityManager;

            Entities
            .WithoutBurst()
            .WithEntityQueryOptions(EntityQueryOptions.IncludePrefab)
            .WithAll<TrafficEntityBakingTag>()
            .ForEach((
                Entity prefabEntity) =>
            {
                TrafficEntityBakingUtils.Bake(
                    ref commandBuffer,
                    ref entityManager,
                    prefabEntity,
                    in trafficCommonSettingsConfigBlobReference,
                    in trafficSettingsConfigBlobReference,
                    in trafficGeneralSettingsData,
                    in trafficRailConfigReference,
                    in trafficCollisionConfigReference,
                    in pedestrianGeneralSettingsData,
                    in pedestrianSettingsReference,
                    in coreSettingsData,
                    in commonGeneralSettingsData);

            }).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }
}