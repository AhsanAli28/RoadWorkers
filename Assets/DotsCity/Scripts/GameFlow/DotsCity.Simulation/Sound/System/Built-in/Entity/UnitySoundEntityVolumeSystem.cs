#if !FMOD
using Spirit604.DotsCity.Core.Sound;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Sound
{
    [UpdateInGroup(typeof(MainThreadInitGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class UnitySoundEntityVolumeSystem : SystemBase
    {
        private EntityQuery updateQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            updateQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAllRW<SoundCacheVolume>()
                .WithAll<AudioSourceBehaviour, SoundVolume>()
                .Build(this);

            RequireForUpdate(updateQuery);
        }

        protected override void OnUpdate()
        {
            Entities
            .WithoutBurst()
            .WithChangeFilter<SoundVolume>()
            .ForEach((
                AudioSourceBehaviour audioSourceBehaviour,
                ref SoundCacheVolume soundCacheVolume,
                in SoundVolume soundVolume) =>
            {
                if (soundCacheVolume.PreviousVolume != soundVolume.Volume)
                {
                    soundCacheVolume.PreviousVolume = soundVolume.Volume;
                    audioSourceBehaviour.SetVolume(soundVolume.Volume);
                }

                if (soundCacheVolume.PreviousPitch != soundVolume.Pitch)
                {
                    soundCacheVolume.PreviousPitch = soundVolume.Pitch;
                    audioSourceBehaviour.SetPitch(soundVolume.Pitch);
                }
            }).Run();
        }
    }
}
#endif