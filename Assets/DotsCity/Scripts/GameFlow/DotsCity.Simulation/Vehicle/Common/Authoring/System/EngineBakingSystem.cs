using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;

namespace Spirit604.DotsCity.Simulation.Car.Authoring
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(BakingSystemGroup))]
    public partial class EngineBakingSystem : SystemBase
    {
        private EntityQuery bakingCarQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            bakingCarQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<EngineDamageBakingData>()
                .WithOptions(EntityQueryOptions.IncludePrefab)
                .Build(this);

            RequireForUpdate(bakingCarQuery);
            RequireForUpdate<TrafficGeneralSettingsReference>();
        }

        protected override void OnUpdate()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            bool hasEngineDamage = false;

            var trafficGeneralSettingsData = SystemAPI.GetSingleton<TrafficGeneralSettingsReference>();

            if (SystemAPI.HasSingleton<EngineStateSettingsHolder>() && trafficGeneralSettingsData.Config.Value.CarVisualDamageSystemSupport)
            {
                var engineStateSettingsHolder = SystemAPI.GetSingleton<EngineStateSettingsHolder>();

                if (engineStateSettingsHolder.SettingsReference != null && engineStateSettingsHolder.SettingsReference.IsCreated)
                {
                    try
                    {
                        hasEngineDamage = engineStateSettingsHolder.SettingsReference.Value.EngineDamageEnabled;
                    }
                    catch { }
                }
            }

            Entities
            .WithBurst()
            .WithEntityQueryOptions(EntityQueryOptions.IncludePrefab)
            .ForEach((
                Entity prefabEntity,
                in EngineDamageBakingData engineDamageBakingData) =>
            {
                if (hasEngineDamage)
                {
                    commandBuffer.AddComponent(prefabEntity, new EngineDamageData(engineDamageBakingData));
                }
            }).Schedule();

            CompleteDependency();
            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }
}