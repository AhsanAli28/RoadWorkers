using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Train
{
    [UpdateAfter(typeof(TrafficProcessMovementDataSystem))]
    [UpdateInGroup(typeof(TrafficFixedUpdateGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrainSpeedSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PathGraphSystem.Singleton>();

            updateQuery = SystemAPI.QueryBuilder()
                .WithAllRW<SpeedComponent>()
                .WithAll<TrainParentTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var simpleMovementJob = new RailSimpleMovementJob()
            {
                TrafficRailConfigReference = SystemAPI.GetSingleton<TrafficRailConfigReference>(),
                DeltaTime = SystemAPI.Time.DeltaTime,
            };

            simpleMovementJob.Schedule();
        }

        [WithAll(typeof(TrainParentTag), typeof(AliveTag))]
        [BurstCompile]
        private partial struct RailSimpleMovementJob : IJobEntity
        {
            [ReadOnly]
            public TrafficRailConfigReference TrafficRailConfigReference;

            [ReadOnly]
            public float DeltaTime;

            void Execute(
                ref VelocityComponent velocityComponent,
                ref SpeedComponent speedComponent,
                in LocalTransform localTransform,
                in VehicleInputReader vehicleInputReader,
                in TrafficSettingsComponent trafficSettingsComponent)
            {
                var gasInput = vehicleInputReader.Throttle;
                var gasInputAbs = math.abs(vehicleInputReader.Throttle);
                var targetSpeed = speedComponent.CurrentLimit * gasInputAbs;

                float speed = speedComponent.Value;

                if (gasInput > 0 && speed < targetSpeed)
                {
                    float acceleration = trafficSettingsComponent.Acceleration;
                    speed += acceleration * DeltaTime;
                }
                else if (gasInput <= 0 || speed > targetSpeed)
                {
                    float backAcceleration = trafficSettingsComponent.BrakePower;

                    if (gasInput != 0)
                    {
                        backAcceleration *= gasInputAbs;
                    }

                    speed -= backAcceleration * DeltaTime;
                }

                speedComponent.Value = math.clamp(speed, 0, trafficSettingsComponent.MaxSpeed);
                velocityComponent.Value = localTransform.Forward() * speedComponent.Value;
            }
        }
    }
}