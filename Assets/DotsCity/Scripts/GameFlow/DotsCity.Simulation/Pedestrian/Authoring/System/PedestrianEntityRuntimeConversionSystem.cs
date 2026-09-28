using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Config;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian.Authoring
{
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial class PedestrianEntityRuntimeConversionSystem : SystemBase
    {
        private EntityQuery npcBakingQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            npcBakingQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<StateComponent>()
                .WithOptions(EntityQueryOptions.IncludePrefab)
                .Build(this);

            RequireForUpdate(npcBakingQuery);

            Enabled = false;
        }

        protected override void OnUpdate()
        {
            var pedestrianGeneralSettingsData = SystemAPI.GetSingleton<PedestrianGeneralSettingsReference>();

            if (pedestrianGeneralSettingsData.Config.Value.EntityBakingType != EntityBakingType.Runtime)
            {
                Enabled = false;
                return;
            }

            var conversionSettings = SystemAPI.GetSingleton<MiscConversionSettingsReference>();
            var coreSettingsData = SystemAPI.GetSingleton<GeneralCoreSettingsDataReference>();
            var commonGeneralSettingsData = SystemAPI.GetSingleton<CommonGeneralSettingsReference>();
            var pedestrianSettingsReference = SystemAPI.GetSingleton<PedestrianSettingsReference>();

            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            var entityManager = EntityManager;

            Entities
            .WithoutBurst()
            .WithStructuralChanges()
            .WithEntityQueryOptions(EntityQueryOptions.IncludePrefab)
            .WithAll<StateComponent>()
            .ForEach((
                Entity entity,
                ref CircleColliderComponent circleColliderComponent) =>
            {
                PedestrianEntityBakingUtils.Bake(
                    ref commandBuffer,
                    ref entityManager,
                    entity,
                    in conversionSettings,
                    in coreSettingsData,
                    in commonGeneralSettingsData,
                    in pedestrianGeneralSettingsData,
                    in pedestrianSettingsReference);
            }).Run();

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();

            Enabled = false;
        }
    }
}