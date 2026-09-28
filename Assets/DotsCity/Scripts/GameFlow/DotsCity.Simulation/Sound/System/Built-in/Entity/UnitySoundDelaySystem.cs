#if !FMOD
using Spirit604.DotsCity.Core.Sound;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Sound
{
    [UpdateInGroup(typeof(StructuralSystemGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class UnitySoundDelaySystem : BeginSimulationSystemBase
    {
        private EntityQuery updateQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            updateQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAllRW<SoundDelayData>()
                .WithAll<AudioSourceBehaviour>()
                .Build(this);

            RequireForUpdate(updateQuery);
        }

        protected override void OnUpdate()
        {
            var time = (float)SystemAPI.Time.ElapsedTime;
            var commandBuffer = GetCommandBuffer();

            Entities
            .WithoutBurst()
            .ForEach((
                Entity entity,
                AudioSourceBehaviour audioSourceBehaviour,
                ref SoundDelayData loopSoundData) =>
            {
                if (loopSoundData.FinishTimestamp == 0)
                {
                    loopSoundData.FinishTimestamp = time + loopSoundData.Duration;
                }

                bool passed = loopSoundData.FinishTimestamp <= time;

                if (passed)
                {
                    audioSourceBehaviour.Play();
                    commandBuffer.RemoveComponent<SoundDelayData>(entity);
                }
            }).Run();

            AddCommandBufferForProducer();
        }
    }
}
#endif