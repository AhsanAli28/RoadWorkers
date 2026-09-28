using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Core
{
    [UpdateInGroup(typeof(DestroyGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct LifeTimeEntitySystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<LifeTimeComponent>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var lifetimeJob = new LifetimeJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                currentTime = (float)SystemAPI.Time.ElapsedTime
            };

            lifetimeJob.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct LifetimeJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter CommandBuffer;

            [ReadOnly]
            public float currentTime;

            void Execute(
                Entity entity,
                [ChunkIndexInQuery] int entityInQueryIndex,
                in LifeTimeComponent lifeTimeComponent)
            {
                if (currentTime >= lifeTimeComponent.DestroyTimeStamp)
                {
                    CommandBuffer.DestroyEntity(entityInQueryIndex, entity);
                }
            }
        }
    }
}