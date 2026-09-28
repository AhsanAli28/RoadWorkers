using Spirit604.DotsCity.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Weapon
{
    [UpdateInGroup(typeof(SimulationGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct BulletLifeTimeSystem : ISystem
    {
        private EntityQuery updateGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateGroup = SystemAPI.QueryBuilder()
                .WithDisabledRW<PooledEventTag>()
                .WithAll<BulletStatsComponent>()
                .Build();

            state.RequireForUpdate(updateGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var bulletLifetimeJob = new BulletLifetimeJob()
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
            };

            bulletLifetimeJob.ScheduleParallel();
        }

        [WithDisabled(typeof(PooledEventTag))]
        [BurstCompile]
        private partial struct BulletLifetimeJob : IJobEntity
        {
            [ReadOnly]
            public float DeltaTime;

            void Execute(
                ref BulletStatsComponent bulletStatsComponent,
                EnabledRefRW<PooledEventTag> pooledEventTagRW)
            {
                bulletStatsComponent.LifeTime -= DeltaTime;

                if (bulletStatsComponent.LifeTime <= 0)
                {
                    PoolEntityUtils.DestroyEntity(ref pooledEventTagRW);
                }
            }
        }
    }
}