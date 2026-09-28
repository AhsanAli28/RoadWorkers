using Spirit604.DotsCity.Simulation.Car;
using Spirit604.Gameplay.Road;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(TrafficInputGroup), OrderLast = true)]
    [BurstCompile]
    public partial struct TrafficInputSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithDisabled<TrafficIdleTag, TrafficInitTag>()
                .WithAllRW<VehicleInputReader>()
                .WithPresentRW<TrafficBackwardMovementTag>()
                .WithAll<HasDriverTag, CarEngineStartedTag, TrafficTag, TrafficObstacleComponent, TrafficPathComponent, SpeedComponent>()
                .Build();

            state.RequireForUpdate(updateQuery);
            state.RequireForUpdate<TrafficSettingsConfigBlobReference>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var inputJob = new InputJob()
            {
                TrafficNpcObstacleLookup = SystemAPI.GetComponentLookup<TrafficNpcObstacleComponent>(true),
                TrafficRaycastObstacleLookup = SystemAPI.GetComponentLookup<TrafficRaycastObstacleComponent>(true),
                TrafficSettingsConfigBlobReference = SystemAPI.GetSingleton<TrafficSettingsConfigBlobReference>(),
            };

            inputJob.ScheduleParallel(updateQuery);
        }

        [WithDisabled(typeof(TrafficIdleTag), typeof(TrafficInitTag))]
        [WithAll(typeof(HasDriverTag), typeof(CarEngineStartedTag), typeof(TrafficTag))]
        [BurstCompile]
        partial struct InputJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<TrafficNpcObstacleComponent> TrafficNpcObstacleLookup;

            [ReadOnly]
            public ComponentLookup<TrafficRaycastObstacleComponent> TrafficRaycastObstacleLookup;

            [ReadOnly]
            public TrafficSettingsConfigBlobReference TrafficSettingsConfigBlobReference;

            void Execute(
                Entity entity,
                ref VehicleInputReader trafficInputComponent,
                EnabledRefRW<TrafficBackwardMovementTag> trafficBackwardMovementTagRW,
                in TrafficObstacleComponent carObstacleComponent,
                in TrafficPathComponent trafficPathComponent,
                in SpeedComponent speedComponent)
            {
                var npcObstacle = false;
                var raycastObstacle = false;
                var carObstacle = false;

                if (TrafficNpcObstacleLookup.HasComponent(entity))
                {
                    npcObstacle = TrafficNpcObstacleLookup[entity].HasObstacle;
                }

                if (TrafficRaycastObstacleLookup.HasComponent(entity))
                {
                    raycastObstacle = TrafficRaycastObstacleLookup[entity].HasObstacle;
                }

                carObstacle = carObstacleComponent.HasObstacle;

                bool shouldStop =
                           npcObstacle ||
                           raycastObstacle ||
                           carObstacle;

                float gasInput = !shouldStop ? 1 : 0;

                bool backwardMovement = trafficPathComponent.PathDirection == PathForwardType.Backward;
                float direction = 1;

                if (backwardMovement)
                {
                    direction = -1;
                    gasInput = gasInput * direction;
                }

                if (speedComponent.Value > speedComponent.CurrentLimit && speedComponent.CurrentLimit > 0)
                {
                    gasInput = direction * -TrafficSettingsConfigBlobReference.Reference.Value.BrakingRate;
                }

                if (trafficInputComponent.Throttle != gasInput)
                {
                    trafficInputComponent.Throttle = gasInput;

                    if (backwardMovement)
                    {
                        if (!trafficBackwardMovementTagRW.ValueRW)
                        {
                            trafficBackwardMovementTagRW.ValueRW = true;
                        }
                    }
                    else
                    {
                        if (trafficBackwardMovementTagRW.ValueRW && trafficInputComponent.Throttle >= 0)
                        {
                            trafficBackwardMovementTagRW.ValueRW = false;
                        }
                    }
                }

                trafficInputComponent.HandbrakeInput = shouldStop ? 1 : 0;
            }
        }
    }
}
