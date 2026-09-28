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
    public partial struct ProcessEnteredSeatNodeSystem : ISystem
    {
        private EntityQuery updateGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateGroup = SystemAPI.QueryBuilder()
                .WithNone<SeatSlotLinkedComponent>()
                .WithAll<ProcessEnterSeatNodeTag>()
                .Build();

            state.RequireForUpdate(updateGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var enteredSitNodeJob = new EnteredSitNodeJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                BenchSeatLookup = SystemAPI.GetBufferLookup<BenchSeatElement>(false),
                NodeCapacityLookup = SystemAPI.GetComponentLookup<NodeCapacityComponent>(false),
                NodeSeatSettingsLookup = SystemAPI.GetComponentLookup<NodeSeatSettingsComponent>(true),
                BenchConfigReference = SystemAPI.GetSingleton<BenchConfigReference>(),
            };

            enteredSitNodeJob.Run();
        }

        [WithNone(typeof(SeatSlotLinkedComponent))]
        [WithAll(typeof(ProcessEnterSeatNodeTag))]
        [BurstCompile]
        public partial struct EnteredSitNodeJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            public BufferLookup<BenchSeatElement> BenchSeatLookup;

            public ComponentLookup<NodeCapacityComponent> NodeCapacityLookup;

            [ReadOnly]
            public ComponentLookup<NodeSeatSettingsComponent> NodeSeatSettingsLookup;

            [ReadOnly]
            public BenchConfigReference BenchConfigReference;

            void Execute(
                Entity entity,
                ref DestinationComponent destinationComponent,
                ref NextStateComponent nextStateComponent)
            {
                Entity targetEntity = destinationComponent.DestinationNode;

                bool isAvailable = NodeSeatSettingsLookup.HasComponent(targetEntity);

                var nodeCapacityComponent = NodeCapacityLookup[targetEntity];

                if (isAvailable)
                {
                    isAvailable = nodeCapacityComponent.IsAvailable() && nextStateComponent.CanSwitchState(ActionState.Sitting);
                }

                if (isAvailable)
                {
                    nodeCapacityComponent = nodeCapacityComponent.Enter();
                    NodeCapacityLookup[targetEntity] = nodeCapacityComponent;

                    nextStateComponent.TryToSetNextState(ActionState.MovingToNextTargetPoint);

                    NodeSeatSettingsComponent nodeSeatSettingsComponent = NodeSeatSettingsLookup[targetEntity];

                    var benchSlots = BenchSeatLookup[targetEntity];
                    int seatIndex;

                    BenchUtils.EnterAnySeat(entity, ref benchSlots, out seatIndex);

                    var enterSeatPosition = BenchUtils.GetEnterPosition(seatIndex, nodeSeatSettingsComponent);
                    destinationComponent.Value = enterSeatPosition;
                    destinationComponent.SetCustomAchieveDistance(BenchConfigReference.Config.Value.EntryDistance);

                    var seatSlotLinkedComponent = new SeatSlotLinkedComponent()
                    {
                        SeatEntity = targetEntity,
                        SeatIndex = seatIndex,
                        EnterSeatPosition = enterSeatPosition
                    };

                    CommandBuffer.AddComponent(entity, seatSlotLinkedComponent);
                }
                else
                {
                    destinationComponent = destinationComponent.SwapBack();
                }

                CommandBuffer.SetComponentEnabled<HasTargetTag>(entity, true);
                CommandBuffer.RemoveComponent<ProcessEnterSeatNodeTag>(entity);
            }
        }
    }
}