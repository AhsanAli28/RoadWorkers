using Spirit604.DotsCity.Simulation.Car;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(EarlyJobGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficSpeedLimitSystem : ISystem
    {
        private EntityQuery updateGroup;

        void ISystem.OnCreate(ref SystemState state)
        {
            updateGroup = SystemAPI.QueryBuilder()
                .WithNone<TrafficWagonComponent>()
                .WithAll<SpeedComponent, TrafficApproachDataComponent>()
                .Build();

            state.RequireForUpdate(updateGroup);
            state.RequireForUpdate<TrafficCommonSettingsConfigBlobReference>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var speedLimitJob = new SpeedLimitJob()
            {
                TrafficCommonSettings = SystemAPI.GetSingleton<TrafficCommonSettingsConfigBlobReference>()
            };

            speedLimitJob.ScheduleParallel();
        }

        [WithNone(typeof(TrafficWagonComponent))]
        [BurstCompile]
        public partial struct SpeedLimitJob : IJobEntity
        {
            [ReadOnly] public TrafficCommonSettingsConfigBlobReference TrafficCommonSettings;

            void Execute(
                ref SpeedComponent speedComponent,
                in TrafficApproachDataComponent trafficApproachData,
                in TrafficMovementComponent trafficMovementComponent)
            {
                var defaultLaneSpeed = TrafficCommonSettings.Reference.Value.DefaultLaneSpeed;

                float speedLimit = speedComponent.LaneLimit;
                float currentLimit = 0;

                if (speedLimit > 0)
                {
                    currentLimit = speedLimit;
                }
                else if (defaultLaneSpeed > 0)
                {
                    currentLimit = defaultLaneSpeed;

                    if (speedComponent.LaneLimit == 0)
                    {
                        speedComponent.LaneLimit = defaultLaneSpeed;
                    }
                }

                if (trafficMovementComponent.CurrentMovementDirection > 0)
                {
                    float approachSpeed = trafficApproachData.ApproachSpeed;

                    if (approachSpeed != -1 && currentLimit > approachSpeed)
                    {
                        currentLimit = approachSpeed;
                    }
                }

                speedComponent.CurrentLimit = currentLimit;
            }
        }
    }
}