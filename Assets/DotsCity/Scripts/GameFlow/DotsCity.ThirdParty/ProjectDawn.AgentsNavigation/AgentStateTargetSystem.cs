using ProjectDawn.Navigation;
using Spirit604.DotsCity.Simulation.Npc.Navigation;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.ThirdParty.ProjectDawn
{
    [UpdateInGroup(typeof(EarlyJobGroup))]
    [BurstCompile]
    public partial struct AgentStateTargetSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<CustomMovementTag, IdleTag, AgentInitTag>()
                .WithDisabledRW<UpdateNavTargetTag>()
                .WithPresent<NavMeshPath>()
                .WithAllRW<PersistNavigationComponent, NavAgentComponent>()
                .WithAllRW<DestinationComponent>()
                .WithAll<AgentBody>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var agentPersistListenJob = new AgentPersistListenJob()
            {
            };

            agentPersistListenJob.ScheduleParallel(updateQuery);
        }

        [WithNone(typeof(CustomMovementTag), typeof(IdleTag), typeof(AgentInitTag))]
        [WithDisabled(typeof(UpdateNavTargetTag))]
        [BurstCompile]
        public partial struct AgentPersistListenJob : IJobEntity
        {
            void Execute(
                ref PersistNavigationComponent persistNavigationComponent,
                ref NavAgentComponent navAgentComponent,
                ref DestinationComponent destinationComponent,
                EnabledRefRW<UpdateNavTargetTag> updateNavTargetTagRW,
                in NavMeshPath navMeshPath,
                in AgentBody agentBody)
            {
                bool update = false;

                if (persistNavigationComponent.CurrentEntity != destinationComponent.DestinationNode)
                {
                    persistNavigationComponent.CurrentEntity = destinationComponent.DestinationNode;
                    update = true;
                }

                if (agentBody.IsStopped)
                {
                    update = true;

                    if (navMeshPath.State == NavMeshPathState.FinishedPartialPath)
                    {
                        destinationComponent = destinationComponent.SwapBack();
                    }
                }

                if (update)
                {
                    navAgentComponent.PathEndPosition = destinationComponent.Value;
                    updateNavTargetTagRW.ValueRW = true;
                }
            }
        }
    }
}