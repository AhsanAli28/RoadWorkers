using ProjectDawn.Navigation;
using Spirit604.DotsCity.Simulation;
using Spirit604.DotsCity.Simulation.Npc.Navigation;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.ThirdParty.ProjectDawn
{
    [UpdateInGroup(typeof(NavSimulationGroup))]
    [BurstCompile]
    public partial struct AgentUpdateTargetSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<IdleTag>()
                .WithPresentRW<EnabledNavigationTag, NavMeshPath>()
                .WithAllRW<NavAgentComponent, AgentBody>()
                .WithAllRW<UpdateNavTargetTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var navTargetJob = new NavTargetJob()
            {
                NavAgentConfigReference = SystemAPI.GetSingleton<NavAgentConfigReference>(),
                Timestamp = (float)SystemAPI.Time.ElapsedTime,
            };

            navTargetJob.Schedule(updateQuery);
        }

        [WithNone(typeof(IdleTag))]
        [WithAll(typeof(UpdateNavTargetTag))]
        [BurstCompile]
        public partial struct NavTargetJob : IJobEntity
        {
            [ReadOnly]
            public NavAgentConfigReference NavAgentConfigReference;

            [ReadOnly]
            public float Timestamp;

            void Execute(
                ref NavAgentComponent navAgentComponent,
                ref AgentBody agentBody,
                EnabledRefRW<NavMeshPath> navMeshPathRW,
                EnabledRefRW<EnabledNavigationTag> enabledNavigationTagRW,
                EnabledRefRW<UpdateNavTargetTag> updateNavTargetTagRW)
            {
                updateNavTargetTagRW.ValueRW = false;

                if (Timestamp - navAgentComponent.LastUpdateTimestamp <= NavAgentConfigReference.Config.Value.UpdateFrequency)
                    return;

                if (navAgentComponent.PathEndPosition.x == 0 && navAgentComponent.PathEndPosition.z == 0)
                    return;

                navAgentComponent.LastUpdateTimestamp = Timestamp;

                navAgentComponent.PathIndex = 0;

                float3 target = navAgentComponent.PathEndPosition;

                agentBody.SetDestination(target);

                navMeshPathRW.ValueRW = true;
                enabledNavigationTagRW.ValueRW = true;
            }
        }
    }
}
