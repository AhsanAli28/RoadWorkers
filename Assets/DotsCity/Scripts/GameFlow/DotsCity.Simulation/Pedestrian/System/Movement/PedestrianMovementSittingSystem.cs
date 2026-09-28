using Spirit604.DotsCity.Simulation.Pedestrian.State;
using Spirit604.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(PedestrianFixedSimulationGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct PedestrianMovementSittingSystem : ISystem
    {
        private const float TargetDirectionDot = 0.99f;

        private EntityQuery updateGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            updateGroup = SystemAPI.QueryBuilder()
                .WithNone<BenchCustomMovementTag>()
                .WithDisabled<SeatAchievedTargetTag>()
                .WithAll<CustomMovementTag>()
                .Build();

            state.RequireForUpdate(updateGroup);
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var movementSittingJob = new MovementSittingJob()
            {
                BenchConfigReference = SystemAPI.GetSingleton<BenchConfigReference>(),
                DeltaTime = SystemAPI.Time.DeltaTime
            };

            movementSittingJob.Schedule();
        }

        [WithDisabled(typeof(SeatAchievedTargetTag))]
        [WithAll(typeof(CustomMovementTag), typeof(BenchCustomMovementTag))]
        [BurstCompile]
        public partial struct MovementSittingJob : IJobEntity
        {
            [ReadOnly]
            public BenchConfigReference BenchConfigReference;

            [ReadOnly]
            public float DeltaTime;

            void Execute(
                Entity entity,
                ref LocalTransform transform,
                EnabledRefRW<SeatAchievedTargetTag> seatAchievedTargetTagRW,
                in SeatSlotLinkedComponent seatSlotLinkedComponent)
            {
                float3 position = transform.Position;

                float rotationSpeed = BenchConfigReference.Config.Value.SittingRotationSpeed * DeltaTime;

                float3 previosTarget = default;
                float3 targetValue = default;
                float3 dir = default;

                quaternion targetRotation = default;

                switch (seatSlotLinkedComponent.SitState)
                {
                    case SitState.SittingIn:
                        {
                            targetValue = seatSlotLinkedComponent.SeatPosition;
                            previosTarget = seatSlotLinkedComponent.EnterSeatPosition;
                            dir = (previosTarget - targetValue).Flat();
                            targetRotation = seatSlotLinkedComponent.SeatRotation;
                            break;
                        }
                    case SitState.SittingOut:
                        {
                            targetValue = seatSlotLinkedComponent.EnterSeatPosition;
                            previosTarget = seatSlotLinkedComponent.SeatPosition;
                            dir = (targetValue - previosTarget).Flat();
                            targetRotation = quaternion.LookRotation(dir, math.up());
                            break;
                        }
                }

                quaternion sitRotation = math.slerp(transform.Rotation, targetRotation, rotationSpeed);
                transform.Rotation = sitRotation;

                var currentPosition = math.lerp(position, targetValue, BenchConfigReference.Config.Value.SittingMovementSpeed * DeltaTime);
                transform.Position = currentPosition;

                var forward = transform.Forward();
                float distanceSQ = math.distancesq(currentPosition, targetValue);

                var currentTargetDirection = math.mul(targetRotation, math.forward());

                float directionDot = math.dot(currentTargetDirection, forward);

                if (distanceSQ <= BenchConfigReference.Config.Value.SitPointDistanceSQ && directionDot >= TargetDirectionDot)
                {
                    seatAchievedTargetTagRW.ValueRW = true;
                }
            }
        }
    }
}