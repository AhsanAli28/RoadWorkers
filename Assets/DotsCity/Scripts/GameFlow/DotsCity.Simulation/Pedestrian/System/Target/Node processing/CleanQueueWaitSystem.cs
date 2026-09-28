using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(CleanupGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct CleanQueueWaitSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<IdleTag>()
                .WithAll<WaitQueueLinkedComponent>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var queueCleanJob = new QueueCleanJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                WaitQueueBufferLookup = SystemAPI.GetBufferLookup<WaitQueueElement>(false),
                PedestrianWaitQueueLookup = SystemAPI.GetComponentLookup<WaitQueueComponent>(false),
                NodeCapacityLookup = SystemAPI.GetComponentLookup<NodeCapacityComponent>(false),
            };

            queueCleanJob.Schedule();
        }

        [WithNone(typeof(IdleTag))]
        [BurstCompile]
        public partial struct QueueCleanJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            public BufferLookup<WaitQueueElement> WaitQueueBufferLookup;
            public ComponentLookup<WaitQueueComponent> PedestrianWaitQueueLookup;
            public ComponentLookup<NodeCapacityComponent> NodeCapacityLookup;

            void Execute(
                Entity entity,
                in WaitQueueLinkedComponent pedestrianWaitQueueLinkedTag)
            {
                CommandBuffer.RemoveComponent<WaitQueueLinkedComponent>(entity);

                var nodeEntity = pedestrianWaitQueueLinkedTag.NodeEntity;

                if (!WaitQueueBufferLookup.HasBuffer(nodeEntity))
                {
                    return;
                }

                var queueBuffer = WaitQueueBufferLookup[nodeEntity];

                for (int i = 0; i < queueBuffer.Length; i++)
                {
                    if (queueBuffer[i].PedestrianEntity.Index != entity.Index)
                    {
                        continue;
                    }

                    if (queueBuffer[i].Activated)
                    {
                        var queueDataElement = PedestrianWaitQueueLookup[nodeEntity];

                        if (queueDataElement.ActivatedCount > 0)
                        {
                            queueDataElement.ActivatedCount--;
                            PedestrianWaitQueueLookup[nodeEntity] = queueDataElement;
                        }
                    }

                    queueBuffer.RemoveAt(i);

                    if (NodeCapacityLookup.HasComponent(nodeEntity))
                    {
                        var nodeCapacity = NodeCapacityLookup[nodeEntity];

                        if (nodeCapacity.MaxAvailableCount > 0)
                        {
                            nodeCapacity.Leave();
                            NodeCapacityLookup[nodeEntity] = nodeCapacity;
                        }
                    }

                    break;
                }
            }
        }
    }
}