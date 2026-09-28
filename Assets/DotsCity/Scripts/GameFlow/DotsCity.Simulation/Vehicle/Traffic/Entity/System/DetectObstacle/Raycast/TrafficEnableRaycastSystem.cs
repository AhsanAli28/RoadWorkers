using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(EarlyEventGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficEnableRaycastSystem : ISystem, ISystemStartStop
    {
        private const int InitialCapacity = 100;

        public struct CustomTargetHash
        {
            public Entity Entity;
            public float3 Position;
        }

        public struct Singleton : IComponentData
        {
            public NativeHashSet<Entity> CustomEntityTargets;

            public bool HasEntity(Entity entity) => CustomEntityTargets.Contains(entity);
        }

        private EntityQuery targetGroupQuery;
        private EntityQuery raycastGroupQuery;
        private NativeParallelMultiHashMap<int, CustomTargetHash> customTargetHashMap;
        private NativeHashSet<Entity> customEntityTargets;
        private bool hasCustomTargets;

        void ISystem.OnCreate(ref SystemState state)
        {
            raycastGroupQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<HasDriverTag, TrafficTag, TrafficRaycastObstacleComponent>()
                .Build(state.EntityManager);

            state.RequireForUpdate(raycastGroupQuery);
        }

        void ISystem.OnDestroy(ref SystemState state)
        {
            if (customTargetHashMap.IsCreated)
            {
                customTargetHashMap.Dispose();
            }

            if (customEntityTargets.IsCreated)
            {
                customEntityTargets.Dispose();
            }
        }

        void ISystemStartStop.OnStartRunning(ref SystemState state)
        {
            var trafficCommonSettingsConfigBlobReference = SystemAPI.GetSingleton<TrafficCommonSettingsConfigBlobReference>();

            if (!RaycastAlways(trafficCommonSettingsConfigBlobReference))
            {
                var trafficGeneralSettingsReference = SystemAPI.GetSingleton<TrafficGeneralSettingsReference>();

                targetGroupQuery = TrafficRaycastObstacleTargetQueryProvider.GetTargetQuery(
                    state.EntityManager,
                    in trafficGeneralSettingsReference,
                    trafficCommonSettingsConfigBlobReference.Reference.Value.DetectObstacleMode,
                    out hasCustomTargets);
            }

            if (!customTargetHashMap.IsCreated)
            {
                customTargetHashMap = new NativeParallelMultiHashMap<int, CustomTargetHash>(InitialCapacity, Allocator.Persistent);
            }

            if (!customEntityTargets.IsCreated)
            {
                customEntityTargets = new NativeHashSet<Entity>(InitialCapacity, Allocator.Persistent);

                state.EntityManager.AddComponentData(state.SystemHandle, new Singleton()
                {
                    CustomEntityTargets = customEntityTargets
                });
            }
        }

        void ISystemStartStop.OnStopRunning(ref SystemState state) { }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var trafficRaycastConfigReference = SystemAPI.GetSingleton<RaycastConfigReference>();

            var depJob = state.Dependency;

            var trafficCommonSettingsConfigBlobReference = SystemAPI.GetSingleton<TrafficCommonSettingsConfigBlobReference>();
            var raycastAlways = RaycastAlways(in trafficCommonSettingsConfigBlobReference);

            if (hasCustomTargets && !raycastAlways)
            {
                customTargetHashMap.Clear();

                var customTargetEntityCount = targetGroupQuery.CalculateEntityCount();

                if (customTargetHashMap.Capacity < customTargetEntityCount)
                {
                    customTargetHashMap.Capacity = customTargetEntityCount;
                    customEntityTargets.Capacity = customTargetEntityCount;
                }

                FillCustomHashJob fillCustomHashJob = new FillCustomHashJob()
                {
                    CustomTargetHashMap = customTargetHashMap.AsParallelWriter(),
                    CustomEntityTargets = customEntityTargets
                };

                var fillCustomHashHandle = fillCustomHashJob.ScheduleParallel(targetGroupQuery, state.Dependency);

                depJob = fillCustomHashHandle;
            }

            var enableRaycastJob = new EnableRaycastJob()
            {
                CustomTargetHashMap = customTargetHashMap,
                CustomEntityTargets = customEntityTargets,
                TrafficChangingLaneEventLookup = SystemAPI.GetComponentLookup<TrafficChangingLaneEventTag>(true),
                TrafficRaycastConfigReference = trafficRaycastConfigReference,
                Timestamp = (float)SystemAPI.Time.ElapsedTime,
                RaycastAlways = raycastAlways,
            };

            state.Dependency = enableRaycastJob.ScheduleParallel(depJob);
        }

        [WithDisabled(typeof(TrafficRaycastTag))]
        [WithAll(typeof(TrafficTag), typeof(HasDriverTag))]
        [BurstCompile]
        public partial struct EnableRaycastJob : IJobEntity
        {
            [ReadOnly]
            public NativeParallelMultiHashMap<int, CustomTargetHash> CustomTargetHashMap;

            [ReadOnly]
            public NativeHashSet<Entity> CustomEntityTargets;

            [ReadOnly]
            public ComponentLookup<TrafficChangingLaneEventTag> TrafficChangingLaneEventLookup;

            [ReadOnly]
            public RaycastConfigReference TrafficRaycastConfigReference;

            [ReadOnly]
            public float Timestamp;

            [ReadOnly]
            public bool RaycastAlways;

            void Execute(
                Entity entity,
                ref TrafficRaycastObstacleComponent trafficRaycastObstacleComponent,
                EnabledRefRW<TrafficRaycastTag> trafficRaycastTagRW,
                in TrafficPathComponent pathComponent,
                in LocalToWorld worldTransform)
            {
                if (trafficRaycastObstacleComponent.NextCastTime > Timestamp)
                    return;

                var rnd = UnityMathematicsExtension.GetRandomGen(Timestamp, entity.Index);
                var frequency = rnd.NextFloat(TrafficRaycastConfigReference.Config.Value.CastFrequency.x, TrafficRaycastConfigReference.Config.Value.CastFrequency.y);
                trafficRaycastObstacleComponent.NextCastTime = Timestamp + frequency;

                var shouldRaycast = RaycastAlways;

                if (!RaycastAlways && CustomTargetHashMap.Count() > 0)
                {
                    float3 forward = pathComponent.PathDirection == PathForwardType.Forward ? worldTransform.Forward : -worldTransform.Forward;

                    var keys = HashMapHelper.GetHashMapPosition9Cells(worldTransform.Position.Flat());

                    for (int i = 0; i < keys.Length; i++)
                    {
                        var key = keys[i];

                        if (CustomTargetHashMap.TryGetFirstValue(key, out var carDataEntity, out var nativeMultiHashMapIterator))
                        {
                            do
                            {
                                if (carDataEntity.Entity == entity)
                                    continue;

                                var targetPosition = carDataEntity.Position;

                                float distanceToTarget = math.distancesq(targetPosition, worldTransform.Position);

                                if (distanceToTarget < TrafficRaycastConfigReference.Config.Value.MaxRayLengthSQ)
                                {
                                    float3 directionToCar = (targetPosition - worldTransform.Position);

                                    float dot = math.dot(forward, directionToCar);

                                    if (dot > TrafficRaycastConfigReference.Config.Value.DotDirection)
                                    {
                                        shouldRaycast = true;
                                        break;
                                    }
                                }

                            } while (CustomTargetHashMap.TryGetNextValue(out carDataEntity, ref nativeMultiHashMapIterator));
                        }

                        if (shouldRaycast)
                            break;
                    }

                    keys.Dispose();
                }

                if (!shouldRaycast)
                {
                    shouldRaycast = trafficRaycastObstacleComponent.HasObstacle;
                }

                if (shouldRaycast)
                {
                    trafficRaycastTagRW.ValueRW = true;
                }
            }
        }

        [BurstCompile]
        public partial struct FillCustomHashJob : IJobEntity
        {
            [NativeDisableContainerSafetyRestriction]
            public NativeParallelMultiHashMap<int, CustomTargetHash>.ParallelWriter CustomTargetHashMap;

            [NativeDisableContainerSafetyRestriction]
            public NativeHashSet<Entity> CustomEntityTargets;

            void Execute(
                Entity entity,
                in LocalToWorld worldTransform)
            {
                var position = worldTransform.Position.Flat();
                var key = HashMapHelper.GetHashMapPosition(position);

                CustomTargetHashMap.Add(key, new CustomTargetHash()
                {
                    Entity = entity,
                    Position = worldTransform.Position
                });

                CustomEntityTargets.Add(entity);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RaycastAlways(in TrafficCommonSettingsConfigBlobReference trafficCommonSettingsConfigBlobReference)
        {
            return trafficCommonSettingsConfigBlobReference.Reference.Value.DetectObstacleMode == DetectObstacleMode.RaycastOnly || trafficCommonSettingsConfigBlobReference.Reference.Value.DetectNpcMode == DetectNpcMode.Raycast;
        }
    }
}