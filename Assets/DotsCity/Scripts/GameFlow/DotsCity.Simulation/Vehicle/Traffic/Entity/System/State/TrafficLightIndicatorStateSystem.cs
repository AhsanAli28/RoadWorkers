using Spirit604.DotsCity.Simulation.Car;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(PreEarlyJobGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficLightIndicatorStateSystem : ISystem
    {
        private EntityQuery vehicleQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            vehicleQuery = SystemAPI.QueryBuilder()
                .WithAll<TrafficTag>()
                .Build();

            state.RequireForUpdate(vehicleQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var blinkerStateJob = new BlinkerStateJob()
            {
                CarEngineStartedTagLookup = SystemAPI.GetComponentLookup<CarEngineStartedTag>(true),
                TrafficChangingLaneEventLookup = SystemAPI.GetComponentLookup<TrafficChangingLaneEventTag>(true),
            };

            blinkerStateJob.ScheduleParallel();
        }

        [WithAll(typeof(TrafficTag))]
        [BurstCompile]
        private partial struct BlinkerStateJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<CarEngineStartedTag> CarEngineStartedTagLookup;

            [ReadOnly]
            public ComponentLookup<TrafficChangingLaneEventTag> TrafficChangingLaneEventLookup;

            void Execute(
                Entity entity,
                ref VehicleLightComponent vehicleLightComponent,
                in TrafficTargetDirectionComponent targetDirectionComponent,
                in VehicleInputReader vehicleInputReader,
                in SpeedComponent speedComponent)
            {
                BlinkerState state = BlinkerState.None;

                var changeLane = TrafficChangingLaneEventLookup.HasComponent(entity) && TrafficChangingLaneEventLookup.IsComponentEnabled(entity);

                if (!changeLane)
                {
                    if (targetDirectionComponent.Direction != 0)
                    {
                        state = targetDirectionComponent.Direction == -1 ? BlinkerState.Left : BlinkerState.Right;
                    }
                }
                else
                {
                    state = targetDirectionComponent.Direction == -1 ? BlinkerState.Left : BlinkerState.Right;
                }

                if (vehicleLightComponent.BlinkerState != state)
                    vehicleLightComponent.BlinkerState = state;

                bool reverse = speedComponent.Value < 0 && vehicleInputReader.Throttle < 0;
                bool braking = !reverse && speedComponent.Value >= 0 && vehicleInputReader.Throttle <= 0;

                bool engine = CarEngineStartedTagLookup.HasComponent(entity);

                if (vehicleLightComponent.Reverse != reverse)
                    vehicleLightComponent.Reverse = reverse;

                if (vehicleLightComponent.EngineOn != engine)
                    vehicleLightComponent.EngineOn = engine;

                if (vehicleLightComponent.Braking != braking)
                    vehicleLightComponent.Braking = braking;
            }
        }
    }
}