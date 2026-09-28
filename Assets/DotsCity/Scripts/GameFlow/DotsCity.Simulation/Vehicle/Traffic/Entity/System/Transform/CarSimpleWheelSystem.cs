using Spirit604.DotsCity.Simulation.Car;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateAfter(typeof(TrafficProcessMovementDataSystem))]
    [UpdateInGroup(typeof(TrafficFixedUpdateGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct CarSimpleWheelSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<WheelHandlingTag, DefaultWheelData>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var rotateWheelJob = new RotateWheelJob()
            {
                SpeedLookup = SystemAPI.GetComponentLookup<SpeedComponent>(true),
                TrafficMovementLookup = SystemAPI.GetComponentLookup<TrafficMovementComponent>(true),
            };

            rotateWheelJob.ScheduleParallel();
        }

        [WithAll(typeof(WheelHandlingTag))]
        [BurstCompile]
        private partial struct RotateWheelJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<SpeedComponent> SpeedLookup;

            [ReadOnly]
            public ComponentLookup<TrafficMovementComponent> TrafficMovementLookup;

            void Execute(
                ref LocalTransform transform,
                ref DefaultWheelData defaultWheelData)
            {
                var speedComponent = SpeedLookup[defaultWheelData.VehicleEntity];

                float weRotation = speedComponent.Value / defaultWheelData.WheelBase;
                weRotation = math.radians(weRotation);

                if (!defaultWheelData.Steering)
                {
                    transform.Rotation = math.mul(transform.Rotation, quaternion.RotateX(weRotation * defaultWheelData.InverseValue));
                }
                else
                {
                    defaultWheelData.Angle += weRotation;

                    var trafficMovementComponent = TrafficMovementLookup[defaultWheelData.VehicleEntity];

                    transform.Rotation = defaultWheelData.InitialRotation;
                    transform = transform.RotateY(trafficMovementComponent.SteeringAngle);
                    transform = transform.RotateX(defaultWheelData.Angle * defaultWheelData.InverseValue);
                }
            }
        }
    }
}