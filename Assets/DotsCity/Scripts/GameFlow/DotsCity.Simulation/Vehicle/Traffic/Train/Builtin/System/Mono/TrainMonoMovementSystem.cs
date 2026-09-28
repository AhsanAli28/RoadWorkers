using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Train
{
    [UpdateInGroup(typeof(BeforePhysXFixedStepGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrainMonoMovementSystem : ISystem
    {
        private EntityQuery updateQuery;
        private SystemHandle movementSystem;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PathGraphSystem.Singleton>();

            movementSystem = state.WorldUnmanaged.GetExistingUnmanagedSystem<TrafficProcessMovementDataSystem>();

            updateQuery = SystemAPI.QueryBuilder()
                .WithNone<TrafficMonoMovementDisabled>()
                .WithAll<Transform, TrafficCustomMovementTag, AliveTag, TrainTag, TrafficDestinationComponent, TrainComponent>()
                .WithAll<TrafficPathComponent, TrafficMovementComponent, TrafficSettingsComponent, TrafficObstacleComponent, SpeedComponent>()
                .Build();

            state.RequireForUpdate(updateQuery);
        }

        void ISystem.OnUpdate(ref SystemState state)
        {
            ref var movementSystemRef = ref state.WorldUnmanaged.ResolveSystemStateRef(movementSystem);

            movementSystemRef.Dependency.Complete();

            var Graph = SystemAPI.GetSingleton<PathGraphSystem.Singleton>();
            var TrafficRailConfigReference = SystemAPI.GetSingleton<TrafficRailConfigReference>();
            var DeltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (
                rb,
                trafficPathComponent,
                speedComponent,
                trafficDestinationComponent,
                trafficSettingsComponent)
                in SystemAPI.Query<
                    SystemAPI.ManagedAPI.UnityEngineComponent<Rigidbody>,
                    RefRO<TrafficPathComponent>,
                    RefRO<SpeedComponent>,
                    RefRO<TrafficDestinationComponent>,
                    RefRO<TrafficSettingsComponent>>()
                .WithNone<TrafficMonoMovementDisabled>()
                .WithAll<TrafficCustomMovementTag, AliveTag, TrainTag>())
            {
                float3 newPos;
                quaternion newRotation;

                var rigidbody = rb.Value;
                var transform = rigidbody.transform;
                var localTransform = LocalTransform.FromPositionRotation(transform.position, transform.rotation);

                var trafficPathComponentLocal = trafficPathComponent.ValueRO;
                var speedComponentLocal = speedComponent.ValueRO;
                var trafficDestinationComponentLocal = trafficDestinationComponent.ValueRO;
                var trafficSettingsComponentLocal = trafficSettingsComponent.ValueRO;

                var found = TrainMovementUtils.Calculate(
                    in Graph,
                    in trafficPathComponentLocal,
                    in trafficDestinationComponentLocal,
                    in trafficSettingsComponentLocal,
                    in speedComponentLocal,
                    in localTransform,
                    in TrafficRailConfigReference,
                    DeltaTime,
                    out newPos,
                    out newRotation);

                newPos.y = transform.position.y;

                rigidbody.Move(newPos, newRotation);
            };
        }
    }
}