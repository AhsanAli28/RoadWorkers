using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.TrafficArea
{
    [UpdateInGroup(typeof(TrafficAreaSimulationGroup))]
    [BurstCompile]
    public partial struct TrafficAreaObserveSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<TrafficAreaCarObserverEnabledTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var observeJob = new ObserveJob()
            {
                HasDriverLookup = SystemAPI.GetComponentLookup<HasDriverTag>(true),
                CarIdleLookup = SystemAPI.GetComponentLookup<TrafficIdleTag>(true),
            };

            observeJob.Schedule();
        }

        [WithAll(typeof(TrafficAreaCarObserverEnabledTag))]
        [BurstCompile]
        public partial struct ObserveJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<HasDriverTag> HasDriverLookup;

            [ReadOnly]
            public ComponentLookup<TrafficIdleTag> CarIdleLookup;

            void Execute(
                ref TrafficAreaComponent trafficAreaComponent,
                in DynamicBuffer<TrafficAreaExitCarQueueElement> exitQueue)
            {
                int count = 0;
                int exitCarCount = 0;

                for (int i = 0; i < exitQueue.Length; i++)
                {
                    var trafficEntity = exitQueue[i].TrafficEntity;

                    bool hasDriver = HasDriverLookup.HasComponent(trafficEntity);
                    bool activeCar = hasDriver && CarIdleLookup.HasComponent(trafficEntity) && !CarIdleLookup.IsComponentEnabled(trafficEntity);

                    if (activeCar)
                    {
                        count++;
                    }

                    if (hasDriver)
                    {
                        exitCarCount++;
                    }
                }

                trafficAreaComponent.ActiveCurrentCarCount = count;
                trafficAreaComponent.ExitCarCount = exitCarCount;
            }
        }
    }
}