using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Car.Custom;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(SimulationGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficDisableWheelSystem : ISystem
    {
        private EntityQuery updateGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateGroup = SystemAPI.QueryBuilder()
                .WithNone<AliveTag>()
                .WithAll<TrafficTag, TrafficWheelsEnabledTag, VehicleWheel>()
                .Build();

            state.RequireForUpdate(updateGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var cullWheelJob = new CullWheelJob()
            {
                WheelHandlingLookup = SystemAPI.GetComponentLookup<WheelHandlingTag>(false),
                VehicleOutputLookup = SystemAPI.GetComponentLookup<VehicleOutput>(false)
            };

            state.Dependency = cullWheelJob.Schedule(updateGroup, state.Dependency);
        }

        [WithNone(typeof(AliveTag))]
        [WithAll(typeof(TrafficTag), typeof(TrafficWheelsEnabledTag))]
        [BurstCompile]
        public partial struct CullWheelJob : IJobEntity
        {
            public EntityCommandBuffer commandBuffer;

            public ComponentLookup<WheelHandlingTag> WheelHandlingLookup;

            [NativeDisableContainerSafetyRestriction]
            public ComponentLookup<VehicleOutput> VehicleOutputLookup;

            void Execute(
                Entity entity,
                EnabledRefRW<TrafficWheelsEnabledTag> trafficWheelsEnabledTagRW,
                in DynamicBuffer<VehicleWheel> wheels)
            {
                trafficWheelsEnabledTagRW.ValueRW = false;

                for (int i = 0; i < wheels.Length; i++)
                {
                    WheelHandlingLookup.SetComponentEnabled(wheels[i].WheelEntity, false);
                }

                if (VehicleOutputLookup.HasComponent(entity))
                {
                    VehicleOutputLookup.SetComponentEnabled(entity, false);
                }
            }
        }
    }
}