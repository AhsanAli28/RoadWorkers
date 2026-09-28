using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.TrafficArea
{
    [UpdateInGroup(typeof(TrafficAreaSimulationGroup))]
    [BurstCompile]
    public partial struct TrafficAreaObserveDisableSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<TrafficAreaProcessingEnterQueueTag, TrafficAreaProcessingExitQueueTag>()
                .WithAll<TrafficAreaCarObserverEnabledTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var disableObserveJob = new DisableObserveJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged)
            };

            disableObserveJob.Schedule();
        }

        [WithNone(typeof(TrafficAreaProcessingEnterQueueTag), typeof(TrafficAreaProcessingExitQueueTag))]
        [WithAll(typeof(TrafficAreaCarObserverEnabledTag))]
        [BurstCompile]
        public partial struct DisableObserveJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            void Execute(
                Entity entity,
                ref TrafficAreaComponent trafficAreaComponent)
            {
                trafficAreaComponent.ActiveCurrentCarCount = 0;
                trafficAreaComponent.ExitCarCount = 0;

                CommandBuffer.SetComponentEnabled<TrafficAreaCarObserverEnabledTag>(entity, false);
            }
        }
    }
}