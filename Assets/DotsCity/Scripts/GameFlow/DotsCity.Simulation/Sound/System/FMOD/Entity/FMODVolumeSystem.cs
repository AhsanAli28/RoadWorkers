#if FMOD
using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Sound
{
    [UpdateInGroup(typeof(MainThreadInitGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct FMODVolumeSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAllRW<SoundCacheVolume>()
                .WithAll<FMODSound, SoundVolume>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var volumeJob = new FMODVolumeJob()
            {
            };

            volumeJob.Run();
        }

        [WithChangeFilter(typeof(SoundVolume))]
        [BurstCompile]
        public partial struct FMODVolumeJob : IJobEntity
        {
            void Execute(
                ref SoundCacheVolume soundCacheVolume,
                in SoundVolume soundVolume,
                in FMODSound fMODSound)
            {
                if (soundCacheVolume.PreviousVolume != soundVolume.Volume)
                {
                    soundCacheVolume.PreviousVolume = soundVolume.Volume;
                    fMODSound.Event.setVolume(soundVolume.Volume);
                }

                if (soundCacheVolume.PreviousPitch != soundVolume.Pitch)
                {
                    soundCacheVolume.PreviousPitch = soundVolume.Pitch;
                    fMODSound.Event.setPitch(soundVolume.Pitch);
                }
            }
        }
    }
}
#endif