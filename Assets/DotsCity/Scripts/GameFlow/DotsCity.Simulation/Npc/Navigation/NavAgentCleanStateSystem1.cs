#if REESE_PATH
using Reese.Path;
using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Npc.Navigation
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct NavAgentCleanStateSystem1 : ISystem
    {
        private EntityQuery cleanQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            cleanQuery = SystemAPI.QueryBuilder()
                .WithNone<PathPlanning, EnabledNavigationTag, PathDestination>()
                .WithAll<NavAgentTag, PathBufferElement>()
                .WithAllRW<NavAgentComponent, NavAgentSteeringComponent>()
                .Build();

            state.RequireForUpdate(cleanQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var cleanNavStateJob = new CleanNavStateJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
            };

            cleanNavStateJob.Schedule(cleanQuery);
        }
    }
}
#endif