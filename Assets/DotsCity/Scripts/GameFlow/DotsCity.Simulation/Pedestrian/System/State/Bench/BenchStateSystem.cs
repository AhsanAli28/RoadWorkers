using Spirit604.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian.State
{
    [UpdateInGroup(typeof(StructuralInitGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct BenchStateSystem : ISystem
    {
        private EntityQuery updateGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateGroup = SystemAPI.QueryBuilder()
                .WithAll<SeatAchievedTargetTag>()
                .Build();

            state.RequireForUpdate(updateGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var benchStateJob = new BenchStateJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                BenchSeatLookup = SystemAPI.GetBufferLookup<BenchSeatElement>(false),
                NodeCapacityLookup = SystemAPI.GetComponentLookup<NodeCapacityComponent>(false),
                NodeSeatSettingsLookup = SystemAPI.GetComponentLookup<NodeSeatSettingsComponent>(true),
                BenchConfigReference = SystemAPI.GetSingleton<BenchConfigReference>(),
                CurrentTime = (float)SystemAPI.Time.ElapsedTime
            };

            benchStateJob.Run();
        }

        [WithAll(typeof(SeatAchievedTargetTag))]
        [BurstCompile]
        public partial struct BenchStateJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            public BufferLookup<BenchSeatElement> BenchSeatLookup;

            public ComponentLookup<NodeCapacityComponent> NodeCapacityLookup;

            [ReadOnly]
            public ComponentLookup<NodeSeatSettingsComponent> NodeSeatSettingsLookup;

            [ReadOnly]
            public BenchConfigReference BenchConfigReference;

            [ReadOnly]
            public float CurrentTime;

            void Execute(
                Entity pedestrianEntity,
                ref NextStateComponent nextStateComponent,
                ref SeatSlotLinkedComponent seatSlotLinkedComponent,
                ref AnimationStateComponent animationStateComponent,
                in DestinationComponent destinationComponent,
                in StateComponent stateComponent)
            {
                switch (seatSlotLinkedComponent.SitState)
                {
                    case SitState.MovingToEnter:
                        {
                            seatSlotLinkedComponent.SitState = SitState.SittingIn;
                            CommandBuffer.SetComponentEnabled<BenchCustomMovementTag>(pedestrianEntity, true);
                            CommandBuffer.SetComponentEnabled<SeatAchievedTargetTag>(pedestrianEntity, false);
                            break;
                        }
                    case SitState.SittingIn:
                        {
                            seatSlotLinkedComponent.SitState = SitState.Sitting;
                            var rnd = UnityMathematicsExtension.GetRandomGen(CurrentTime, pedestrianEntity.Index);
                            seatSlotLinkedComponent.DeactivateTimestamp = CurrentTime + rnd.NextFloat(BenchConfigReference.Config.Value.MinIdleTime, BenchConfigReference.Config.Value.MaxIdleTime);

                            AnimatorStateExtension.ChangeAnimatorState(ref CommandBuffer, pedestrianEntity, ref animationStateComponent, AnimationState.SittingIdle, false);

                            CommandBuffer.SetComponentEnabled<BenchCustomMovementTag>(pedestrianEntity, true);

                            return;
                        }
                    case SitState.Sitting:
                        {
                            if (CurrentTime < seatSlotLinkedComponent.DeactivateTimestamp && stateComponent.IsActionState(ActionState.Sitting))
                            {
                                return;
                            }

                            seatSlotLinkedComponent.SitState = SitState.SittingOut;

                            AnimatorStateExtension.ChangeAnimatorState(ref CommandBuffer, pedestrianEntity, ref animationStateComponent, AnimationState.SitToStand);

                            CommandBuffer.SetComponentEnabled<BenchCustomMovementTag>(pedestrianEntity, true);
                            CommandBuffer.SetComponentEnabled<SeatAchievedTargetTag>(pedestrianEntity, false);

                            return;
                        }
                    case SitState.SittingOut:
                        {
                            seatSlotLinkedComponent.SitState = SitState.IdleAfterAchievedExit;
                            seatSlotLinkedComponent.DeactivateTimestamp = CurrentTime + BenchConfigReference.Config.Value.ExitIdleDuration;
                            CommandBuffer.SetComponentEnabled<BenchWaitForExitTag>(pedestrianEntity, true);
                            return;
                        }
                    case SitState.IdleAfterAchievedExit:
                        {
                            if (CurrentTime < seatSlotLinkedComponent.DeactivateTimestamp)
                            {
                                return;
                            }

                            if (!seatSlotLinkedComponent.Exited)
                            {
                                return;
                            }

                            bool leaveFromSitState = stateComponent.IsActionState(ActionState.Sitting);

                            nextStateComponent.TryToSetNextState(ActionState.MovingToNextTargetPoint);

                            var seatEntity = seatSlotLinkedComponent.SeatEntity;

                            CommandBuffer.RemoveComponent<SeatSlotLinkedComponent>(pedestrianEntity);

                            // Unloaded by streaming
                            if (!NodeCapacityLookup.HasComponent(seatEntity))
                            {
                                return;
                            }

                            // Leave
                            var nodeCapacityComponent = NodeCapacityLookup[seatEntity];

                            nodeCapacityComponent = nodeCapacityComponent.Leave();

                            var slots = BenchSeatLookup[seatEntity];
                            BenchUtils.GetOut(pedestrianEntity, ref slots);

                            NodeCapacityLookup[seatEntity] = nodeCapacityComponent;

                            BenchStateExtension.RemoveBenchStateComponents(ref CommandBuffer, pedestrianEntity);
                            AnimatorStateExtension.RemoveCustomAnimator(ref CommandBuffer, pedestrianEntity, true);

                            if (leaveFromSitState)
                            {
                                var targetPosition = destinationComponent.PreviousDestination;
                                var previousTargetPosition = destinationComponent.Value;

                                var newDestinationComponent = new DestinationComponent()
                                {
                                    PreviousDestination = previousTargetPosition,
                                    Value = targetPosition,
                                    DestinationNode = destinationComponent.PreviuosDestinationNode,
                                    PreviuosDestinationNode = destinationComponent.DestinationNode
                                };

                                CommandBuffer.SetComponent(pedestrianEntity, newDestinationComponent);
                            }

                            CommandBuffer.SetComponentEnabled<HasTargetTag>(pedestrianEntity, true);

                            break;
                        }
                }
            }
        }
    }
}