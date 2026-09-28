#if FMOD
using Spirit604.DotsCity.Core;
using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Sound
{
    [UpdateInGroup(typeof(DestroyGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct FMODPoolSoundSystem : ISystem
    {
        private EntityQuery cleanupQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            cleanupQuery = SystemAPI.QueryBuilder()
                .WithAll<PooledEventTag, FMODSound>()
                .Build();

            state.RequireForUpdate(cleanupQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var stopSoundJob = new StopSoundJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                DestroyEntity = true,
            };

            stopSoundJob.Schedule(cleanupQuery);
        }
    }
}
#endif