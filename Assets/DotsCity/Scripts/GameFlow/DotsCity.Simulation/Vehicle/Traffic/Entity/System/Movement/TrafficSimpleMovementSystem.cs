using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Traffic.Authoring;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateAfter(typeof(TrafficProcessMovementDataSystem))]
    [UpdateInGroup(typeof(TrafficFixedUpdateGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficSimpleMovementSystem : ISystem, ISystemStartStop
    {
        private EntityQuery updateQuery;

        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TrafficRuntimeConfig>();
        }

        public void OnStartRunning(ref SystemState state)
        {
            var trafficRuntimeConfig = SystemAPI.GetSingleton<TrafficRuntimeConfig>();
            var entityType = trafficRuntimeConfig.EntityType;

            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithNone<TrafficCustomMovementTag>()
                .WithAllRW<LocalTransform>()
                .WithAll<TrafficTag, HasDriverTag, AliveTag, TrafficMovementComponent>();

            switch (entityType)
            {
                case EntityType.PureEntityNoPhysics:
                    {
                        builder = builder.WithNone<PhysicsVelocity>();
                        updateQuery = builder.Build(state.EntityManager);
                        break;
                    }
                case EntityType.HybridEntityMonoPhysics:
                    {
                        builder = builder.WithAll<TrafficMonoMovementDisabled>();
                        updateQuery = builder.Build(state.EntityManager);
                        break;
                    }
                default:
                    {
                        builder = builder.WithAll<PhysicsWorldIndex>();
                        updateQuery = builder.Build(state.EntityManager);
                        updateQuery.SetSharedComponentFilter(new PhysicsWorldIndex() { Value = ProjectConstants.NoPhysicsWorldIndex });
                        break;
                    }
            }

            state.RequireForUpdate(updateQuery);
        }

        public void OnStopRunning(ref SystemState state) { }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var simpleMovementJob = new SimpleMovementJob()
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            };

            simpleMovementJob.ScheduleParallel(updateQuery);
        }

        [WithNone(typeof(TrafficCustomMovementTag))]
        [WithAll(typeof(TrafficTag), typeof(HasDriverTag), typeof(AliveTag))]
        [BurstCompile]
        public partial struct SimpleMovementJob : IJobEntity
        {
            [ReadOnly]
            public float DeltaTime;

            void Execute(
                ref LocalTransform transform,
                in TrafficMovementComponent trafficMovementComponent)
            {
                var movingDelta = trafficMovementComponent.LinearVelocity * DeltaTime;
                transform.Position += movingDelta;

                var newRotation = trafficMovementComponent.CurrentCalculatedRotation;
                transform.Rotation = newRotation;
            }
        }
    }
}