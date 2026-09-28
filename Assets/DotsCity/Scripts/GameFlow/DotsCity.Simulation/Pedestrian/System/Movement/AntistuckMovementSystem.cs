using Spirit604.DotsCity.Simulation.Npc.Navigation;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(PedestrianFixedSimulationGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct AntistuckMovementSystem : ISystem
    {
        private const float MaxMovementTime = 4f;

        private EntityQuery updateGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateGroup = SystemAPI.QueryBuilder()
                .WithDisabled<AntistuckActivateTag, AntistuckDeactivateTag>()
                .WithPresentRW<UpdateNavTargetTag>()
                .WithAllRW<LocalTransform, NavAgentComponent>()
                .WithAllRW<NextStateComponent, AntistuckDestinationComponent>()
                .WithAll<CustomMovementTag, StateComponent, DestinationComponent, PedestrianMovementSettings>()
                .Build();

            state.RequireForUpdate(updateGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var antistuckMovementJob = new AntistuckMovementJob()
            {
                PersistNavigationLookup = SystemAPI.GetComponentLookup<PersistNavigationTag>(true),
                AchievedNavTargetLookup = SystemAPI.GetComponentLookup<AchievedNavTargetTag>(false),
                AntistuckConfigReference = SystemAPI.GetSingleton<AntistuckConfigReference>(),
                DeltaTime = SystemAPI.Time.DeltaTime,
                Timestamp = (float)SystemAPI.Time.ElapsedTime,
            };

            antistuckMovementJob.Schedule(updateGroup);
        }

        [WithDisabled(typeof(AntistuckActivateTag), typeof(AntistuckDeactivateTag))]
        [WithAll(typeof(CustomMovementTag))]
        [BurstCompile]
        private partial struct AntistuckMovementJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<PersistNavigationTag> PersistNavigationLookup;

            public ComponentLookup<AchievedNavTargetTag> AchievedNavTargetLookup;

            [ReadOnly]
            public AntistuckConfigReference AntistuckConfigReference;

            [ReadOnly]
            public float DeltaTime;

            [ReadOnly]
            public float Timestamp;

            void Execute(
                Entity entity,
                ref LocalTransform transform,
                ref NavAgentComponent navAgentComponent,
                ref NextStateComponent nextStateComponent,
                ref AntistuckDestinationComponent antiStuckTarget,
                EnabledRefRW<UpdateNavTargetTag> updateNavTargetTagRW,
                EnabledRefRW<AntistuckDeactivateTag> antistuckDeactivateTagRW,
                in StateComponent stateComponent,
                in DestinationComponent destinationComponent,
                in PedestrianMovementSettings movementSettings)
            {
                bool outOfTime = Timestamp - antiStuckTarget.ActivateTimestamp >= MaxMovementTime;

                var targetRotation = antiStuckTarget.DstRotation;
                var currentRotation = transform.Rotation;

                float dot = math.dot(antiStuckTarget.DstDirection, math.mul(currentRotation, math.forward()));

                if (dot < AntistuckConfigReference.Config.Value.TargetDirectionDot)
                {
                    var rotationSpeed = movementSettings.RotationSpeed * DeltaTime;
                    var newRotation = math.slerp(currentRotation, targetRotation, rotationSpeed);

                    transform.Rotation = newRotation;
                }
                else
                {
                    if (!antiStuckTarget.RotationComplete)
                    {
                        antiStuckTarget.RotationComplete = true;

                        if (stateComponent.ActionState != antiStuckTarget.PreviousActionState)
                        {
                            nextStateComponent.TryToSetNextState(antiStuckTarget.PreviousActionState);
                        }
                    }

                    float distance = math.distancesq(transform.Position, antiStuckTarget.Destination);
                    var targetAchieved = distance <= AntistuckConfigReference.Config.Value.AchieveDistanceSQ;

                    if (!targetAchieved && !outOfTime)
                    {
                        transform.Position += antiStuckTarget.DstDirection * movementSettings.CurrentMovementSpeed * DeltaTime;
                    }
                    else
                    {
                        if (antiStuckTarget.PreviousFlags != ActionState.Default)
                        {
                            nextStateComponent.TryToSetNextState(antiStuckTarget.PreviousFlags);
                        }

                        antistuckDeactivateTagRW.ValueRW = true;

                        bool updateTarget = PersistNavigationLookup.HasComponent(entity);

                        if (updateTarget)
                        {
                            navAgentComponent.PathEndPosition = destinationComponent.Value;
                            updateNavTargetTagRW.ValueRW = true;
                        }

                        if (AchievedNavTargetLookup.IsComponentEnabled(entity))
                        {
                            AchievedNavTargetLookup.SetComponentEnabled(entity, false);
                        }
                    }
                }
            }
        }
    }
}
