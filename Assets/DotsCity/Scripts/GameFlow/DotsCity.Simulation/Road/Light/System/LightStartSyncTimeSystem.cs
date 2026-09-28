using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Road.Authoring
{
    [UpdateInGroup(typeof(PreEarlyJobGroup))]
    [BurstCompile]
    public partial struct LightStartSyncTimeSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<LightHandlerOverrideStateTag>()
                .WithPresentRW<LightHandlerStateUpdateTag>()
                .WithAllRW<LightHandlerComponent, LightHandlerInitTag>()
                .WithAll<LightHandlerStateElement>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var initLightJob = new InitLightJob()
            {
                CurrentTime = (float)SystemAPI.Time.ElapsedTime,
            };

            initLightJob.Schedule(updateQuery);
        }

        [BurstCompile]
        [WithNone(typeof(LightHandlerOverrideStateTag))]
        private partial struct InitLightJob : IJobEntity
        {
            [ReadOnly]
            public float CurrentTime;

            void Execute(
                ref LightHandlerComponent lightComponent,
                EnabledRefRW<LightHandlerStateUpdateTag> lightHandlerStateUpdateTagRW,
                EnabledRefRW<LightHandlerInitTag> lightHandlerInitTagRW,
                in DynamicBuffer<LightHandlerStateElement> lightStates)
            {
                lightComponent.NextSwitchTime = CurrentTime;

                var currentCycleTime = CurrentTime % lightComponent.CycleDuration;

                float cycleTime = 0;

                for (int stateIndex = 0; stateIndex < lightStates.Length; stateIndex++)
                {
                    cycleTime += lightStates[stateIndex].Duration;

                    if (cycleTime > currentCycleTime)
                    {
                        var remainTime = cycleTime - currentCycleTime;

                        lightComponent.NextSwitchTime = CurrentTime + remainTime;
                        lightComponent.State = lightStates[stateIndex].LightState;
                        lightComponent.StateIndex = stateIndex;
                        lightHandlerStateUpdateTagRW.ValueRW = true;
                        break;
                    }
                }

                lightHandlerInitTagRW.ValueRW = false;
            }
        }
    }
}