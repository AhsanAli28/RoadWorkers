using Spirit604.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(StructuralSystemGroup))]
    [BurstCompile]
    public partial struct TrafficIdleAtNodeSystem : ISystem
    {
        private const float MinIdleTime = 5f;
        private const float MaxIdleTime = 10f;

        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAllRW<TrafficIdleNodeProcessComponent, TrafficStateComponent>()
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
                Timestamp = (float)SystemAPI.Time.ElapsedTime,
            };

            idleJob.Run(updateQuery);
        }

        [WithAll(typeof(TrafficTag))]
        [BurstCompile]
        public partial struct IdleJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public float Timestamp;

            void Execute(
                Entity entity,
                ref TrafficIdleNodeProcessComponent trafficIdleNodeTag,
                ref TrafficStateComponent trafficStateComponent,
                EnabledRefRW<TrafficIdleTag> trafficIdleTagRW)
            {
                if (!trafficIdleNodeTag.Activated)
                {
                    trafficIdleNodeTag.Activated = true;
                    var rndGen = UnityMathematicsExtension.GetRandomGen(Timestamp, entity.Index);
                    trafficIdleNodeTag.DeactivateTimestamp = Timestamp + rndGen.NextFloat(MinIdleTime, MaxIdleTime);
                    TrafficStateExtension.AddIdleState(ref trafficStateComponent, ref trafficIdleTagRW, TrafficIdleState.IdleNode);
                }
                else
                {
                    if (Timestamp >= trafficIdleNodeTag.DeactivateTimestamp)
                    {
                        TrafficStateExtension.RemoveIdleState<TrafficIdleNodeProcessComponent>(ref CommandBuffer, entity, ref trafficStateComponent, ref trafficIdleTagRW, TrafficIdleState.IdleNode);
                    }
                }
            }
        }
    }
}
