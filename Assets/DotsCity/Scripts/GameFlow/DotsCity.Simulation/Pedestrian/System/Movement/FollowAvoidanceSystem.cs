using Spirit604.DotsCity.Simulation.Npc.Navigation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateAfter(typeof(LocalAvoidanceObstacleSystem))]
    [UpdateInGroup(typeof(NavSimulationGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct FollowAvoidanceSystem : ISystem
    {
        private EntityQuery updateGroup;
        private SystemHandle localAvoidanceObstacleSystem;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            localAvoidanceObstacleSystem = state.WorldUnmanaged.GetExistingUnmanagedSystem<LocalAvoidanceObstacleSystem>();

            updateGroup = SystemAPI.QueryBuilder()
                .WithDisabledRW<AchievedNavTargetTag>()
                .WithAllRW<NavAgentSteeringComponent, NavAgentComponent>()
                .WithAllRW<PathPointAvoidanceElement>()
                .WithPresent<EnabledNavigationTag>()
                .WithAll<PathLocalAvoidanceEnabledTag, LocalAvoidanceAgentTag, LocalTransform>()
                .Build();

            state.RequireForUpdate(updateGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            ref var localAvoidanceObstacleSystemRef = ref state.WorldUnmanaged.ResolveSystemStateRef(localAvoidanceObstacleSystem);

            var followAvoidanceJob = new FollowAvoidanceJob()
            {
                ObstacleAvoidanceSettingsReference = SystemAPI.GetSingleton<ObstacleAvoidanceSettingsReference>(),
            };

            state.Dependency = followAvoidanceJob.ScheduleParallel(updateGroup, localAvoidanceObstacleSystemRef.Dependency);
        }

        [WithDisabled(typeof(AchievedNavTargetTag))]
        [WithAll(typeof(PathLocalAvoidanceEnabledTag), typeof(LocalAvoidanceAgentTag))]
        [BurstCompile]
        private partial struct FollowAvoidanceJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter CommandBuffer;

            [ReadOnly]
            public ObstacleAvoidanceSettingsReference ObstacleAvoidanceSettingsReference;

            void Execute(
                ref NavAgentSteeringComponent navAgentSteeringComponent,
                ref NavAgentComponent navAgentComponent,
                ref DynamicBuffer<PathPointAvoidanceElement> points,
                EnabledRefRO<EnabledNavigationTag> enabledNavigationTagRO,
                EnabledRefRW<AchievedNavTargetTag> achievedNavTargetTagRW,
                in LocalTransform transform)
            {
                if (!enabledNavigationTagRO.ValueRO)
                {
                    achievedNavTargetTagRW.ValueRW = true;
                    return;
                }

                float distance = math.distancesq(transform.Position, navAgentSteeringComponent.SteeringTargetValue);

                if (distance < ObstacleAvoidanceSettingsReference.SettingsReference.Value.AchieveDistanceSQ)
                {
                    if (points.Length > 0)
                    {
                        points.RemoveAt(0);
                    }

                    if (points.Length > 0)
                    {
                        navAgentSteeringComponent.SteeringTargetValue = points[0].Point;
                    }
                    else
                    {
                        navAgentComponent.ObstacleEntity = Entity.Null;
                        achievedNavTargetTagRW.ValueRW = true;
                    }
                }
            }
        }
    }
}