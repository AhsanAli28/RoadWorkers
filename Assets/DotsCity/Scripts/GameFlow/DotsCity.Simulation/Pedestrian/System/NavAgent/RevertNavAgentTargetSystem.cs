using Unity.Burst;
using Unity.Entities;

#if REESE_PATH
using Reese.Path;
using Spirit604.DotsCity.Simulation.Npc.Navigation;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
#endif

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct RevertNavAgentTargetSystem : ISystem
    {
        private EntityQuery pedestrianGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
#if REESE_PATH
            pedestrianGroup = SystemAPI.QueryBuilder()
                .WithNone<PathPlanning, AntistuckActivateTag, AntistuckDestinationComponent>()
                .WithAll<EnabledNavigationTag, NavAgentTag, PathBufferElement>()
                .Build();

            state.RequireForUpdate(pedestrianGroup);
#else
            state.Enabled = false;
#endif
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
#if REESE_PATH
            var revertNavAgentTargetJob = new RevertNavAgentTargetJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                HasCollisionLookup = SystemAPI.GetComponentLookup<HasCollisionTag>(true),
                PathProblemLookup = SystemAPI.GetComponentLookup<PathProblem>(true),
                NavAgentConfigReference = SystemAPI.GetSingleton<NavAgentConfigReference>(),
                AntistuckConfigReference = SystemAPI.GetSingleton<AntistuckConfigReference>()
            };

            revertNavAgentTargetJob.Run();
#endif
        }

#if REESE_PATH
        [WithNone(typeof(PathPlanning), typeof(AntistuckActivateTag), typeof(AntistuckDestinationComponent))]
        [WithAll(typeof(EnabledNavigationTag), typeof(NavAgentTag), typeof(PathBufferElement))]
        [BurstCompile]
        private partial struct RevertNavAgentTargetJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public ComponentLookup<HasCollisionTag> HasCollisionLookup;

            [ReadOnly]
            public ComponentLookup<PathProblem> PathProblemLookup;

            [ReadOnly]
            public NavAgentConfigReference NavAgentConfigReference;

            [ReadOnly]
            public AntistuckConfigReference AntistuckConfigReference;

            void Execute(
                Entity entity,
                ref DestinationComponent destinationComponent,
                ref NavAgentSteeringComponent navAgentSteeringComponent,
                ref NavAgentComponent navAgentComponent,
                EnabledRefRW<EnabledNavigationTag> enabledNavigationTagRW,
                in CollisionComponent collisionComponent,
                in LocalTransform transform)
            {
                bool targetSwapped = false;
                bool hasCollision = HasCollisionLookup.HasComponent(entity) && HasCollisionLookup.IsComponentEnabled(entity);

                float remainingDistance = navAgentComponent.RemainingDistance;

                bool hasSteeringTarget = navAgentSteeringComponent.HasSteeringTarget;
                float distanceToTargetPoint = math.distance(transform.Position, navAgentSteeringComponent.SteeringTargetValue);

                bool shouldDisableAgent = navAgentComponent.HasPath == 1 && PathProblemLookup.HasComponent(entity);

                if (!shouldDisableAgent && NavAgentConfigReference.Config.Value.RevertTargetSupport)
                {
                    shouldDisableAgent = hasSteeringTarget && (distanceToTargetPoint >= NavAgentConfigReference.Config.Value.RevertSteeringTargetDistance && remainingDistance <= NavAgentConfigReference.Config.Value.RevertEndTargetRemainingDistance);
                }

                if (shouldDisableAgent)
                {
                    targetSwapped = true;
                }

                if (hasCollision)
                {
                    var outOfTime = collisionComponent.FirstCollisionTime != 0 && collisionComponent.CollideTime >= NavAgentConfigReference.Config.Value.MaxCollisionTime;

                    if (outOfTime)
                    {
                        navAgentSteeringComponent.SteeringTargetValue = default;
                        navAgentSteeringComponent.SteeringTarget = 0;

                        enabledNavigationTagRW.ValueRW = false;

                        ref var antiStuckconfig = ref AntistuckConfigReference.Config;

                        AntistuckUtils.ActivateAntistuck(
                            ref CommandBuffer,
                            entity,
                            in antiStuckconfig,
                            ref destinationComponent);

                        return;
                    }
                }

                if (targetSwapped)
                {
                    enabledNavigationTagRW.ValueRW = false;
                    navAgentSteeringComponent.SteeringTargetValue = default;
                    navAgentSteeringComponent.SteeringTarget = 0;

                    destinationComponent = destinationComponent.SwapBack();
                }
            }
        }
#endif
    }
}
