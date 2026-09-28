using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.TrafficArea
{
    [UpdateInGroup(typeof(CleanupGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficAreaCleanSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<TrafficAreaAlignedTag>()
                .WithAll<TrafficAreaLinked>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var cleanJob = new CleanJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                EnterQueueLookup = SystemAPI.GetBufferLookup<TrafficAreaEnterCarQueueElement>(false),
                ExitCarQueueLookup = SystemAPI.GetBufferLookup<TrafficAreaExitCarQueueElement>(false),
                TrafficAreaLookup = SystemAPI.GetComponentLookup<TrafficAreaComponent>(false),
            };

            cleanJob.Schedule();
        }

        [WithNone(typeof(TrafficAreaAlignedTag))]
        [BurstCompile]
        public partial struct CleanJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            public BufferLookup<TrafficAreaEnterCarQueueElement> EnterQueueLookup;
            public BufferLookup<TrafficAreaExitCarQueueElement> ExitCarQueueLookup;
            public ComponentLookup<TrafficAreaComponent> TrafficAreaLookup;

            void Execute(
                Entity entity,
                in TrafficAreaLinked trafficAreaLinked)
            {
                CommandBuffer.RemoveComponent<TrafficAreaLinked>(entity);

                var areaEntity = trafficAreaLinked.AreaEntity;

                if (!EnterQueueLookup.HasBuffer(areaEntity))
                    return;

                var enterQueue = EnterQueueLookup[areaEntity];

                for (int i = 0; i < enterQueue.Length; i++)
                {
                    if (enterQueue[i].TrafficEntity == entity)
                    {
                        enterQueue.RemoveAt(i);
                        CommandBuffer.SetComponentEnabled<TrafficAreaUpdateLockStateTag>(areaEntity, true);
                        break;
                    }
                }

                if (ExitCarQueueLookup.HasBuffer(areaEntity))
                {
                    var exitQueue = ExitCarQueueLookup[areaEntity];

                    for (int i = 0; i < exitQueue.Length; i++)
                    {
                        if (exitQueue[i].TrafficEntity == entity)
                        {
                            exitQueue.RemoveAt(i);

                            var trafficAreaComponent = TrafficAreaLookup[areaEntity];

                            if (enterQueue.Length > 0 && trafficAreaComponent.SkipOrderSupported)
                            {
                                trafficAreaComponent.SkippedOrderCount++;
                            }

                            TrafficAreaLookup[areaEntity] = trafficAreaComponent;

                            break;
                        }
                    }
                }
            }
        }
    }
}
