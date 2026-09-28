#if !FMOD
using Spirit604.DotsCity.Core.Sound;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Sound
{
    [UpdateInGroup(typeof(MainThreadEventPlaybackGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class UnitySoundEventPlaybackSystem : SystemBase
    {
        private EntityQuery updateQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            updateQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAllRW<SoundEventComponent>()
                .WithAll<AudioSourceBehaviour>()
                .Build(this);

            RequireForUpdate(updateQuery);
        }

        protected override void OnUpdate()
        {
            Entities
            .WithoutBurst()
            .WithChangeFilter<SoundEventComponent>()
            .ForEach((
                AudioSourceBehaviour audioSourceBehaviour,
                ref SoundEventComponent soundEventComponent) =>
            {
                if (soundEventComponent.NewEvent == SoundEventType.Default)
                    return;

                switch (soundEventComponent.NewEvent)
                {
                    case SoundEventType.Stop:
                        audioSourceBehaviour.Stop();
                        break;
                    case SoundEventType.StopFadeout:
                        audioSourceBehaviour.StopFadeout();
                        break;
                    case SoundEventType.Play:
                        audioSourceBehaviour.Replay();
                        break;
                }

                soundEventComponent.NewEvent = SoundEventType.Default;
            }).Run();
        }
    }
}
#endif