#if FMOD
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Sound
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct FMODSoundDelaySystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<SoundDelayData>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var delaySoundJob = new DelaySoundJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                OneShotLookup = SystemAPI.GetComponentLookup<OneShot>(true),
                CurrentTimestamp = (float)SystemAPI.Time.ElapsedTime
            };

            delaySoundJob.Schedule();
        }

        [BurstCompile]
        public partial struct DelaySoundJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public ComponentLookup<OneShot> OneShotLookup;

            [ReadOnly]
            public float CurrentTimestamp;

            void Execute(
                Entity entity,
                ref SoundDelayData loopSoundData,
                in FMODSound fMODSound)
            {
                if (loopSoundData.FinishTimestamp == 0)
                {
                    loopSoundData.FinishTimestamp = CurrentTimestamp + loopSoundData.Duration;
                }

                bool passed = loopSoundData.FinishTimestamp <= CurrentTimestamp;

                if (passed)
                {
                    fMODSound.Event.start();

                    if (OneShotLookup.HasComponent(entity))
                    {
                        fMODSound.Event.release();
                        CommandBuffer.RemoveComponent<FMODSound>(entity);
                        CommandBuffer.DestroyEntity(entity);
                    }
                    else
                    {
                        CommandBuffer.RemoveComponent<SoundDelayData>(entity);
                    }
                }
            }
        }
    }
}
#endif