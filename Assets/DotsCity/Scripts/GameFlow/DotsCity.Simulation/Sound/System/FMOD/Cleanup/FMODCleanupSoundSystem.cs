#if FMOD
using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Sound
{
    [UpdateInGroup(typeof(CleanupGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct FMODCleanupSoundSystem : ISystem
    {
        private EntityQuery cleanupQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            cleanupQuery = SystemAPI.QueryBuilder()
                .WithNone<SoundComponent>()
                .WithAll<FMODSound>()
                .Build();

            state.RequireForUpdate(cleanupQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var stopSoundJob = new StopSoundJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                DestroyEntity = false,
            };

            stopSoundJob.Schedule(cleanupQuery);
        }
    }
}
#endif