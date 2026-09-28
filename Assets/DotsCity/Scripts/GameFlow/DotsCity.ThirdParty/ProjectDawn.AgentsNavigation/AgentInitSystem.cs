using Spirit604.DotsCity.Simulation.Npc.Navigation;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Spirit604.DotsCity.ThirdParty.ProjectDawn
{
    [UpdateInGroup(typeof(EarlyEventGroup))]
    [BurstCompile]
    public partial struct AgentInitSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAllRW<AgentInitTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var initJob = new InitJob()
            {
            };

            initJob.Schedule();
        }

        [WithNone(typeof(EnabledNavigationTag))]
        [BurstCompile]
        public partial struct InitJob : IJobEntity
        {
            void Execute(
                EnabledRefRW<AgentInitTag> initTagRW,
                in LocalTransform localTransform,
                in DestinationComponent destinationComponent)
            {
                if (!destinationComponent.Value.Equals(localTransform.Position))
                    initTagRW.ValueRW = false;
            }
        }
    }
}