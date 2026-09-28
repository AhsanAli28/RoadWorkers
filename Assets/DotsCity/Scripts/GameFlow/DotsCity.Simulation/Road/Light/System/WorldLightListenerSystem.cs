using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Road
{
    [UpdateAfter(typeof(LightHandlerSwitchSystem))]
    [UpdateInGroup(typeof(SimulationGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    [BurstCompile]
    public partial struct WorldLightListenerSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithDisabled<LightHandlerStateUpdateTag>()
                .WithAll<LightFrameHandlerEntityComponent>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var switchLightJob = new SwitchLightJob()
            {
                LightHandlerLookup = SystemAPI.GetComponentLookup<LightHandlerComponent>(true)
            };

            switchLightJob.ScheduleParallel();
        }

        [WithDisabled(typeof(LightHandlerStateUpdateTag))]
        [BurstCompile]
        private partial struct SwitchLightJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<LightHandlerComponent> LightHandlerLookup;

            void Execute(
                ref LightFrameHandlerStateComponent lightFrameHandlerStateComponent,
                EnabledRefRW<LightHandlerStateUpdateTag> lightHandlerStateUpdateTagRW,
                in LightFrameHandlerEntityComponent lightHandlerComponent,
                in LightFrameData lightFrameData)
            {
                if (!LightHandlerLookup.HasComponent(lightHandlerComponent.RelatedEntityHandler))
                    return;

                var lightHandlerData = LightHandlerLookup[lightHandlerComponent.RelatedEntityHandler];
                var newState = lightHandlerData.State;

                if (lightFrameHandlerStateComponent.CurrentLightState != newState)
                {
                    lightHandlerStateUpdateTagRW.ValueRW = true;
                }
            }
        }
    }
}