using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(LateSimulationGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficEnableWheelSystem : ISystem
    {
        private EntityQuery updateGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateGroup = SystemAPI.QueryBuilder()
                .WithDisabled<CulledEventTag, TrafficWheelsEnabledTag>()
                .WithAll<CullWheelTag, TrafficTag, InViewOfCameraTag>()
                .Build();

            state.RequireForUpdate(updateGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var cullWheelJob = new EnableWheelJob()
            {
                WheelHandlingLookup = SystemAPI.GetComponentLookup<WheelHandlingTag>(false),
            };

            cullWheelJob.Schedule();
        }

        [WithDisabled(typeof(CulledEventTag), typeof(TrafficWheelsEnabledTag))]
        [WithAll(typeof(CullWheelTag), typeof(TrafficTag), typeof(InViewOfCameraTag), typeof(AliveTag))]
        [BurstCompile]
        private partial struct EnableWheelJob : IJobEntity
        {
            public ComponentLookup<WheelHandlingTag> WheelHandlingLookup;

            void Execute(
                EnabledRefRW<TrafficWheelsEnabledTag> trafficWheelsEnabledTagRW,
                in DynamicBuffer<VehicleWheel> wheels)
            {
                trafficWheelsEnabledTagRW.ValueRW = true;

                for (int i = 0; i < wheels.Length; i++)
                {
                    WheelHandlingLookup.SetComponentEnabled(wheels[i].WheelEntity, true);
                }
            }
        }
    }
}