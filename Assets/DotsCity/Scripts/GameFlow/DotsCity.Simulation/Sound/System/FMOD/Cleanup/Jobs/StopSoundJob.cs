#if FMOD
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Sound
{
    [BurstCompile]
    public partial struct StopSoundJob : IJobEntity
    {
        public EntityCommandBuffer CommandBuffer;

        [ReadOnly]
        public bool DestroyEntity;

        void Execute(
            Entity entity,
            in FMODSound sound)
        {
            sound.Event.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            sound.Event.release();
            CommandBuffer.RemoveComponent<FMODSound>(entity);

            if (DestroyEntity)
            {
                CommandBuffer.DestroyEntity(entity);
            }
        }
    }
}
#endif