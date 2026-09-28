using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(SimulationGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct CalculateCollisionSystem : ISystem
    {
        private const float MinimumCollisionDefaultForce = 2f;
        private const float CustomHashCellOffset = 3f;
        private const float PointInRectangleObstacleSizeMultiplier = 0.95f;
        private const float PedestrianHeight = 2f;

#if !RUKHANKA
        private const float SpeedSizeMultiplier = 1.5f;
#else
        private const float SpeedSizeMultiplier = 2f;
#endif

        private EntityQuery npcQuery;
        private SystemHandle carHashMapSystem;

        void ISystem.OnCreate(ref SystemState state)
        {
            npcQuery = SystemAPI.QueryBuilder()
                .WithNone<PhysicsVelocity>()
                .WithPresentRW<HasCollisionTag>()
                .WithAllRW<CollisionComponent>()
                .WithAll<LocalToWorld, PedestrianMovementSettings, CircleColliderComponent>()
                .Build();

            carHashMapSystem = state.WorldUnmanaged.GetExistingUnmanagedSystem<CarHashMapSystem>();

            state.RequireForUpdate(npcQuery);
            state.RequireForUpdate<CarHashMapSystem.Singleton>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var collisionJob = new CollisionJob()
            {
                CarHashMapSingleton = SystemAPI.GetSingleton<CarHashMapSystem.Singleton>(),
                DeltaTime = SystemAPI.Time.DeltaTime,
                Timestamp = (float)SystemAPI.Time.ElapsedTime,
            };

            collisionJob.ScheduleParallel(npcQuery);
        }

        [WithNone(typeof(PhysicsVelocity))]
        [BurstCompile]
        public partial struct CollisionJob : IJobEntity
        {
            [ReadOnly]
            public CarHashMapSystem.Singleton CarHashMapSingleton;

            [ReadOnly]
            public float DeltaTime;

            [ReadOnly]
            public float Timestamp;

            void Execute(
               ref CollisionComponent collisionComponent,
               EnabledRefRW<HasCollisionTag> hasCollisionTagRW,
               in PedestrianMovementSettings pedestrianSpeed,
               in CircleColliderComponent pedestrianCollider,
               in LocalToWorld worldTransform)
            {
                float3 localPedestrianPosition = worldTransform.Position.Flat();

                bool hasIntersect = false;

                NativeList<int> keys = HashMapHelper.GetHashMapPosition9Cells(localPedestrianPosition, offset: CustomHashCellOffset);

                for (int i = 0; i < keys.Length; i++)
                {
                    int pedestrianKey = keys[i];

                    if (CarHashMapSingleton.CarHashMap.TryGetFirstValue(pedestrianKey, out var carHashEntity, out var nativeMultiHashMapIterator))
                    {
                        do
                        {
                            float yDistance = math.abs(carHashEntity.Position.y - worldTransform.Position.y);

                            if (yDistance >= PedestrianHeight)
                                continue;

                            float3 carPosition = carHashEntity.Position;
                            float3 carPositionFlat = carPosition.Flat();

                            float distance = math.distance(localPedestrianPosition, carPositionFlat);

                            if (distance >= carHashEntity.BoundsSize.z)
                                continue;

                            float impulse = ((Vector3)(carHashEntity.Velocity)).magnitude;

                            Vector3 extents = carHashEntity.BoundsExtents;

                            float moveSpeed = pedestrianSpeed.CurrentMovementSpeed;

                            Quaternion carRotation = carHashEntity.Rotation;

                            float3 forceDirection = math.normalize(carHashEntity.Velocity).Flat();
                            float3 force = forceDirection * impulse;

                            Vector3 extents2 = extents;

                            if (math.length(force) > 1f)
                            {
                                extents2 = new Vector3(extents2.x, extents2.y, extents2.z * SpeedSizeMultiplier);
                            }

                            ObstacleSquare carSquare = new ObstacleSquare(carPositionFlat, carRotation, extents2 * PointInRectangleObstacleSizeMultiplier);

                            bool pointInRectangle = VectorExtensions.PointInRectangle3D(carSquare.Square, localPedestrianPosition);

                            if (pointInRectangle)
                            {
                                hasIntersect = true;

                                if (math.length(force) < MinimumCollisionDefaultForce || (math.isnan(force.x) && math.isnan(force.z)))
                                {
                                    force = math.mul(carRotation, new float3(0, 0, 1) * MinimumCollisionDefaultForce);
                                }

                                collisionComponent.CollidablePosition = carPosition;
                                collisionComponent.Position = localPedestrianPosition;
                                collisionComponent.Force = force;
                                collisionComponent.LastDamageDealerID = (int)carHashEntity.FactionType;
                                break;
                            }
                            else
                            {
                                ObstacleSquare carSquare2 = new ObstacleSquare(carPositionFlat, carRotation, extents);

                                float collisionRadius = pedestrianCollider.Radius;

                                Vector3 localPedestrianPositionVectorSecondLinePoint = localPedestrianPosition + worldTransform.Forward * collisionRadius;
                                VectorExtensions.Line pedestrianLineToCar = new VectorExtensions.Line(localPedestrianPosition, localPedestrianPositionVectorSecondLinePoint);

                                Vector3 intersectPoint = VectorExtensions.LineWithSquareIntersect(pedestrianLineToCar, carSquare2.Square, true);

                                bool isIntersect = intersectPoint != Vector3.zero;

                                float3 collisionPosition = default;

                                if (isIntersect)
                                {
                                    hasIntersect = true;
                                    collisionPosition = new float3(intersectPoint);
                                    collisionComponent.CollidablePosition = carPosition;
                                }

                                Vector3 nextPedestrianPoint = worldTransform.Position + worldTransform.Forward * (collisionRadius + moveSpeed * DeltaTime);
                                var nextPointInRectangle = VectorExtensions.PointInRectangle3D(carSquare.Square, nextPedestrianPoint);

                                if (nextPointInRectangle)
                                {
                                    collisionComponent.Position = collisionPosition;
                                    collisionComponent.Force = force;
                                    collisionComponent.LastDamageDealerID = (int)carHashEntity.FactionType;
                                    break;
                                }
                            }

                        } while (CarHashMapSingleton.CarHashMap.TryGetNextValue(out carHashEntity, ref nativeMultiHashMapIterator));
                    }

                    if (hasIntersect)
                    {
                        break;
                    }
                }

                if (!hasIntersect)
                {
                    collisionComponent = default;
                }
                else
                {
                    if (collisionComponent.FirstCollisionTime == 0)
                    {
                        collisionComponent.FirstCollisionTime = Timestamp;
                    }

                    collisionComponent.LastCollisionTimestamp = Timestamp;
                }

                if (hasCollisionTagRW.ValueRW != hasIntersect)
                {
                    hasCollisionTagRW.ValueRW = hasIntersect;
                }

                keys.Dispose();
            }
        }
    }
}