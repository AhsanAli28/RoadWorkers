using Spirit604.DotsCity.Simulation.Road;
using Spirit604.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(MainThreadEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficIdleAtParkingNodeSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAllRW<TrafficIdleParkingNodeProcessComponent, TrafficStateComponent>()
                .WithPresentRW<TrafficIdleTag>()
                .WithAll<TrafficTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var idleJob = new IdleJob()
            {
                CommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
                TrafficNodeCapacityLookup = SystemAPI.GetComponentLookup<TrafficNodeCapacityComponent>(isReadOnly: false),
                TrafficNodeLinkedComponentLookup = SystemAPI.GetComponentLookup<TrafficNodeLinkedComponent>(isReadOnly: true),
                TrafficParkingConfigReference = SystemAPI.GetSingleton<TrafficParkingConfigReference>(),
                Timestamp = (float)SystemAPI.Time.ElapsedTime,
            };

            idleJob.Run(updateQuery);
        }

        [WithAll(typeof(TrafficTag))]
        [BurstCompile]
        public partial struct IdleJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            public ComponentLookup<TrafficNodeCapacityComponent> TrafficNodeCapacityLookup;

            [ReadOnly]
            public ComponentLookup<TrafficNodeLinkedComponent> TrafficNodeLinkedComponentLookup;

            [ReadOnly]
            public TrafficParkingConfigReference TrafficParkingConfigReference;

            [ReadOnly]
            public float Timestamp;

            void Execute(
                Entity entity,
                ref TrafficIdleParkingNodeProcessComponent trafficIdleNodeTag,
                ref TrafficStateComponent trafficStateComponent,
                EnabledRefRW<TrafficIdleTag> trafficIdleTagRW)
            {
                if (!trafficIdleNodeTag.Activated)
                {
                    trafficIdleNodeTag.Activated = true;

                    var duration = TrafficParkingConfigReference.Config.Value.IdleDuration;
                    var idleDuration = UnityMathematicsExtension.GetRandomValue(duration, Timestamp, entity.Index);
                    trafficIdleNodeTag.DeactivateTimestamp = Timestamp + idleDuration;
                }
                else
                {
                    if (Timestamp >= trafficIdleNodeTag.DeactivateTimestamp)
                    {
                        var trafficNode = TrafficNodeLinkedComponentLookup[entity].LinkedPlace;

                        if (TrafficNodeCapacityLookup.HasComponent(trafficNode))
                        {
                            var trafficNodeCapacityComponent = TrafficNodeCapacityLookup[trafficNode];

                            if (trafficNodeCapacityComponent.UnlinkNodeAndReqDriver(ref CommandBuffer, true))
                            {
                            }

                            TrafficNodeCapacityLookup[trafficNode] = trafficNodeCapacityComponent;
                        }

                        TrafficStateExtension.RemoveIdleState<TrafficIdleParkingNodeProcessComponent>(ref CommandBuffer, entity, ref trafficStateComponent, ref trafficIdleTagRW, TrafficIdleState.IdleNode);
                    }
                }
            }
        }
    }
}
