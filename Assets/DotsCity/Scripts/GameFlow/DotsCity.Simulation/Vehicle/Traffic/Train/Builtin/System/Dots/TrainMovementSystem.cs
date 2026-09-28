using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.DotsCity.Simulation.Traffic.Authoring;
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
    public partial struct TrainMovementSystem : ISystem, ISystemStartStop
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TrafficRuntimeConfig>();
            state.RequireForUpdate<PathGraphSystem.Singleton>();
        }

        public void OnStartRunning(ref SystemState state)
        {
            var trafficRuntimeConfig = SystemAPI.GetSingleton<TrafficRuntimeConfig>();
            var entityType = trafficRuntimeConfig.EntityType;

            var updateQueryBuilder =
                new EntityQueryBuilder(Allocator.Temp)
                    .WithNone<TrafficAccurateAligmentCustomMovementTag>()
                    .WithAllRW<LocalTransform>()
                    .WithAll<TrafficCustomMovementTag, AliveTag, TrainTag, TrafficDestinationComponent>()
                    .WithAll<TrafficPathComponent, TrafficSettingsComponent, TrafficObstacleComponent, SpeedComponent>();

            if (entityType == EntityType.PureEntityNoPhysics)
            {
                updateQuery = updateQueryBuilder.Build(state.EntityManager);
            }
            else if (entityType == EntityType.HybridEntityMonoPhysics)
            {
                updateQueryBuilder = updateQueryBuilder.WithAll<TrafficMonoMovementDisabled>();
                updateQuery = updateQueryBuilder.Build(state.EntityManager);
            }
            else
            {
                updateQueryBuilder = updateQueryBuilder.WithAll<PhysicsWorldIndex>();

                updateQuery = updateQueryBuilder.Build(state.EntityManager);
                updateQuery.SetSharedComponentFilter(new PhysicsWorldIndex() { Value = ProjectConstants.NoPhysicsWorldIndex });
            }

            state.RequireForUpdate(updateQuery);
        }

        public void OnStopRunning(ref SystemState state) { }

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

                transform.Position = newPos;
                transform.Rotation = newRotation;
            }
        }
    }
}