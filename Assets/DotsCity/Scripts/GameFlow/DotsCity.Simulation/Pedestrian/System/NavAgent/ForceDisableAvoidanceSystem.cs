using Spirit604.DotsCity.Simulation.Npc.Navigation;
using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(LateEventGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct ForceDisableAvoidanceSystem : ISystem
    {
        private EntityQuery pedestrianGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            pedestrianGroup = SystemAPI.QueryBuilder()
                .WithDisabled<EnabledNavigationTag>()
                .WithAll<PathLocalAvoidanceEnabledTag>()
                .Build();

            state.RequireForUpdate(pedestrianGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var forceAvoidanceJob = new ForceAvoidanceJob()
            {
            };

            forceAvoidanceJob.Schedule();
        }

        [WithDisabled(typeof(EnabledNavigationTag))]
        [WithAll(typeof(PathLocalAvoidanceEnabledTag))]
        [BurstCompile]
        private partial struct ForceAvoidanceJob : IJobEntity
        {
            private void Execute(
                ref DynamicBuffer<PathPointAvoidanceElement> pathBuffer,
                ref NavAgentComponent navAgentComponent,
                ref NavAgentSteeringComponent navAgentSteeringComponent,
                EnabledRefRW<PathLocalAvoidanceEnabledTag> pathLocalAvoidanceEnabledTagRW)
            {
                pathBuffer.Clear();
                DisableAvoidanceSystem.ClearAvoidance(ref navAgentComponent, ref navAgentSteeringComponent, ref pathLocalAvoidanceEnabledTagRW);
            }
        }
    }
}