using Unity.Burst;
using Unity.Entities;

#if REESE_PATH
using Reese.Path;
using Unity.Transforms;
using Unity.Collections;
using Unity.Mathematics;
#endif

namespace Spirit604.DotsCity.Simulation.Npc.Navigation
{
    [UpdateInGroup(typeof(NavSimulationGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct NavAgentNavigationSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
#if REESE_PATH
            updateQuery = SystemAPI.QueryBuilder()
                .WithDisabled<PathPlanning, AchievedNavTargetTag>()
                .WithAll<EnabledNavigationTag, NavAgentTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
#else
            state.Enabled = false;
#endif
        }

#if REESE_PATH

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var navJob = new NavJob()
            {
                PathProblemLookup = SystemAPI.GetComponentLookup<PathProblem>(true),
                NavAgentConfigReference = SystemAPI.GetSingleton<NavAgentConfigReference>()
            };

            navJob.ScheduleParallel();
        }

        [WithDisabled(typeof(PathPlanning), typeof(AchievedNavTargetTag))]
        [WithAll(typeof(EnabledNavigationTag), typeof(NavAgentTag))]
        [BurstCompile]
        public partial struct NavJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<PathProblem> PathProblemLookup;

            [ReadOnly]
            public NavAgentConfigReference NavAgentConfigReference;

            void Execute(
                 Entity entity,
                 ref DynamicBuffer<PathBufferElement> pathBuffer,
                 ref NavAgentComponent navAgentComponent,
                 ref NavAgentSteeringComponent navAgentSteeringComponent,
                 EnabledRefRW<AchievedNavTargetTag> achievedNavTargetTagRw,
                 in LocalTransform transform)
            {
                bool hasPath = pathBuffer.Length > 0 && !PathProblemLookup.HasComponent(entity);

                if (hasPath)
                {
                    int currentPathIndex = pathBuffer.Length - 1;
                    int finishPathIndex = 0;

                    float3 steeringTargetValue = pathBuffer[currentPathIndex];

                    float remainingDistanceToTarget = math.distance(transform.Position, pathBuffer[finishPathIndex].Value);
                    float remainingDistanceToSteeringTarget = math.distance(transform.Position, steeringTargetValue);

                    if (remainingDistanceToTarget < NavAgentConfigReference.Config.Value.MaxDistanceToTargetNode)
                    {
                        achievedNavTargetTagRw.ValueRW = true;
                        return;
                    }
                    else
                    {
                        navAgentComponent.RemainingDistance = remainingDistanceToTarget;
                    }

                    if (remainingDistanceToSteeringTarget < NavAgentConfigReference.Config.Value.MaxDistanceToTargetNode)
                    {
                        pathBuffer.RemoveAt(currentPathIndex);
                    }

                    navAgentSteeringComponent.SteeringTargetValue = steeringTargetValue;
                    navAgentSteeringComponent.SteeringTarget = 1;
                }

                navAgentComponent.HasPath = hasPath ? 1 : 0;

                if (!hasPath)
                {
                    achievedNavTargetTagRw.ValueRW = true;
                }
            }
        }

#endif
    }
}

