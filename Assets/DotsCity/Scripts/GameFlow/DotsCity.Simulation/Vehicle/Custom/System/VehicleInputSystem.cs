using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car.Custom
{
    [UpdateInGroup(typeof(PreEarlyJobGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct VehicleInputSystem : ISystem
    {
        private const float SteeringSpeed = 4f;
        private const float ThrottleSpeed = 4f;
        private const float BrakeSpeed = 4f;
        private const float HandBrakeSpeed = 10f;

        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<VehicleInput>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var inputJob = new InputJob()
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            };

            inputJob.ScheduleParallel();
        }

        [WithAll(typeof(VehicleInput))]
        [BurstCompile]
        public partial struct InputJob : IJobEntity
        {
            [ReadOnly]
            public float DeltaTime;

            void Execute(
                ref VehicleInput vehicleInput,
                in VehicleOutput output,
                in VehicleInputReader vehicleInputReader,
                in CustomVehicleData customVehicleData,
                in SpeedComponent speedComponent)
            {
                var throttleInput = vehicleInputReader.Throttle;
                var brakeInput = 0.0f;
                switch (vehicleInput.ThrottleMode)
                {
                    case ThrottleMode.AccelerationForward:
                        if (throttleInput < 0)
                        {
                            vehicleInput.ThrottleMode = ThrottleMode.Braking;
                        }
                        break;
                    case ThrottleMode.AccelerationBackward:
                        if (throttleInput > 0)
                        {
                            vehicleInput.ThrottleMode = ThrottleMode.Braking;
                        }
                        break;
                    case ThrottleMode.Braking:
                        if (output.LocalVelocity.z * vehicleInputReader.Throttle > 0 || math.abs(output.LocalVelocity.z) < 0.1f)
                        {
                            vehicleInput.ThrottleMode = vehicleInputReader.Throttle > 0
                                ? ThrottleMode.AccelerationForward
                                : ThrottleMode.AccelerationBackward;
                            break;
                        }

                        throttleInput = 0.0f;
                        brakeInput = math.abs(vehicleInputReader.Throttle);
                        break;
                }

                var steeringLimit = 1f;

                if (customVehicleData.CustomSteeringLimit)
                {
                    steeringLimit = customVehicleData.SteeringLimitCurve.Value.Evaluate(speedComponent.Value);
                }

                vehicleInput.Steering = Mathf.MoveTowards(vehicleInput.Steering, vehicleInputReader.SteeringInput * steeringLimit, DeltaTime * SteeringSpeed);
                vehicleInput.Throttle = Mathf.MoveTowards(vehicleInput.Throttle, throttleInput, DeltaTime * ThrottleSpeed);
                vehicleInput.Brake = Mathf.MoveTowards(vehicleInput.Brake, brakeInput, DeltaTime * BrakeSpeed);
                vehicleInput.Handbrake = Mathf.MoveTowards(vehicleInput.Handbrake, vehicleInputReader.HandbrakeInput, DeltaTime * HandBrakeSpeed);
            }
        }
    }
}