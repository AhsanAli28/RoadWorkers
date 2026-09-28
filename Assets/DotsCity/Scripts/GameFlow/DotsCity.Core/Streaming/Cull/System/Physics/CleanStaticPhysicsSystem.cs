using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Spirit604.DotsCity.Core
{
    [UpdateInGroup(typeof(EarlyJobGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct CleanStaticPhysicsSystem : ISystem
    {
        private EntityQuery cullQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            cullQuery = SystemAPI.QueryBuilder()
                .WithAll<CullPhysicsTag, Static, CullStateComponent>()
                .Build();

            state.RequireForUpdate(cullQuery);
            state.Enabled = false;
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var cleanStaticPhysicsJob = new CleanStaticPhysicsJob()
            {
            };

            state.Dependency = cleanStaticPhysicsJob.Schedule(cullQuery, state.Dependency);
        }

        [WithAll(typeof(CullPhysicsTag), typeof(Static), typeof(CullStateComponent))]
        [BurstCompile]
        public partial struct CleanStaticPhysicsJob : IJobEntity
        {
            void Execute(Entity entity, EnabledRefRW<CullStateComponent> cullStateComponentRW)
            {
                cullStateComponentRW.ValueRW = false;
            }
        }
    }
}