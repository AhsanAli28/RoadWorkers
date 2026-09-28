using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Car.Sound;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.Sound;
using Spirit604.DotsCity.Simulation.TrafficPublic;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateAfter(typeof(TrafficEnteringLinkedNodeEventSystem))]
    [UpdateInGroup(typeof(TrafficProcessNodeGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficProcessEntereredTriggerTrafficNodeSystem : ISystem
    {
        private EntityQuery carQuery;

        void ISystem.OnCreate(ref SystemState state)
        {
            carQuery = SystemAPI.QueryBuilder()
                .WithAllRW<TrafficDestinationComponent, TrafficStateComponent>()
                .WithPresentRW<TrafficIdleTag>()
                .WithAll<TrafficTag, TrafficEnteredTriggerNodeTag, CarModelComponent, LocalTransform>()
                .Build();

            state.RequireForUpdate(carQuery);
            state.RequireForUpdate<PedestrianEntityPrefabContainer>();
            state.RequireForUpdate<PedestrianSettingsReference>();
            state.RequireForUpdate<CarSharedDataConfigReference>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var enteredTrafficNodeJob = new EnteredTrafficNodeJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                SoundEventQueue = SystemAPI.GetSingleton<SoundEventPlaybackSystem.Singleton>(),

                TrafficNodeCapacityLookup = SystemAPI.GetComponentLookup<TrafficNodeCapacityComponent>(isReadOnly: false),
                NodeCapacityLookup = SystemAPI.GetComponentLookup<NodeCapacityComponent>(isReadOnly: false),

                ConnectedPedestrianNodeElementLookup = SystemAPI.GetBufferLookup<ConnectedPedestrianNodeElement>(true),

                WorldTransformLookup = SystemAPI.GetComponentLookup<LocalToWorld>(isReadOnly: true),

                TrafficNodeSettingsLookup = SystemAPI.GetComponentLookup<TrafficNodeSettingsComponent>(isReadOnly: true),
                PedestrianNodeProcessWaitQueueLookup = SystemAPI.GetComponentLookup<NodeProcessWaitQueueTag>(isReadOnly: true),
                NodeConnectionDataLookup = SystemAPI.GetBufferLookup<NodeConnectionDataElement>(isReadOnly: true),
                TrafficPublicIdleSettingsLookup = SystemAPI.GetComponentLookup<TrafficPublicIdleSettingsComponent>(isReadOnly: true),
                CarCustomEnginePitchLookup = SystemAPI.GetComponentLookup<CarCustomEnginePitchTag>(isReadOnly: true),

                PrefabContainer = SystemAPI.GetSingletonBuffer<PedestrianEntityPrefabContainer>(true),
                PedestrianSettingsRef = SystemAPI.GetSingleton<PedestrianSettingsReference>(),

                TrafficSharedDataConfigReference = SystemAPI.GetSingleton<CarSharedDataConfigReference>(),
                TrafficRoadConfigReference = SystemAPI.GetSingleton<TrafficRoadConfigReference>(),
                TrafficParkingConfigReference = SystemAPI.GetSingleton<TrafficParkingConfigReference>(),
                CurrentTime = (float)SystemAPI.Time.ElapsedTime,
            };

            enteredTrafficNodeJob.Run(carQuery);
        }

        [WithAll(typeof(TrafficTag), typeof(TrafficEnteredTriggerNodeTag))]
        [BurstCompile]
        private partial struct EnteredTrafficNodeJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            public SoundEventPlaybackSystem.Singleton SoundEventQueue;

            public ComponentLookup<TrafficNodeCapacityComponent> TrafficNodeCapacityLookup;

            public ComponentLookup<NodeCapacityComponent> NodeCapacityLookup;

            [ReadOnly]
            public BufferLookup<ConnectedPedestrianNodeElement> ConnectedPedestrianNodeElementLookup;

            [ReadOnly]
            public ComponentLookup<LocalToWorld> WorldTransformLookup;

            [ReadOnly]
            public ComponentLookup<TrafficNodeSettingsComponent> TrafficNodeSettingsLookup;

            [ReadOnly]
            public ComponentLookup<NodeProcessWaitQueueTag> PedestrianNodeProcessWaitQueueLookup;

            [ReadOnly]
            public BufferLookup<NodeConnectionDataElement> NodeConnectionDataLookup;

            [ReadOnly]
            public ComponentLookup<TrafficPublicIdleSettingsComponent> TrafficPublicIdleSettingsLookup;

            [ReadOnly]
            public ComponentLookup<CarCustomEnginePitchTag> CarCustomEnginePitchLookup;

            [ReadOnly]
            public DynamicBuffer<PedestrianEntityPrefabContainer> PrefabContainer;

            [ReadOnly]
            public PedestrianSettingsReference PedestrianSettingsRef;

            [ReadOnly]
            public CarSharedDataConfigReference TrafficSharedDataConfigReference;

            [ReadOnly]
            public TrafficRoadConfigReference TrafficRoadConfigReference;

            [ReadOnly]
            public TrafficParkingConfigReference TrafficParkingConfigReference;

            [ReadOnly]
            public float CurrentTime;

            private void Execute(
                Entity entity,
                ref TrafficDestinationComponent destinationComponent,
                ref TrafficStateComponent trafficStateComponent,
                EnabledRefRW<TrafficEnteredTriggerNodeTag> trafficEnteredTriggerNodeTag,
                EnabledRefRW<TrafficIdleTag> trafficIdleTagRW,
                in CarModelComponent carModelComponent,
                in LocalTransform transform)
            {
                var enteredNodeEntity = destinationComponent.CurrentNode;

                if (!TrafficNodeSettingsLookup.HasComponent(enteredNodeEntity))
                    return;

                var settingsComponent = TrafficNodeSettingsLookup[enteredNodeEntity];

                var linked = false;
                var canLink = true;

                if (settingsComponent.TrafficNodeType == TrafficNodeType.TrafficPublicStop)
                {
                    if (!TrafficPublicIdleSettingsLookup.HasComponent(entity))
                    {
                        canLink = false;
                    }
                }

                if (canLink)
                {
                    linked = TrafficNodeCapacityUtils.TryToLinkNode(
                        entity,
                        enteredNodeEntity,
                        ref CommandBuffer,
                        ref TrafficNodeCapacityLookup,
                        in TrafficNodeSettingsLookup,
                        in TrafficRoadConfigReference);
                }

                var trafficNodeCapacity = TrafficNodeCapacityLookup[enteredNodeEntity];

                trafficEnteredTriggerNodeTag.ValueRW = false;

                if (!linked)
                    return;

                if (settingsComponent.TrafficNodeType == TrafficNodeType.TrafficPublicStop)
                {
                    TrafficStateExtension.AddIdleState<TrafficPublicIdleComponent>(ref CommandBuffer, entity, ref trafficStateComponent, ref trafficIdleTagRW, TrafficIdleState.PublicTransportStop);

                    if (ConnectedPedestrianNodeElementLookup.HasBuffer(enteredNodeEntity))
                    {
                        var buffer = ConnectedPedestrianNodeElementLookup[enteredNodeEntity];

                        for (int i = 0; i < buffer.Length; i++)
                        {
                            var pedestrianNodeEntity = buffer[i].PedestrianNodeEntity;

                            if (pedestrianNodeEntity == Entity.Null) continue;

                            if (!PedestrianNodeProcessWaitQueueLookup.IsComponentEnabled(pedestrianNodeEntity))
                            {
                                CommandBuffer.SetComponentEnabled<NodeProcessWaitQueueTag>(pedestrianNodeEntity, true);
                            }
                        }
                    }
                }

                if (settingsComponent.TrafficNodeType == TrafficNodeType.Parking)
                {
                    ref var soundConfig = ref TrafficSharedDataConfigReference.Config;

                    InteractCarUtils.ExitCar(ref CommandBuffer, ref soundConfig, ref SoundEventQueue, transform.Position, carModelComponent.Value);

                    if (CarCustomEnginePitchLookup.HasComponent(entity))
                    {
                        CommandBuffer.RemoveComponent<CarCustomEnginePitchTag>(entity);
                    }

                    CommandBuffer.RemoveComponent<HasDriverTag>(entity);
                    CommandBuffer.RemoveComponent<CarEngineStartedTag>(entity);

                    var parkingIdleType = TrafficParkingConfigReference.Config.Value.ParkingIdleType;

                    switch (parkingIdleType)
                    {
                        case ParkingIdleType.WaitForPedestrian:
                            {
                                if (trafficNodeCapacity.PedestrianNodeEntity != Entity.Null)
                                {
                                    var pedestrianNodeParkingComponent = NodeCapacityLookup[trafficNodeCapacity.PedestrianNodeEntity];
                                    pedestrianNodeParkingComponent = pedestrianNodeParkingComponent.Leave();
                                    NodeCapacityLookup[trafficNodeCapacity.PedestrianNodeEntity] = pedestrianNodeParkingComponent;

                                    var nodeConnectionData = NodeConnectionDataLookup[trafficNodeCapacity.PedestrianNodeEntity];

                                    var rndGen = UnityMathematicsExtension.GetRandomGen(CurrentTime, entity);

                                    var prefabEntity = PrefabContainer.GetRandomPrefab(rndGen, out var skinIndex);

                                    if (prefabEntity != Entity.Null)
                                    {
                                        var pedestrianSettings = PedestrianSettingsRef.Config;

                                        uint seed = UnityMathematicsExtension.GetSeed(CurrentTime, entity.Index);

                                        var parkingData = new ParkingSpawnHelper.ParkingSpawnData()
                                        {
                                            Seed = seed,
                                            PedestrianEntityPrefab = prefabEntity,
                                            SpawnPointEntity = trafficNodeCapacity.PedestrianNodeEntity,
                                            NodeConnectionData = nodeConnectionData,
                                            WorldTransformLookup = WorldTransformLookup,
                                            SkinIndex = skinIndex,
                                        };

                                        ParkingSpawnHelper.Spawn(ref CommandBuffer, in parkingData, in pedestrianSettings);
                                    }
                                }
                                else
                                {
#if UNITY_EDITOR
                                    UnityEngine.Debug.Log($"Parking node Index {destinationComponent.CurrentNode.Index} doesn't have linked pedestrian node");
#endif
                                }

                                break;
                            }
                        case ParkingIdleType.TempIdleAndStart:
                            {
                                TrafficStateExtension.AddIdleState<TrafficIdleParkingNodeProcessComponent>(ref CommandBuffer, entity, ref trafficStateComponent, ref trafficIdleTagRW, TrafficIdleState.IdleNode);
                                break;
                            }
                    }

                    destinationComponent.CurrentNode = Entity.Null;
                }
            }
        }
    }
}