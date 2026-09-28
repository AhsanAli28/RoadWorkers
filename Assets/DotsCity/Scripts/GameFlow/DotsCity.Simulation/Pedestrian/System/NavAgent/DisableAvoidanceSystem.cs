using Spirit604.DotsCity.Simulation.Npc.Navigation;
using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(LateEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct DisableAvoidanceSystem : ISystem
    {
        private EntityQuery pedestrianGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            pedestrianGroup = SystemAPI.QueryBuilder()
                .WithAny<UpdateNavTargetTag, AchievedNavTargetTag>()
                .WithAll<PathLocalAvoidanceEnabledTag>()
                .Build();

            state.RequireForUpdate(pedestrianGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var disableAvoidanceJob = new DisableAvoidanceJob()
            {
                AchievedNavTargetLookup = SystemAPI.GetComponentLookup<AchievedNavTargetTag>(false),
            };

            disableAvoidanceJob.Schedule();
        }

        [WithAny(typeof(UpdateNavTargetTag), typeof(AchievedNavTargetTag))]
        [WithAll(typeof(PathLocalAvoidanceEnabledTag))]
        [BurstCompile]
        private partial struct DisableAvoidanceJob : IJobEntity
        {
            public ComponentLookup<AchievedNavTargetTag> AchievedNavTargetLookup;

            private void Execute(
                Entity entity,
                ref DynamicBuffer<PathPointAvoidanceElement> pathBuffer,
                ref NavAgentComponent navAgentComponent,
                ref NavAgentSteeringComponent navAgentSteeringComponent,
                EnabledRefRW<EnabledNavigationTag> enabledNavigationTagRW,
                EnabledRefRW<PathLocalAvoidanceEnabledTag> pathLocalAvoidanceEnabledTagRW)
            {
                enabledNavigationTagRW.ValueRW = false;

                if (AchievedNavTargetLookup.IsComponentEnabled(entity))
                {
                    AchievedNavTargetLookup.SetComponentEnabled(entity, false);
                }

                pathBuffer.Clear();
                ClearAvoidance(ref navAgentComponent, ref navAgentSteeringComponent, ref pathLocalAvoidanceEnabledTagRW);
            }
        }

        public static void ClearAvoidance(
            ref NavAgentComponent navAgentComponent,
            ref NavAgentSteeringComponent navAgentSteeringComponent,
            ref EnabledRefRW<PathLocalAvoidanceEnabledTag> pathLocalAvoidanceEnabledTagRW)
        {
            ClearAvoidance(ref navAgentComponent, ref navAgentSteeringComponent);

            pathLocalAvoidanceEnabledTagRW.ValueRW = false;
        }

        public static void ClearAvoidance(
          ref NavAgentComponent navAgentComponent,
          ref NavAgentSteeringComponent navAgentSteeringComponent)
        {
            navAgentSteeringComponent.SteeringTarget = 0;
            navAgentComponent.HasPath = 0;
        }
    }
}