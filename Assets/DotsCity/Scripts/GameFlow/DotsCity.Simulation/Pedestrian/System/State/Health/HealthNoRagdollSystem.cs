using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Npc;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(LateEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct HealthNoRagdollSystem : ISystem
    {
        private EntityQuery updateGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateGroup = SystemAPI.QueryBuilder()
                .WithNone<AliveTag, PooledEventTag, RagdollComponent>()
                .WithAll<StateComponent>()
                .Build();

            state.RequireForUpdate(updateGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var pedestrianCleanJob = new CleanJob()
            {
            };

            pedestrianCleanJob.Schedule();
        }

        [WithNone(typeof(RagdollComponent))]
        [WithDisabled(typeof(AliveTag), typeof(PooledEventTag))]
        [BurstCompile]
        public partial struct CleanJob : IJobEntity
        {
            void Execute(
                EnabledRefRW<PooledEventTag> pooledEventTagRW)
            {
                PoolEntityUtils.DestroyEntity(ref pooledEventTagRW);
            }
        }
    }
}