using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Traffic.Authoring;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateAfter(typeof(TrafficProcessMovementDataSystem))]
    [UpdateInGroup(typeof(TrafficFixedUpdateGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct RailSimpleMovementSystem : ISystem, ISystemStartStop
    {
        private EntityQuery updateQuery;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TrafficRuntimeConfig>();
        }

        public void OnStartRunning(ref SystemState state)
        {
            var trafficRuntimeConfig = SystemAPI.GetSingleton<TrafficRuntimeConfig>();
            var entityType = trafficRuntimeConfig.EntityType;

            if (entityType == EntityType.PureEntityNoPhysics)
            {
                updateQuery = SystemAPI.QueryBuilder()
                    .WithNone<TrafficAccurateAligmentCustomMovementTag, TrafficIdleTag>()
                    .WithAllRW<LocalTransform>()
                    .WithAll<TrafficRailMovementTag, TrafficCustomMovementTag, AliveTag>()
                    .WithAll<TrafficPathComponent, TrafficMovementComponent, TrafficSettingsComponent, TrafficObstacleComponent, SpeedComponent>()
                    .Build();
            }
            else
            {
                updateQuery = SystemAPI.QueryBuilder()
                    .WithNone<TrafficAccurateAligmentCustomMovementTag, TrafficIdleTag>()
                    .WithAllRW<LocalTransform>()
                    .WithAll<PhysicsWorldIndex, TrafficRailMovementTag, TrafficCustomMovementTag, AliveTag>()
                    .WithAll<TrafficPathComponent, TrafficMovementComponent, TrafficSettingsComponent, TrafficObstacleComponent, SpeedComponent>()
                    .Build();

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
                TrafficCustomDestinationLookup = SystemAPI.GetComponentLookup<TrafficCustomDestinationComponent>(),
                TrafficRailConfigReference = SystemAPI.GetSingleton<TrafficRailConfigReference>(),
                DeltaTime = SystemAPI.Time.DeltaTime,
            };

            simpleMovementJob.Schedule(updateQuery);
        }

        [WithNone(typeof(TrafficAccurateAligmentCustomMovementTag), typeof(TrafficIdleTag))]
        [WithAll(typeof(TrafficRailMovementTag), typeof(TrafficCustomMovementTag), typeof(AliveTag))]
        [BurstCompile]
        private partial struct RailSimpleMovementJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<TrafficCustomDestinationComponent> TrafficCustomDestinationLookup;

            [ReadOnly]
            public TrafficRailConfigReference TrafficRailConfigReference;

            [ReadOnly]
            public float DeltaTime;

            void Execute(
                Entity entity,
                ref LocalTransform transform,
                in TrafficPathComponent trafficPathComponent,
                in TrafficMovementComponent trafficMovementComponent,
                in TrafficSettingsComponent trafficSettingsComponent,
                in TrafficObstacleComponent trafficObstacleComponent,
                in SpeedComponent speedComponent)
            {
                float3 movingSpeedVector;
                quaternion newRotation;

                bool customDestination = TrafficCustomDestinationLookup.HasComponent(entity);

                TramRailMovementUtils.CalculateRailMovement(
                    in trafficPathComponent,
                    in trafficMovementComponent,
                    in trafficSettingsComponent,
                    in trafficObstacleComponent,
                    in speedComponent,
                    in transform,
                    in TrafficRailConfigReference,
                    DeltaTime,
                    customDestination,
                    out movingSpeedVector,
                    out newRotation);

                transform.Position += movingSpeedVector * DeltaTime;
                transform.Rotation = newRotation;
            }
        }
    }
}