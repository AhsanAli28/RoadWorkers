using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Config;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Spirit604.DotsCity.Simulation.Level.Props.Authoring
{
    [TemporaryBakingType]
    public struct PropsBakingData : IComponentData
    {
        public float3 InitialPosition;
        public float3 InitialForward;
        public bool HasCustomPropReset;
    }

    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(BakingSystemGroup))]
    public partial class PropsBakingSystem : SystemBase
    {
        private EntityQuery bakingQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            bakingQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<PropsBakingData>()
                .WithOptions(EntityQueryOptions.IncludePrefab)
                .Build(this);

            RequireForUpdate(bakingQuery);
        }

        protected override void OnUpdate()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            var coreGeneralSettings = SystemAPI.GetSingleton<GeneralCoreSettingsDataReference>();
            var commonGeneralSettings = SystemAPI.GetSingleton<CommonGeneralSettingsReference>();

            Entities
            .WithBurst()
            .ForEach((
                Entity entity,
                in PropsBakingData propsBakingData) =>
            {
                if (commonGeneralSettings.Config.Value.PropsDamageSupport)
                {
                    commandBuffer.AddComponent(entity,
                        new PropsComponent()
                        {
                            InitialPosition = propsBakingData.InitialPosition,
                            InitialForward = propsBakingData.InitialForward,
                        });

                    commandBuffer.AddComponent<PropsDamagedTag>(entity);
                    commandBuffer.AddComponent<PropsProcessDamageTag>(entity);
                    commandBuffer.AddComponent<PropsResetTag>(entity);

                    commandBuffer.SetComponentEnabled<PropsDamagedTag>(entity, false);
                    commandBuffer.SetComponentEnabled<PropsProcessDamageTag>(entity, false);
                    commandBuffer.SetComponentEnabled<PropsResetTag>(entity, false);

                    if (propsBakingData.HasCustomPropReset)
                    {
                        commandBuffer.AddComponent<PropsCustomResetTag>(entity);
                    }
                }

                if (coreGeneralSettings.Config.Value.CullPhysics)
                {
                    commandBuffer.AddComponent<CullPhysicsTag>(entity);
                    commandBuffer.AddComponent<CustomCullPhysicsTag>(entity);
                }

            }).Schedule();

            CompleteDependency();
            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }
}