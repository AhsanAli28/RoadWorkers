using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.TrafficPublic
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficPublicIdleSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithPresentRW<TrafficPublicExitCompleteTag>()
                .WithAllRW<TrafficPublicIdleComponent, TrafficPublicExitSettingsComponent>()
                .WithAllRW<TrafficStateComponent, TrafficIdleTag>()
                .WithAll<TrafficPublicIdleSettingsComponent, CarCapacityComponent, TrafficNodeLinkedComponent, SpeedComponent>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var trafficPublicIdleJob = new TrafficPublicIdleJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                TrafficNodeCapacityLookup = SystemAPI.GetComponentLookup<TrafficNodeCapacityComponent>(false),
                TrafficPublicExitSettingsComponentLookup = SystemAPI.GetComponentLookup<TrafficPublicExitSettingsComponent>(false),
                TrafficPublicProccessExitTagLookup = SystemAPI.GetComponentLookup<TrafficPublicProccessExitTag>(false),
                TrafficPublicExitCompleteTagLookup = SystemAPI.GetComponentLookup<TrafficPublicExitCompleteTag>(false),
                TrafficWagonElementLookup = SystemAPI.GetBufferLookup<TrafficWagonElement>(true),
                CarCapacityComponentLookup = SystemAPI.GetComponentLookup<CarCapacityComponent>(true),
                ConnectedPedestrianNodeElementLookup = SystemAPI.GetBufferLookup<ConnectedPedestrianNodeElement>(true),
                PedestrianWaitQueueLookup = SystemAPI.GetBufferLookup<WaitQueueElement>(true),
                Time = (float)SystemAPI.Time.ElapsedTime,
            };

            trafficPublicIdleJob.Run(updateQuery);
        }

        [BurstCompile]
        public partial struct TrafficPublicIdleJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            public ComponentLookup<TrafficNodeCapacityComponent> TrafficNodeCapacityLookup;
            public ComponentLookup<TrafficPublicExitSettingsComponent> TrafficPublicExitSettingsComponentLookup;
            public ComponentLookup<TrafficPublicProccessExitTag> TrafficPublicProccessExitTagLookup;
            public ComponentLookup<TrafficPublicExitCompleteTag> TrafficPublicExitCompleteTagLookup;

            [ReadOnly]
            public BufferLookup<TrafficWagonElement> TrafficWagonElementLookup;

            [ReadOnly]
            public BufferLookup<WaitQueueElement> PedestrianWaitQueueLookup;

            [ReadOnly]
            public ComponentLookup<CarCapacityComponent> CarCapacityComponentLookup;

            [ReadOnly]
            public BufferLookup<ConnectedPedestrianNodeElement> ConnectedPedestrianNodeElementLookup;

            [ReadOnly]
            public float Time;

            void Execute(
                Entity entity,
                ref TrafficPublicIdleComponent trafficPublicIdleComponent,
                ref TrafficStateComponent trafficStateComponent,
                EnabledRefRW<TrafficIdleTag> trafficIdleTagRW,
                in TrafficPublicIdleSettingsComponent trafficPublicIdleSettingsComponent,
                in CarCapacityComponent carCapacityComponent,
                in SpeedComponent speedComponent,
                in TrafficNodeLinkedComponent trafficNodeLinkedComponent)
            {
                if (trafficPublicIdleComponent.StationState == StationState.Default)
                {
                    trafficPublicIdleComponent.NodeEntity = trafficNodeLinkedComponent.LinkedPlace;
                    trafficPublicIdleComponent.StationState = StationState.WaitingForStop;
                }

                if (trafficPublicIdleComponent.StationState == StationState.WaitingForStop)
                {
                    if (speedComponent.ValueAbs < 0.01f)
                    {
                        trafficPublicIdleComponent.StationState = StationState.InitIdleAfterStop;
                    }
                    else
                    {
                        return;
                    }
                }

                if (trafficPublicIdleComponent.StationState == StationState.InitIdleAfterStop)
                {
                    trafficPublicIdleComponent.DeactivateTime = Time + trafficPublicIdleSettingsComponent.IdleTimeAfterStop;
                    trafficPublicIdleComponent.StationState = StationState.IdleAfterStop;
                }

                if (trafficPublicIdleComponent.StationState == StationState.IdleAfterStop)
                {
                    float remainTimeIdle = trafficPublicIdleComponent.DeactivateTime - Time;

                    if (remainTimeIdle <= 0)
                    {
                        trafficPublicIdleComponent.StationState = StationState.StartExitting;
                    }
                    else
                    {
                        return;
                    }
                }

                if (trafficPublicIdleComponent.StationState == StationState.StartExitting)
                {
                    trafficPublicIdleComponent.StationState = StationState.Proccesing;
                    Random random = UnityMathematicsExtension.GetRandomGen(Time, entity.Index);

                    trafficPublicIdleComponent.DeactivateTime = Time + random.NextFloat(trafficPublicIdleSettingsComponent.MinIdleTime, trafficPublicIdleSettingsComponent.MaxIdleTime);

                    ActivateEntity(entity, ref trafficPublicIdleComponent, ref CarCapacityComponentLookup);

                    if (TrafficWagonElementLookup.HasBuffer(entity))
                    {
                        var buffer = TrafficWagonElementLookup[entity];

                        for (int i = 0; i < buffer.Length; i++)
                        {
                            ActivateEntity(buffer[i].Entity, ref trafficPublicIdleComponent, ref CarCapacityComponentLookup);
                        }
                    }
                }

                float remainTime = trafficPublicIdleComponent.DeactivateTime - Time;

                var outOfIdleTime = remainTime <= 0;
                var outOfCapacity = carCapacityComponent.AvailableCapacity == 0;

                var trafficNodeLinkedEntity = trafficNodeLinkedComponent.LinkedPlace;

                if (!TrafficNodeCapacityLookup.HasComponent(trafficNodeLinkedEntity))
                {
                    LeaveStation(
                        entity,
                        ref trafficStateComponent,
                        ref TrafficPublicExitCompleteTagLookup,
                        ref TrafficPublicProccessExitTagLookup,
                        ref TrafficWagonElementLookup,
                        ref trafficIdleTagRW);
                    return;
                }

                var nodeCapacity = TrafficNodeCapacityLookup[trafficNodeLinkedEntity];

                var queueIsEmpty = true;
                bool hasBuffer = false;
                DynamicBuffer<ConnectedPedestrianNodeElement> connectedBuffer = default;

                if (ConnectedPedestrianNodeElementLookup.HasBuffer(trafficNodeLinkedEntity))
                {
                    hasBuffer = true;
                    connectedBuffer = ConnectedPedestrianNodeElementLookup[trafficNodeLinkedEntity];

                    for (int i = 0; i < connectedBuffer.Length; i++)
                    {
                        var pedestrianNodeEntity = connectedBuffer[i].PedestrianNodeEntity;

                        if (PedestrianWaitQueueLookup.HasBuffer(pedestrianNodeEntity))
                        {
                            queueIsEmpty = PedestrianWaitQueueLookup[pedestrianNodeEntity].Length == 0;

                            if (!queueIsEmpty)
                                break;
                        }
                    }
                }

                var readyToMove = outOfIdleTime && (outOfCapacity || queueIsEmpty);

                if (readyToMove)
                {
                    nodeCapacity.UnlinkNode();
                    TrafficNodeCapacityLookup[trafficNodeLinkedEntity] = nodeCapacity;

                    LeaveStation(
                        entity,
                        ref trafficStateComponent,
                        ref TrafficPublicExitCompleteTagLookup,
                        ref TrafficPublicProccessExitTagLookup,
                        ref TrafficWagonElementLookup,
                        ref trafficIdleTagRW);

                    if (hasBuffer)
                    {
                        for (int i = 0; i < connectedBuffer.Length; i++)
                        {
                            var pedestrianNodeEntity = connectedBuffer[i].PedestrianNodeEntity;

                            if (pedestrianNodeEntity != Entity.Null)
                            {
                                CommandBuffer.SetComponentEnabled<NodeProcessWaitQueueTag>(pedestrianNodeEntity, false);
                            }
                        }
                    }
                }
            }

            private void ActivateEntity(
                Entity entity,
                ref TrafficPublicIdleComponent trafficPublicIdleComponent,
                ref ComponentLookup<CarCapacityComponent> carCapacityLookup)
            {
                Random random = UnityMathematicsExtension.GetRandomGen(Time, entity.Index);

                var trafficPublicCapacitySettingsComponent = TrafficPublicExitSettingsComponentLookup[entity];
                var currentPedestrianExitCount = random.NextInt(trafficPublicCapacitySettingsComponent.MinPedestrianExitCount, trafficPublicCapacitySettingsComponent.MaxPedestrianExitCount);

                var carCapacityComponent = carCapacityLookup[entity];
                currentPedestrianExitCount = math.min(currentPedestrianExitCount, carCapacityComponent.EnteredCount);

                if (currentPedestrianExitCount > 0)
                {
                    trafficPublicCapacitySettingsComponent.CurrentPedestrianExitCount = currentPedestrianExitCount;
                    TrafficPublicProccessExitTagLookup.SetComponentEnabled(entity, true);
                }
                else
                {
                    TrafficPublicExitCompleteTagLookup.SetComponentEnabled(entity, true);
                }

                TrafficPublicExitSettingsComponentLookup[entity] = trafficPublicCapacitySettingsComponent;
            }

            private void LeaveStation(
                Entity entity,
                ref TrafficStateComponent trafficStateComponent,
                ref ComponentLookup<TrafficPublicExitCompleteTag> trafficPublicExitCompleteTagRW,
                ref ComponentLookup<TrafficPublicProccessExitTag> trafficPublicProccessExitTagLookup,
                ref BufferLookup<TrafficWagonElement> wagonLookup,
                ref EnabledRefRW<TrafficIdleTag> trafficIdleTagRW)
            {
                CommandBuffer.RemoveComponent<TrafficNodeLinkedComponent>(entity);
                TrafficStateExtension.RemoveIdleState<TrafficPublicIdleComponent>(ref CommandBuffer, entity, ref trafficStateComponent, ref trafficIdleTagRW, TrafficIdleState.PublicTransportStop);

                trafficPublicExitCompleteTagRW.SetComponentEnabled(entity, false);

                if (wagonLookup.HasBuffer(entity))
                {
                    var buffer = wagonLookup[entity];

                    for (int i = 0; i < buffer.Length; i++)
                    {
                        trafficPublicExitCompleteTagRW.SetComponentEnabled(buffer[i].Entity, false);
                        trafficPublicProccessExitTagLookup.SetComponentEnabled(buffer[i].Entity, false);
                        trafficPublicExitCompleteTagRW.SetComponentEnabled(entity, false);
                    }
                }
            }
        }
    }
}