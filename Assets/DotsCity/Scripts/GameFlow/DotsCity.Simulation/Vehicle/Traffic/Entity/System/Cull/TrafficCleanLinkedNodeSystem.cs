using Spirit604.DotsCity.Simulation.Road;
using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(CleanupGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficCleanLinkedNodeSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<TrafficTag>()
                .WithAll<TrafficNodeLinkedComponent>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var cleanLinkedNodeJob = new CleanLinkedNodeJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                TrafficNodeCapacityLookup = SystemAPI.GetComponentLookup<TrafficNodeCapacityComponent>(isReadOnly: false)
            };

            cleanLinkedNodeJob.Run();
        }

        [WithNone(typeof(TrafficTag))]
        [BurstCompile]
        public partial struct CleanLinkedNodeJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;
            public ComponentLookup<TrafficNodeCapacityComponent> TrafficNodeCapacityLookup;

            void Execute(
                Entity entity,
                TrafficNodeLinkedComponent carParkingLinkedComponent)
            {
                var trafficNodeEntity = carParkingLinkedComponent.LinkedPlace;

                if (TrafficNodeCapacityLookup.HasComponent(trafficNodeEntity))
                {
                    var trafficNodeCapacity = TrafficNodeCapacityLookup[trafficNodeEntity];
                    trafficNodeCapacity.UnlinkNode();
                    TrafficNodeCapacityLookup[trafficNodeEntity] = trafficNodeCapacity;
                }

                CommandBuffer.RemoveComponent<TrafficNodeLinkedComponent>(entity);
            }
        }
    }
}