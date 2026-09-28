using ProjectDawn.Navigation;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.ThirdParty.ProjectDawn
{
    [UpdateAfter(typeof(NavMeshSteeringSystem))]
    [UpdateInGroup(typeof(AgentPathingSystemGroup))]
    [BurstCompile]
    public partial struct AgentDisableSystem : ISystem
    {
        private EntityQuery updateQuery;
        private SystemHandle navMeshSteeringSystem;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<LocalTransform>()
                .WithPresent<IdleTag>()
                .WithAllRW<AgentBody, NavMeshPath>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var disableJob = new DisableJob()
            {
                CustomMovementTagLookup = SystemAPI.GetComponentLookup<CustomMovementTag>(true),
            };

            state.Dependency = disableJob.ScheduleParallel(updateQuery, state.Dependency);
        }

        [BurstCompile]
        public partial struct DisableJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<CustomMovementTag> CustomMovementTagLookup;

            void Execute(
                Entity entity,
                ref AgentBody agentBody,
                EnabledRefRO<IdleTag> IdleTagRO,
                EnabledRefRW<NavMeshPath> navMeshPathRW,
                in LocalTransform localTransform)
            {
                bool disable = false;

                if (CustomMovementTagLookup.HasComponent(entity))
                {
                    disable = true;
                }

                if (math.distancesq(agentBody.Destination, localTransform.Position) == 0)
                {
                    disable = true;
                }

                if (IdleTagRO.ValueRO)
                {
                    disable = true;
                }

                if (disable)
                {
                    navMeshPathRW.ValueRW = false;
                    agentBody.Stop();
                }
            }
        }
    }
}