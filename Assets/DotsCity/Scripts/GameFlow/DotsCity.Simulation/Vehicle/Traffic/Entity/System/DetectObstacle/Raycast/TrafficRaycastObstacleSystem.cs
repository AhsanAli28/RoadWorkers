using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(RaycastGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficRaycastObstacleSystem : ISystem
    {
        private EntityQuery raycastGroupQuery;

        void ISystem.OnCreate(ref SystemState state)
        {
            raycastGroupQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<HasDriverTag, TrafficTag, TrafficRaycastTag, TrafficRaycastObstacleComponent>()
                .Build(state.EntityManager);

            state.RequireForUpdate(raycastGroupQuery);

#if UNITY_EDITOR
            state.RequireForUpdate<TrafficRaycastGizmosSystem.Singleton>();
#endif

            state.RequireForUpdate<TrafficEnableRaycastSystem.Singleton>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
#if UNITY_EDITOR
            SystemAPI.GetSingleton<TrafficRaycastGizmosSystem.Singleton>().Clear();
#endif

            var trafficCommonSettingsConfigBlobReference = SystemAPI.GetSingleton<TrafficCommonSettingsConfigBlobReference>();
            var raycastAlways = TrafficEnableRaycastSystem.RaycastAlways(in trafficCommonSettingsConfigBlobReference);
            var depJob = state.Dependency;

            var raycastObstacleJob = new RaycastObstacleJob()
            {
                CollisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld.CollisionWorld,
                TrafficEnableRaycastSystemSingleton = SystemAPI.GetSingleton<TrafficEnableRaycastSystem.Singleton>(),
                TrafficChangingLaneEventLookup = SystemAPI.GetComponentLookup<TrafficChangingLaneEventTag>(true),
                TrafficRaycastConfigReference = SystemAPI.GetSingleton<RaycastConfigReference>(),
                Timestamp = (float)SystemAPI.Time.ElapsedTime,
                RaycastAlways = raycastAlways,

#if UNITY_EDITOR
                TrafficsRaycastDebugInfoParallel = SystemAPI.GetSingleton<TrafficRaycastGizmosSystem.Singleton>().TrafficRaycastDebugInfo.AsParallelWriter()
#endif
            };

            state.Dependency = raycastObstacleJob.ScheduleParallel(depJob);
        }

        [WithAll(typeof(TrafficTag), typeof(HasDriverTag))]
        [BurstCompile]
        public partial struct RaycastObstacleJob : IJobEntity
        {
            [ReadOnly]
            public CollisionWorld CollisionWorld;

            [ReadOnly]
            public TrafficEnableRaycastSystem.Singleton TrafficEnableRaycastSystemSingleton;

            [ReadOnly]
            public ComponentLookup<TrafficChangingLaneEventTag> TrafficChangingLaneEventLookup;

            [ReadOnly]
            public RaycastConfigReference TrafficRaycastConfigReference;

            [ReadOnly]
            public float Timestamp;

            [ReadOnly]
            public bool RaycastAlways;

#if UNITY_EDITOR
            [Unity.Collections.LowLevel.Unsafe.NativeDisableContainerSafetyRestriction]
            public NativeParallelMultiHashMap<Entity, TrafficRaycastGizmosSystem.BoxCastInfo>.ParallelWriter TrafficsRaycastDebugInfoParallel;
#endif

            void Execute(
                Entity entity,
                ref TrafficRaycastObstacleComponent trafficRaycastObstacleComponent,
                EnabledRefRW<TrafficRaycastTag> trafficRaycastTagRW,
                in TrafficPathComponent pathComponent,
                in TrafficSettingsComponent trafficSettingsComponent,
                in SpeedComponent speedComponent,
                in BoundsComponent boundsComponent,
                in LocalToWorld worldTransform)
            {
                trafficRaycastTagRW.ValueRW = false;

                trafficRaycastObstacleComponent.HasObstacle = false;
                trafficRaycastObstacleComponent.ObstacleEntity = Entity.Null;

                var startOffset = boundsComponent.Size.z * TrafficRaycastConfigReference.Config.Value.BoundsMultiplier;

                float currentSpeed = speedComponent.Value;
                float maxSpeed = trafficSettingsComponent.MaxSpeed;
                float t = 1 - (maxSpeed - currentSpeed) / maxSpeed;
                var rayLength = math.lerp(TrafficRaycastConfigReference.Config.Value.MinRayLength, TrafficRaycastConfigReference.Config.Value.MaxRayLength, t);

                var sourcePoint = worldTransform.Position;
                var targetPoint = pathComponent.DestinationWayPoint;
                float3 castDirection = math.normalizesafe(targetPoint - sourcePoint);

                float3 center = worldTransform.Position + new float3(0, TrafficRaycastConfigReference.Config.Value.RayYaxisOffset, 0) + castDirection * startOffset;
                quaternion orientation = worldTransform.Rotation;
                float3 halfExtents = new float3(TrafficRaycastConfigReference.Config.Value.SideOffset, TrafficRaycastConfigReference.Config.Value.BoxCastHeight, 0.01f);
                float maxDistance = rayLength;

                var currentCollisionFilter = new CollisionFilter()
                {
                    BelongsTo = TrafficRaycastConfigReference.Config.Value.RaycastFilter,
                    CollidesWith = TrafficRaycastConfigReference.Config.Value.RaycastFilter,
                    GroupIndex = 0
                };

                if (CollisionWorld.BoxCast(center, orientation, halfExtents, castDirection, maxDistance, out var hitInfo, currentCollisionFilter, QueryInteraction.IgnoreTriggers))
                {
                    var targetVehicleEntity = hitInfo.Entity;
                    var hasTarget = RaycastAlways && targetVehicleEntity != Entity.Null || TrafficEnableRaycastSystemSingleton.HasEntity(targetVehicleEntity);

                    if (hasTarget && targetVehicleEntity != entity)
                    {
                        var myVehicleChangingLane = TrafficChangingLaneEventLookup.HasComponent(entity) && TrafficChangingLaneEventLookup.IsComponentEnabled(entity);
                        var targetVehicleChangingLane = TrafficChangingLaneEventLookup.HasComponent(targetVehicleEntity) && TrafficChangingLaneEventLookup.IsComponentEnabled(targetVehicleEntity);

                        if (!myVehicleChangingLane || !targetVehicleChangingLane)
                        {
                            trafficRaycastObstacleComponent.HasObstacle = true;
                            trafficRaycastObstacleComponent.ObstacleEntity = targetVehicleEntity;
                        }
                    }
                }

#if UNITY_EDITOR
                var boxCastInfo = new TrafficRaycastGizmosSystem.BoxCastInfo()
                {
                    origin = center,
                    halfExtents = halfExtents,
                    orientation = orientation,
                    direction = castDirection,
                    MaxDistance = maxDistance,
                    HasHit = trafficRaycastObstacleComponent.HasObstacle
                };

                TrafficsRaycastDebugInfoParallel.Add(entity, boxCastInfo);
#endif
            }
        }
    }
}