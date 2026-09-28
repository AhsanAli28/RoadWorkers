using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Burst;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.TrafficArea
{
    [UpdateInGroup(typeof(TrafficAreaSimulationGroup))]
    [BurstCompile]
    public partial struct TrafficAreaExitIdleSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateQuery = SystemAPI.QueryBuilder()
                .WithDisabled<TrafficIdleTag, TrafficMovingToExitTag>()
                .WithAll<TrafficWaitForExitTag, HasDriverTag>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var exitIdleJob = new ExitIdleJob()
            {
            };

            exitIdleJob.Schedule();
        }

        [WithDisabled(typeof(TrafficIdleTag), typeof(TrafficMovingToExitTag))]
        [WithAll(typeof(HasDriverTag), typeof(TrafficWaitForExitTag))]
        [BurstCompile]
        public partial struct ExitIdleJob : IJobEntity
        {
            void Execute(
                ref TrafficStateComponent stateComponent,
                EnabledRefRW<TrafficIdleTag> trafficIdleTagRW)
            {
                TrafficStateExtension.AddIdleState(ref stateComponent, ref trafficIdleTagRW, TrafficIdleState.WaitForExitArea);
            }
        }
    }
}
