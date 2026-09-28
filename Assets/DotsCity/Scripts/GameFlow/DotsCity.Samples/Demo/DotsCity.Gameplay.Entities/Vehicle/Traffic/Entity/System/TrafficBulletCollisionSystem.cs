using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Events;
using Spirit604.DotsCity.Gameplay.Weapon;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.Extensions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Traffic
{
    [UpdateInGroup(typeof(LateSimulationGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class TrafficBulletCollisionSystem : EntityDamageEventSystemBase
    {
        private const float YBorderOffset = 1f;

        private SystemHandle carHashMapSystem;
        private EntityQuery bulletQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            carHashMapSystem = World.Unmanaged.GetExistingUnmanagedSystem<CarHashMapSystem>();

            bulletQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithDisabledRW<PooledEventTag>()
                .WithAll<BulletComponent, BulletStatsComponent, LocalTransform>()
                .Build(this);

            RequireForUpdate(bulletQuery);
            RequireForUpdate<CarHashMapSystem.Singleton>();
        }

        protected override void OnUpdate()
        {
            ref var carHashMapSystemRef = ref World.Unmanaged.ResolveSystemStateRef(carHashMapSystem);

            Dependency = new TrafficBulletCollisionJob()
            {
                Writer = EntityDamageEventConsumerSystem.CreateConsumerWriter(bulletQuery.CalculateChunkCount()),
                CarHashMapSingleton = SystemAPI.GetSingleton<CarHashMapSystem.Singleton>(),
                PooledEventTagHandle = SystemAPI.GetComponentTypeHandle<PooledEventTag>(false),
                LocalTransformHandle = SystemAPI.GetComponentTypeHandle<LocalTransform>(true),
                BulletComponentHandle = SystemAPI.GetComponentTypeHandle<BulletComponent>(true),
                BulletStatsComponentHandle = SystemAPI.GetComponentTypeHandle<BulletStatsComponent>(true),

            }.ScheduleParallel(bulletQuery, carHashMapSystemRef.Dependency);

            EntityDamageEventConsumerSystem.RegisterTriggerDependency(Dependency);
        }

        [BurstCompile]
        public struct TrafficBulletCollisionJob : IJobChunk
        {
            [NativeDisableParallelForRestriction]
            public NativeStream.Writer Writer;

            [ReadOnly]
            public CarHashMapSystem.Singleton CarHashMapSingleton;

            public ComponentTypeHandle<PooledEventTag> PooledEventTagHandle;

            [NativeDisableContainerSafetyRestriction]
            [ReadOnly] public ComponentTypeHandle<LocalTransform> LocalTransformHandle;

            [ReadOnly] public ComponentTypeHandle<BulletComponent> BulletComponentHandle;
            [ReadOnly] public ComponentTypeHandle<BulletStatsComponent> BulletStatsComponentHandle;

            public void Execute(in ArchetypeChunk chunk, int chunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var localToWorlds = chunk.GetNativeArray(ref LocalTransformHandle);
                var bullets = chunk.GetNativeArray(ref BulletComponentHandle);
                var bulletStats = chunk.GetNativeArray(ref BulletStatsComponentHandle);

                Writer.BeginForEachIndex(chunkIndex);

                for (int entityIndex = 0; entityIndex < chunk.Count; ++entityIndex)
                {
                    var destroyed = chunk.IsComponentEnabled(ref PooledEventTagHandle, entityIndex);

                    if (destroyed)
                        continue;

                    var position = localToWorlds[entityIndex].Position;
                    var rotation = localToWorlds[entityIndex].Rotation;
                    var forward = localToWorlds[entityIndex].Forward();

                    var keys = HashMapHelper.GetHashMapPosition9Cells(position);
                    bool didHit = false;

                    var bulletComponent = bullets[entityIndex];
                    var bulletStatsComponent = bulletStats[entityIndex];

                    for (int i = 0; i < keys.Length; i++)
                    {
                        var key = keys[i];

                        if (CarHashMapSingleton.CarHashMap.TryGetFirstValue(key, out var carDataEntity, out var nativeMultiHashMapIterator))
                        {
                            do
                            {
                                bool canHit = bulletComponent.FactionType == FactionType.All || bulletComponent.FactionType != carDataEntity.FactionType;

                                if (!canHit)
                                    continue;

                                var carPosition = carDataEntity.Position;
                                float3 bulletPosition = position;

                                if (carPosition.y - YBorderOffset > bulletPosition.y || bulletPosition.y > carPosition.y + carDataEntity.BoundsSize.y + YBorderOffset)
                                    continue;

                                Vector3 carSize = carDataEntity.BoundsExtents;
                                float3 carPositionFlat = carPosition.Flat();
                                float3 bulletPositionFlat = position.Flat();

                                float distance = math.distance(carPositionFlat, bulletPositionFlat);

                                if (distance < carSize.z * 1.2f)
                                {
                                    Quaternion carRotation = rotation;

                                    ObstacleSquare myCarSquare = new ObstacleSquare(carPositionFlat, carRotation, carSize);

                                    bool inRectangle = VectorExtensions.PointInRectangle3D
                                        (myCarSquare.Square.Line1.A1,
                                        myCarSquare.Square.Line2.A1,
                                        myCarSquare.Square.Line2.A2,
                                        myCarSquare.Square.Line1.A2,
                                        bulletPositionFlat);

                                    if (inRectangle)
                                    {
                                        PoolEntityUtils.DestroyEntity(in chunk, ref PooledEventTagHandle, entityIndex);

                                        var damageHitData = new DamageHitData()
                                        {
                                            DamagedEntity = carDataEntity.Entity,
                                            Damage = bulletStatsComponent.Damage,
                                            HitDirection = forward,
                                            HitPosition = position
                                        };

                                        Writer.Write(damageHitData);
                                        didHit = true;
                                        break;
                                    }
                                }

                            } while (CarHashMapSingleton.CarHashMap.TryGetNextValue(out carDataEntity, ref nativeMultiHashMapIterator));
                        }

                        if (didHit)
                            break;
                    }

                    keys.Dispose();
                }

                Writer.EndForEachIndex();
            }
        }
    }
}