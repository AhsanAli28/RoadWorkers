using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.TrafficPublic
{
    [UpdateInGroup(typeof(CleanupGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficPublicStationCleanupSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<CarCapacityComponent>()
                .WithAll<TrafficPublicIdleComponent>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var trafficPublicCleanupJob = new TrafficPublicCleanupJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                NodeProcessWaitQueueLookup = SystemAPI.GetComponentLookup<NodeProcessWaitQueueTag>(false)
            };

            trafficPublicCleanupJob.Schedule();
        }

        [WithNone(typeof(CarCapacityComponent))]
        [BurstCompile]
        public partial struct TrafficPublicCleanupJob : IJobEntity
        {
            public ComponentLookup<NodeProcessWaitQueueTag> NodeProcessWaitQueueLookup;
            internal EntityCommandBuffer CommandBuffer;

            void Execute(
                Entity entity,
                in TrafficPublicIdleComponent trafficPublicIdleComponent)
            {
                var nodeEntity = trafficPublicIdleComponent.NodeEntity;

                if (NodeProcessWaitQueueLookup.HasComponent(nodeEntity) && NodeProcessWaitQueueLookup.IsComponentEnabled(nodeEntity))
                {
                    NodeProcessWaitQueueLookup.SetComponentEnabled(nodeEntity, false);
                }

                CommandBuffer.RemoveComponent<TrafficPublicIdleComponent>(entity);
            }
        }
    }
}