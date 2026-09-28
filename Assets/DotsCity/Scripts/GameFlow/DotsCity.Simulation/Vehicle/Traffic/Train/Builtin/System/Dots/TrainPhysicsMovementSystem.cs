using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Train
{
    [UpdateAfter(typeof(TrafficProcessMovementDataSystem))]
    [UpdateInGroup(typeof(TrafficFixedUpdateGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrainPhysicsMovementSystem : ISystem
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PathGraphSystem.Singleton>();

            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<TrafficAccurateAligmentCustomMovementTag>()
                .WithAllRW<PhysicsVelocity, LocalTransform>()
                .WithAll<PhysicsWorldIndex, TrafficCustomMovementTag, AliveTag, TrainTag, TrafficDestinationComponent>()
                .WithAll<TrafficPathComponent, TrafficSettingsComponent, SpeedComponent>()
                .Build();

            updateQuery.SetSharedComponentFilter(new PhysicsWorldIndex() { Value = 0 });

            state.RequireForUpdate(updateQuery);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var simpleMovementJob = new RailSimpleMovementJob()
            {
                Graph = SystemAPI.GetSingleton<PathGraphSystem.Singleton>(),
                TrafficRailConfigReference = SystemAPI.GetSingleton<TrafficRailConfigReference>(),
                DeltaTime = SystemAPI.Time.DeltaTime,
            };

            simpleMovementJob.Schedule(updateQuery);
        }

        [WithNone(typeof(TrafficAccurateAligmentCustomMovementTag))]
        [WithAll(typeof(TrafficCustomMovementTag), typeof(AliveTag), typeof(TrainTag))]
        [BurstCompile]
        private partial struct RailSimpleMovementJob : IJobEntity
        {
            [ReadOnly]
            public PathGraphSystem.Singleton Graph;

            [ReadOnly]
            public TrafficRailConfigReference TrafficRailConfigReference;

            [ReadOnly]
            public float DeltaTime;

            void Execute(
                ref PhysicsVelocity physicsVelocity,
                ref LocalTransform transform,
                in TrafficPathComponent trafficPathComponent,
                in SpeedComponent speedComponent,
                in TrafficDestinationComponent trafficDestinationComponent,
                in TrafficSettingsComponent trafficSettingsComponent)
            {
                float3 newPos;
                quaternion newRotation;

                TrainMovementUtils.Calculate(
                    in Graph,
                    in trafficPathComponent,
                    in trafficDestinationComponent,
                    in trafficSettingsComponent,
                    in speedComponent,
                    in transform,
                    in TrafficRailConfigReference,
                    DeltaTime,
                    out newPos,
                    out newRotation);

                var velocity = (newPos - transform.Position) / DeltaTime;
                physicsVelocity.Linear = velocity;
                transform.Rotation = newRotation;
            }
        }
    }
}