using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Events;
using Spirit604.DotsCity.Simulation.Common;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using RaycastHit = Unity.Physics.RaycastHit;

namespace Spirit604.DotsCity.Gameplay.Weapon
{
    [UpdateInGroup(typeof(RaycastGroup))]
    public partial class BulletRaycastSystem : EntityDamageEventSystemBase
    {
        private EntityQuery bulletQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            bulletQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithDisabledRW<PooledEventTag>()
                .WithAll<BulletComponent, BulletStatsComponent, LocalToWorld>()
                .Build(this);

            RequireForUpdate(bulletQuery);
        }

        protected override void OnUpdate()
        {
            Dependency = new TrafficBulletRaycastJob()
            {
                Writer = EntityDamageEventConsumerSystem.CreateConsumerWriter(bulletQuery.CalculateChunkCount()),
                WorldTransformHandle = SystemAPI.GetComponentTypeHandle<LocalToWorld>(true),
                BulletComponentHandle = SystemAPI.GetComponentTypeHandle<BulletComponent>(true),
                BulletStatsComponentHandle = SystemAPI.GetComponentTypeHandle<BulletStatsComponent>(true),
                PooledEventTagHandle = SystemAPI.GetComponentTypeHandle<PooledEventTag>(false),
                HealthLookup = SystemAPI.GetComponentLookup<HealthComponent>(true),
                FactionTypeLookup = SystemAPI.GetComponentLookup<FactionTypeComponent>(true),
                CollisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld.CollisionWorld,
                DeltaTime = SystemAPI.Time.DeltaTime,
            }.Schedule(bulletQuery, Dependency);

            EntityDamageEventConsumerSystem.RegisterTriggerDependency(Dependency);
        }

        [BurstCompile]
        public struct TrafficBulletRaycastJob : IJobChunk
        {
            public NativeStream.Writer Writer;

            [ReadOnly] public ComponentTypeHandle<LocalToWorld> WorldTransformHandle;
            [ReadOnly] public ComponentTypeHandle<BulletComponent> BulletComponentHandle;
            [ReadOnly] public ComponentTypeHandle<BulletStatsComponent> BulletStatsComponentHandle;
            public ComponentTypeHandle<PooledEventTag> PooledEventTagHandle;

            [ReadOnly] public ComponentLookup<HealthComponent> HealthLookup;
            [ReadOnly] public ComponentLookup<FactionTypeComponent> FactionTypeLookup;
            [ReadOnly] public CollisionWorld CollisionWorld;
            [ReadOnly] public float DeltaTime;

            public void Execute(in ArchetypeChunk chunk, int chunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var worldTransforms = chunk.GetNativeArray(ref WorldTransformHandle);
                var bullets = chunk.GetNativeArray(ref BulletComponentHandle);
                var bulletStats = chunk.GetNativeArray(ref BulletStatsComponentHandle);

                Writer.BeginForEachIndex(chunkIndex);

                for (int entityIndex = 0; entityIndex < chunk.Count; ++entityIndex)
                {
                    var position = worldTransforms[entityIndex].Position;
                    var forward = worldTransforms[entityIndex].Forward;

                    var bulletComponent = bullets[entityIndex];
                    var bulletStatsComponent = bulletStats[entityIndex];

                    var destroyed = chunk.IsComponentEnabled(ref PooledEventTagHandle, entityIndex);

                    if (destroyed)
                        continue;

                    float3 rayStart = position;
                    float3 rayEnd = position + forward * bulletStatsComponent.FlySpeed * DeltaTime;
                    CollisionFilter filter = CollisionFilter.Default;

                    var input = (new RaycastInput()
                    {
                        Start = rayStart,
                        End = rayEnd,
                        Filter = filter
                    });

                    RaycastHit raycastHit;

                    if (CollisionWorld.CastRay(input, out raycastHit) && HealthLookup.HasComponent(raycastHit.Entity))
                    {
                        FactionType factionType = FactionType.All;

                        if (FactionTypeLookup.HasComponent(raycastHit.Entity))
                        {
                            factionType = FactionTypeLookup[raycastHit.Entity].Value;
                        }

                        bool canHit = bulletComponent.FactionType == FactionType.All || factionType == FactionType.All || bulletComponent.FactionType != factionType;

                        if (canHit)
                        {
                            PoolEntityUtils.DestroyEntity(in chunk, ref PooledEventTagHandle, entityIndex);

                            var bulletHitDataComponent = new DamageHitData()
                            {
                                DamagedEntity = raycastHit.Entity,
                                Damage = bulletStatsComponent.Damage,
                                HitPosition = position,
                                HitDirection = forward
                            };

                            Writer.Write(bulletHitDataComponent);
                        }
                    }
                }

                Writer.EndForEachIndex();
            }
        }
    }
}