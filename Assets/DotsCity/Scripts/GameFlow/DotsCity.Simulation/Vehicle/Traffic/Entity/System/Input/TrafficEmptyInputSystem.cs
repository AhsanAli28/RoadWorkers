using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(TrafficInputGroup), OrderLast = true)]
    [BurstCompile]
    public partial struct TrafficEmptyInputSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<HasDriverTag, CarEngineStartedTag>()
                .WithAll<TrafficTag, AliveTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var inputJob = new InputJob();

            inputJob.Schedule();
        }

        [WithNone(typeof(HasDriverTag), typeof(CarEngineStartedTag))]
        [WithAll(typeof(TrafficTag), typeof(AliveTag))]
        [BurstCompile]
        partial struct InputJob : IJobEntity
        {
            void Execute(ref VehicleInputReader trafficInputComponent)
            {
                trafficInputComponent = VehicleInputReader.GetBrake();
            }
        }
    }
}