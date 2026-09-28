using ProjectDawn.Navigation;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.ThirdParty.ProjectDawn
{
    [UpdateBefore(typeof(AgentLocomotionSystem))]
    [UpdateInGroup(typeof(AgentLocomotionSystemGroup))]
    [BurstCompile]
    public partial struct AgentBodySyncSpeedSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<AgentLocomotion, PedestrianMovementSettings>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var syncJob = new SyncJob()
            {
            };

            syncJob.ScheduleParallel();
        }

        [WithChangeFilter(typeof(PedestrianMovementSettings))]
        [BurstCompile]
        public partial struct SyncJob : IJobEntity
        {
            void Execute(
                ref AgentLocomotion agentLocomotion,
                in PedestrianMovementSettings movementSettings)
            {
                agentLocomotion.Speed = movementSettings.CurrentMovementSpeed;
            }
        }
    }
}