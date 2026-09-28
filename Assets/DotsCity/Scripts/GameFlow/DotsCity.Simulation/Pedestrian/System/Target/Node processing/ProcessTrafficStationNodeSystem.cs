using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Spirit604.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct ProcessTrafficStationNodeSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<ProcessEnterTrafficStationNodeTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var enterTrafficStationJob = new EnterTrafficStationJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                WaitQueueLookup = SystemAPI.GetBufferLookup<WaitQueueElement>(false),
                NodeCapacityLookup = SystemAPI.GetComponentLookup<NodeCapacityComponent>(false),
                NodeIdleLookup = SystemAPI.GetComponentLookup<NodeIdleComponent>(true),
                Timestamp = (float)SystemAPI.Time.ElapsedTime,
            };

            enterTrafficStationJob.Run();
        }

        [WithAll(typeof(ProcessEnterTrafficStationNodeTag))]
        [BurstCompile]
        public partial struct EnterTrafficStationJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            public BufferLookup<WaitQueueElement> WaitQueueLookup;

            public ComponentLookup<NodeCapacityComponent> NodeCapacityLookup;

            [ReadOnly]
            public ComponentLookup<NodeIdleComponent> NodeIdleLookup;

            [ReadOnly]
            public float Timestamp;

            void Execute(
                Entity pedestrianEntity,
                ref DestinationComponent destinationComponent,
                ref NextStateComponent nextStateComponent)
            {
                CommandBuffer.RemoveComponent<ProcessEnterTrafficStationNodeTag>(pedestrianEntity);

                Entity reachedNode = destinationComponent.DestinationNode;

                // For unloading streaming case
                if (!NodeCapacityLookup.HasComponent(reachedNode))
                    return;

                var nodeCapacityComponent = NodeCapacityLookup[reachedNode];

                if (!nodeCapacityComponent.IsAvailable() || !nextStateComponent.TryToSetNextState(ActionState.Idle, ref destinationComponent) || !WaitQueueLookup.HasBuffer(reachedNode))
                {
                    CommandBuffer.SetComponentEnabled<ProcessEnterDefaultNodeTag>(pedestrianEntity, true);
                    return;
                }

                bool changed = false;

                if (nodeCapacityComponent.MaxAvailableCount > 0)
                {
                    nodeCapacityComponent.Enter();
                    changed = true;
                }

                var waitQueue = WaitQueueLookup[reachedNode];

                waitQueue.Add(new WaitQueueElement()
                {
                    PedestrianEntity = pedestrianEntity
                });

                var nodeIdle = NodeIdleLookup[reachedNode];
                var rndGen = UnityMathematicsExtension.GetRandomGen(Timestamp, pedestrianEntity.Index);
                var idleTime = rndGen.NextFloat(nodeIdle.MinIdleTime, nodeIdle.MaxIdleTime);

                CommandBuffer.SetComponentEnabled<IdleTag>(pedestrianEntity, true);

                CommandBuffer.AddComponent(pedestrianEntity, new IdleTimeComponent()
                {
                    IsInitialized = true,
                    DisableIdleTimestamp = Timestamp + idleTime
                });

                CommandBuffer.AddComponent(pedestrianEntity, new WaitQueueLinkedComponent()
                {
                    NodeEntity = reachedNode
                });

                if (changed)
                {
                    NodeCapacityLookup[reachedNode] = nodeCapacityComponent;
                }
            }
        }
    }
}