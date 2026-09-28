using Spirit604.Gameplay.Road;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Road
{
    [UpdateInGroup(typeof(SimulationGroup))]
    [BurstCompile]
    public partial struct LightHandlerSwitchSystem : ISystem
    {
        private EntityQuery lightQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            lightQuery = SystemAPI.QueryBuilder()
                .WithNone<LightHandlerInitTag, LightHandlerOverrideStateTag>()
                .WithPresentRW<LightHandlerStateUpdateTag>()
                .WithAllRW<LightHandlerComponent>()
                .WithAll<LightHandlerStateElement>()
                .Build();

            state.RequireForUpdate(lightQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var switchLightJob = new SwitchLightJob()
            {
                CurrentTime = (float)SystemAPI.Time.ElapsedTime,
            };

            switchLightJob.ScheduleParallel(lightQuery);
        }

        [WithNone(typeof(LightHandlerInitTag), typeof(LightHandlerOverrideStateTag))]
        [BurstCompile]
        public partial struct SwitchLightJob : IJobEntity
        {
            [ReadOnly]
            public float CurrentTime;

            void Execute(
                ref LightHandlerComponent lightHandlerComponent,
                EnabledRefRW<LightHandlerStateUpdateTag> lightHandlerStateUpdateTagRW,
                in DynamicBuffer<LightHandlerStateElement> lightStates)
            {
                bool shouldSwitchState = lightHandlerComponent.NextSwitchTime <= CurrentTime;

                if (!shouldSwitchState)
                    return;

                if (lightStates.Length > 0)
                {
                    LightState lightState = lightHandlerComponent.State;

                    float duration = 0;

                    while (duration == 0)
                    {
                        lightHandlerComponent.StateIndex = ++lightHandlerComponent.StateIndex % lightStates.Length;

                        lightState = lightStates[lightHandlerComponent.StateIndex].LightState;
                        duration = lightStates[lightHandlerComponent.StateIndex].Duration;
                    }

                    lightHandlerComponent.State = lightState;
                    lightHandlerComponent.NextSwitchTime += duration;
                    lightHandlerStateUpdateTagRW.ValueRW = true;
                }
            }
        }
    }
}
