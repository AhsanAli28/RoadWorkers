using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(LateEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficCullWheelSystem : ISystem
    {
        private EntityQuery updateGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateGroup = SystemAPI.QueryBuilder()
                .WithNone<CulledEventTag, InViewOfCameraTag>()
                .WithAllRW<TrafficWheelsEnabledTag>()
                .WithAll<CullWheelTag, TrafficTag, VehicleWheel>()
                .Build();

            state.RequireForUpdate(updateGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var cullWheelJob = new CullWheelJob()
            {
                WheelHandlingLookup = SystemAPI.GetComponentLookup<WheelHandlingTag>(false),
            };

            cullWheelJob.Schedule(updateGroup);
        }

        [WithNone(typeof(CulledEventTag), typeof(InViewOfCameraTag))]
        [WithAll(typeof(TrafficTag))]
        [BurstCompile]
        private partial struct CullWheelJob : IJobEntity
        {
            public ComponentLookup<WheelHandlingTag> WheelHandlingLookup;

            void Execute(
                EnabledRefRW<TrafficWheelsEnabledTag> trafficWheelsEnabledTagRW,
                in DynamicBuffer<VehicleWheel> wheels)
            {
                trafficWheelsEnabledTagRW.ValueRW = false;

                for (int i = 0; i < wheels.Length; i++)
                {
                    WheelHandlingLookup.SetComponentEnabled(wheels[i].WheelEntity, false);
                }
            }
        }
    }
}