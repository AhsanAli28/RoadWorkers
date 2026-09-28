#if !FMOD
using Spirit604.DotsCity.Core.Sound;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Sound
{
    [UpdateInGroup(typeof(InitGroup))]
    public partial class UnitySoundInitSystem : BeginInitSystemBase
    {
        private BuiltInSoundService soundService;

        private EntityQuery updateQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            updateQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithNone<AudioSourceBehaviour>()
                .WithAll<SoundComponent>()
                .Build(this);

            RequireForUpdate(updateQuery);
            Enabled = false;
        }

        protected override void OnUpdate()
        {
            var commandBuffer = GetCommandBuffer();

            Entities
            .WithoutBurst()
            .WithStructuralChanges()
            .WithNone<AudioSourceBehaviour>()
            .ForEach((
                Entity entity,
                in SoundVolume soundVolume,
                in SoundComponent sound,
                in LocalTransform transform) =>
            {
                if (EntityManager.HasComponent<OneShot>(entity))
                {
                    soundService.PlayOneShot(sound.Id, transform.Position);
                    commandBuffer.DestroyEntity(entity);
                    return;
                }

                var loop = EntityManager.HasComponent<LoopSoundData>(entity);
                var behaviour = soundService.GetSound(sound.Id, !loop);

                if (behaviour != null)
                {
                    EntityManager.AddComponentObject(entity, behaviour);
                    EntityManager.AddComponentObject(entity, behaviour.Transform);
                    behaviour.SetVolume(soundVolume.Volume);
                    behaviour.SetPitch(soundVolume.Pitch);
                }
                else
                {
                    commandBuffer.DestroyEntity(entity);
                }

            }).Run();

            AddCommandBufferForProducer();
        }

        public void Initialize(BuiltInSoundService soundService)
        {
            this.soundService = soundService;
            Enabled = true;
        }
    }
}
#endif