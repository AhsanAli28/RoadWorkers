using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Spirit604.DotsCity.Simulation.Road;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(TrafficProcessNodeGroup), OrderLast = true)]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct ProcessEnterParkingNodeSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<ProcessEnterCarParkingNodeTag, DestinationComponent>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var enteredParkingNodeJob = new EnteredParkingNodeJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                TrafficNodeCapacityLookup = SystemAPI.GetComponentLookup<TrafficNodeCapacityComponent>(false),
                NodeCapacityLookup = SystemAPI.GetComponentLookup<NodeCapacityComponent>(false),
                PedestrianNodeLinkedTrafficNodeLookup = SystemAPI.GetComponentLookup<NodeLinkedTrafficNodeComponent>(true),
            };

            enteredParkingNodeJob.Run();
        }

        [WithDisabled(typeof(PooledEventTag))]
        [WithAll(typeof(ProcessEnterCarParkingNodeTag))]
        [BurstCompile]
        public partial struct EnteredParkingNodeJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            public ComponentLookup<TrafficNodeCapacityComponent> TrafficNodeCapacityLookup;
            public ComponentLookup<NodeCapacityComponent> NodeCapacityLookup;

            [ReadOnly]
            public ComponentLookup<NodeLinkedTrafficNodeComponent> PedestrianNodeLinkedTrafficNodeLookup;

            void Execute(
                Entity pedestrianEntity,
                ref DestinationComponent destinationComponent,
                EnabledRefRW<PooledEventTag> pooledEventTagRW)
            {
                Entity targetEntity = destinationComponent.DestinationNode;

                bool nodeEntered = false;

                if (PedestrianNodeLinkedTrafficNodeLookup.HasComponent(targetEntity))
                {
                    NodeLinkedTrafficNodeComponent pedestrianNodeParkingComponent = PedestrianNodeLinkedTrafficNodeLookup[targetEntity];
                    var trafficNode = pedestrianNodeParkingComponent.LinkedEntity;

                    if (TrafficNodeCapacityLookup.HasComponent(trafficNode))
                    {
                        var trafficNodeCapacityComponent = TrafficNodeCapacityLookup[trafficNode];

                        if (trafficNodeCapacityComponent.UnlinkNodeAndReqDriver(ref CommandBuffer))
                        {
                            if (NodeCapacityLookup.HasComponent(trafficNodeCapacityComponent.PedestrianNodeEntity))
                            {
                                var nodeCapacityComponent = NodeCapacityLookup[trafficNodeCapacityComponent.PedestrianNodeEntity];
                                nodeCapacityComponent = nodeCapacityComponent.Enter();
                                NodeCapacityLookup[trafficNodeCapacityComponent.PedestrianNodeEntity] = nodeCapacityComponent;
                            }

                            TrafficNodeCapacityLookup[trafficNode] = trafficNodeCapacityComponent;
                            PoolEntityUtils.DestroyEntity(ref pooledEventTagRW);
                            nodeEntered = true;
                        }
                    }
                }

                if (!nodeEntered)
                {
                    CommandBuffer.RemoveComponent<ProcessEnterCarParkingNodeTag>(pedestrianEntity);

                    destinationComponent = destinationComponent.SwapBack();
                    CommandBuffer.SetComponentEnabled<HasTargetTag>(pedestrianEntity, true);
                }
            }
        }
    }
}