using Spirit604.DotsCity.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(CleanupGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct BenchCleanSystem : ISystem
    {
        private EntityQuery updateGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateGroup = SystemAPI.QueryBuilder()
                .WithNone<PoolableTag>()
                .WithAll<SeatSlotLinkedComponent>()
                .Build();

            state.RequireForUpdate(updateGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var benchCleanJob = new BenchCleanJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                BenchSeatLookup = SystemAPI.GetBufferLookup<BenchSeatElement>(false),
                NodeCapacityLookup = SystemAPI.GetComponentLookup<NodeCapacityComponent>(false),
                NodeSeatSettingsLookup = SystemAPI.GetComponentLookup<NodeSeatSettingsComponent>(true),
            };

            benchCleanJob.Schedule();
        }

        [WithNone(typeof(PoolableTag))]
        [WithAll(typeof(SeatSlotLinkedComponent))]
        [BurstCompile]
        public partial struct BenchCleanJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            public BufferLookup<BenchSeatElement> BenchSeatLookup;

            public ComponentLookup<NodeCapacityComponent> NodeCapacityLookup;

            [ReadOnly]
            public ComponentLookup<NodeSeatSettingsComponent> NodeSeatSettingsLookup;

            void Execute(
                Entity entity,
                ref SeatSlotLinkedComponent seatSlotLinkedComponent)
            {
                CommandBuffer.RemoveComponent<SeatSlotLinkedComponent>(entity);

                var seatEntity = seatSlotLinkedComponent.SeatEntity;

                // Node unloaded by road streaming
                if (!NodeCapacityLookup.HasComponent(seatEntity))
                    return;

                var nodeCapacityComponent = NodeCapacityLookup[seatEntity];
                var nodeSeatSettingsComponent = NodeSeatSettingsLookup[seatEntity];

                nodeCapacityComponent = nodeCapacityComponent.Leave();

                var slots = BenchSeatLookup[seatEntity];
                BenchUtils.GetOut(entity, ref slots);

                NodeCapacityLookup[seatEntity] = nodeCapacityComponent;
                CommandBuffer.SetComponent(seatEntity, nodeSeatSettingsComponent);
            }
        }
    }
}