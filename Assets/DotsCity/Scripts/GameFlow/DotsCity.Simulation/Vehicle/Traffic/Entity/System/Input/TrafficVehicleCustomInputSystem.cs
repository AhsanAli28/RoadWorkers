using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Car.Custom;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(TrafficInputGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficVehicleCustomInputSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<CarStoppingEngineStartedTag>()
                .WithDisabled<TrafficIdleTag>()
                .WithAllRW<VehicleInputReader>()
                .WithAll<TrafficTag, HasDriverTag, AliveTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var customInputJob = new CustomInputJob()
            {
                TrafficSettingsConfigBlobReference = SystemAPI.GetSingleton<TrafficSettingsConfigBlobReference>()
            };

            customInputJob.ScheduleParallel();
        }

        [WithNone(typeof(CarStoppingEngineStartedTag))]
        [WithDisabled(typeof(TrafficIdleTag))]
        [WithAll(typeof(TrafficTag), typeof(HasDriverTag), typeof(AliveTag))]
        [BurstCompile]
        public partial struct CustomInputJob : IJobEntity
        {
            [ReadOnly]
            public TrafficSettingsConfigBlobReference TrafficSettingsConfigBlobReference;

            void Execute(
                ref VehicleInputReader trafficInputComponent,
                in SpeedComponent speedComponent,
                in CustomSteeringData customSteeringData,
                in TrafficMovementComponent trafficMovementComponent)
            {
                if (!TrafficSettingsConfigBlobReference.Reference.Value.UseSteeringDamping)
                {
                    trafficInputComponent.SteeringInput = trafficMovementComponent.DesiredSteeringAngle / customSteeringData.MaxSteeringAngle;
                }
                else
                {
                    trafficInputComponent.SteeringInput = trafficMovementComponent.SteeringAngle / customSteeringData.MaxSteeringAngle;
                }

                if (TrafficSettingsConfigBlobReference.Reference.Value.LimitSteeringPower)
                {
                    float maxSteering = TrafficSettingsConfigBlobReference.Reference.Value.SteeringPowerCurve.Value.Evaluate(speedComponent.Value);
                    trafficInputComponent.SteeringInput = trafficInputComponent.SteeringInput * maxSteering;
                }
            }
        }
    }
}
