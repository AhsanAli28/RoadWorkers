using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(PreEarlyJobGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficSpeedRotationSystem : ISystem
    {
        private EntityQuery updateGroup;

        void ISystem.OnCreate(ref SystemState state)
        {
            updateGroup = SystemAPI.QueryBuilder()
                .WithNone<TrafficCustomLocomotion>()
                .WithAll<TrafficTag, HasDriverTag, AliveTag>()
                .Build();

            state.RequireForUpdate(updateGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var rotationSpeedJob = new RotationSpeedJob()
            {
                RotationCurveReference = SystemAPI.GetSingleton<RotationCurveReference>()
            };

            rotationSpeedJob.ScheduleParallel();
        }

        [WithNone(typeof(TrafficCustomLocomotion))]
        [WithAll(typeof(HasDriverTag), typeof(AliveTag))]
        [BurstCompile]
        public partial struct RotationSpeedJob : IJobEntity
        {
            [ReadOnly]
            public RotationCurveReference RotationCurveReference;

            void Execute(
                ref TrafficRotationSpeedComponent rotationSpeedComponent,
                in SpeedComponent speedComponent,
                in TrafficSettingsComponent trafficSettingsComponent)
            {
                float speed = speedComponent.Value;
                float maxSpeed = trafficSettingsComponent.MaxSpeed;

                float time = math.abs(speed / maxSpeed);

                ref BlobArray<float> values = ref RotationCurveReference.Curve.Value.Values;
                float multiplier = BlobCurveUtils.Evaluate(ref values, time);
                float calculatedRotationSpeed = RotationCurveReference.Curve.Value.RotationSpeed * multiplier;

                rotationSpeedComponent = new TrafficRotationSpeedComponent { Value = calculatedRotationSpeed };
            }
        }
    }
}