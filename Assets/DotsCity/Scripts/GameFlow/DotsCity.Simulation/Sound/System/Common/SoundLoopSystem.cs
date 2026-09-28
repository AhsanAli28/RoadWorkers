using Spirit604.DotsCity.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Sound
{
    [UpdateInGroup(typeof(CarSimulationGroup))]
    [BurstCompile]
    public partial struct SoundLoopSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<PooledEventTag>()
                .WithAll<LoopSoundData>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var soundLoopJob = new SoundLoopJob()
            {
                CurrentTimestamp = (float)SystemAPI.Time.ElapsedTime
            };

            soundLoopJob.Schedule();
        }

        [WithDisabled(typeof(PooledEventTag))]
        [BurstCompile]
        public partial struct SoundLoopJob : IJobEntity
        {
            [ReadOnly]
            public float CurrentTimestamp;

            void Execute(
                ref LoopSoundData loopSoundData,
                ref SoundEventComponent soundEventComponent,
                EnabledRefRW<PooledEventTag> pooledEventTagRW)
            {
                if (loopSoundData.FinishTimestamp == 0)
                {
                    loopSoundData.FinishTimestamp = CurrentTimestamp + loopSoundData.Duration;
                    soundEventComponent.NewEvent = SoundEventType.Play;
                }

                bool passed = loopSoundData.FinishTimestamp <= CurrentTimestamp;

                if (passed)
                {
                    PoolEntityUtils.DestroyEntity(ref pooledEventTagRW);
                }
            }
        }
    }
}
