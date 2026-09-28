using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(MainThreadInitGroup))]
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficPoolSystem : ISystem
    {
        public const float MAX_EXPLODE_DURATION_TIME = 10f;

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            float time = (float)SystemAPI.Time.ElapsedTime;

            foreach (var (trafficStartExplodeComponent, pooledEventTagRW)
                in SystemAPI.Query<RefRO<CarStartExplodeComponent>, EnabledRefRW<PooledEventTag>>()
                .WithDisabled<PooledEventTag>()
                .WithAll<CarTag>())
            {
                if (trafficStartExplodeComponent.ValueRO.IsPooled)
                {
                    float explodeDurationTime = time - trafficStartExplodeComponent.ValueRO.EnableTimeStamp;

                    bool explodeIsEnabled = trafficStartExplodeComponent.ValueRO.ExplodeIsEnabled == 1;
                    bool shouldHide = explodeIsEnabled && explodeDurationTime >= MAX_EXPLODE_DURATION_TIME;

                    if (!explodeIsEnabled || shouldHide)
                    {
                        pooledEventTagRW.ValueRW = true;
                    }
                }
            }
        }
    }
}