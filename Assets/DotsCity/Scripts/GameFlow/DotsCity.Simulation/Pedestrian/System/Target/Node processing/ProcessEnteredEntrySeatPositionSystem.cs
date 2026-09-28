using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(StructuralSystemGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct ProcessEnteredEntrySeatPositionSystem : ISystem
    {
        private EntityQuery updateGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateGroup = SystemAPI.QueryBuilder()
                .WithAll<ProcessEnterSeatNodeTag, SeatSlotLinkedComponent>()
                .Build();

            state.RequireForUpdate(updateGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var enteredEntrySitNodeJob = new EnteredEntrySitNodeJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                NodeSeatSettingsLookup = SystemAPI.GetComponentLookup<NodeSeatSettingsComponent>(true),
            };

            enteredEntrySitNodeJob.Run();
        }

        [WithAll(typeof(ProcessEnterSeatNodeTag))]
        [BurstCompile]
        public partial struct EnteredEntrySitNodeJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public ComponentLookup<NodeSeatSettingsComponent> NodeSeatSettingsLookup;

            void Execute(
                Entity entity,
                ref DestinationComponent destinationComponent,
                ref AnimationStateComponent animationStateComponent,
                ref NextStateComponent nextStateComponent,
                ref SeatSlotLinkedComponent linkedSeat)
            {
                Entity targetEntity = destinationComponent.DestinationNode;

                if (!NodeSeatSettingsLookup.HasComponent(targetEntity))
                {
                    destinationComponent = destinationComponent.SwapBack();
                    CommandBuffer.RemoveComponent<ProcessEnterSeatNodeTag>(entity);
                    return;
                }

                NodeSeatSettingsComponent nodeSeatSettingsComponent = NodeSeatSettingsLookup[targetEntity];

                if (linkedSeat.SitState == SitState.MovingToEnter)
                {
                    if (!nextStateComponent.TryToSetNextState(ActionState.Sitting, ref destinationComponent))
                    {
                        CommandBuffer.RemoveComponent<ProcessEnterSeatNodeTag>(entity);
                        return;
                    }

                    var seatIndex = linkedSeat.SeatIndex;

                    var seatPosition = BenchUtils.GetSeatPosition(seatIndex, nodeSeatSettingsComponent);
                    destinationComponent.Value = seatPosition;

                    linkedSeat.SeatPosition = seatPosition;
                    linkedSeat.SeatRotation = nodeSeatSettingsComponent.InitialRotation;

                    CommandBuffer.SetComponent(entity, linkedSeat);

                    BenchStateExtension.AddBenchStateComponents(ref CommandBuffer, entity);
                    AnimatorStateExtension.AddCustomAnimatorState(ref CommandBuffer, entity, ref animationStateComponent, AnimationState.StandToSit, true);

                    CommandBuffer.RemoveComponent<ProcessEnterSeatNodeTag>(entity);
                }
            }
        }
    }
}