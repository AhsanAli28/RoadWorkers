#if FMOD
using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Sound
{
    [UpdateInGroup(typeof(LateEventGroup))]
    [BurstCompile]
    public partial struct FMODEventPlaybackSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAllRW<SoundEventComponent>()
                .WithAll<FMODSound>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var eventJob = new FMODEventJob()
            {
            };

            eventJob.Schedule();
        }

        [WithChangeFilter(typeof(SoundEventComponent))]
        [BurstCompile]
        public partial struct FMODEventJob : IJobEntity
        {
            void Execute(
                ref SoundEventComponent soundEventComponent,
                in FMODSound fMODSound)
            {
                if (soundEventComponent.NewEvent == SoundEventType.Default)
                    return;

                switch (soundEventComponent.NewEvent)
                {
                    case SoundEventType.Stop:
                        fMODSound.Event.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                        break;
                    case SoundEventType.StopFadeout:
                        fMODSound.Event.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                        break;
                    case SoundEventType.Play:
                        fMODSound.Event.start();
                        break;
                }

                soundEventComponent.NewEvent = SoundEventType.Default;
            }
        }
    }
}
#endif