using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Events;
using Spirit604.DotsCity.Gameplay.Traffic;
using Spirit604.DotsCity.Gameplay.Weapon;
using Spirit604.DotsCity.Simulation.Npc;
using Spirit604.Extensions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Gameplay.Npc
{
    [UpdateAfter(typeof(TrafficBulletCollisionSystem))]
    [UpdateInGroup(typeof(LateSimulationGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class NpcBulletCollisionSystem : EntityDamageEventSystemBase
    {
        private SystemHandle npcHashMapSystem;
        private TrafficBulletCollisionSystem trafficBulletCollisionSystem;
        private EntityQuery bulletQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            npcHashMapSystem = World.Unmanaged.GetExistingUnmanagedSystem<NpcHashMapSystem>();
            trafficBulletCollisionSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<TrafficBulletCollisionSystem>();

            bulletQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithDisabledRW<PooledEventTag>()
                .WithAll<BulletComponent, BulletStatsComponent, LocalToWorld>()
                .Build(this);

            RequireForUpdate(bulletQuery);
            RequireForUpdate<NpcHashMapSystem.Singleton>();
        }

        protected override void OnUpdate()
        {
            ref var npcHashMapSystemRef = ref World.Unmanaged.ResolveSystemStateRef(npcHashMapSystem);

            var deps = JobHandle.CombineDependencies(npcHashMapSystemRef.Dependency, trafficBulletCollisionSystem.GetDependency());

            Dependency = new BulletNpcCollisionJob()
            {
                Writer = EntityDamageEventConsumerSystem.CreateConsumerWriter(bulletQuery.CalculateChunkCount()),
                NpcHashMapSingleton = SystemAPI.GetSingleton<NpcHashMapSystem.Singleton>(),
                PooledEventTagHandle = SystemAPI.GetComponentTypeHandle<PooledEventTag>(false),
                LocalToWorldHandle = GetComponentTypeHandle<LocalToWorld>(true),
                BulletComponentHandle = GetComponentTypeHandle<BulletComponent>(true),
                BulletStatsComponentHandle = GetComponentTypeHandle<BulletStatsComponent>(true),
            }.ScheduleParallel(bulletQuery, deps);

            EntityDamageEventConsumerSystem.RegisterTriggerDependency(Dependency);
        }

        [BurstCompile]
        public struct BulletNpcCollisionJob : IJobChunk
        {
            [NativeDisableParallelForRestriction]
            public NativeStream.Writer Writer;

            [ReadOnly]
            public NpcHashMapSystem.Singleton NpcHashMapSingleton;

            public ComponentTypeHandle<PooledEventTag> PooledEventTagHandle;

            [NativeDisableContainerSafetyRestriction]
            [ReadOnly] public ComponentTypeHandle<LocalToWorld> LocalToWorldHandle;
            [ReadOnly] public ComponentTypeHandle<BulletComponent> BulletComponentHandle;
            [ReadOnly] public ComponentTypeHandle<BulletStatsComponent> BulletStatsComponentHandle;

            public void Execute(in ArchetypeChunk chunk, int chunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var localToWorlds = chunk.GetNativeArray(ref LocalToWorldHandle);
                var bullets = chunk.GetNativeArray(ref BulletComponentHandle);
                var bulletStats = chunk.GetNativeArray(ref BulletStatsComponentHandle);

                Writer.BeginForEachIndex(chunkIndex);

                for (int entityIndex = 0; entityIndex < chunk.Count; ++entityIndex)
                {
                    var destroyed = chunk.IsComponentEnabled(ref PooledEventTagHandle, entityIndex);

                    if (destroyed)
                        continue;

                    var position = localToWorlds[entityIndex].Position;
                    float3 bulletPosition = position.Flat();

                    var forward = localToWorlds[entityIndex].Forward;
                    var keys = HashMapHelper.GetHashMapPosition9Cells(bulletPosition);
                    bool hitted = false;

                    var bulletComponent = bullets[entityIndex];
                    var bulletStatsComponent = bulletStats[entityIndex];

                    for (int i = 0; i < keys.Length; i++)
                    {
                        var key = keys[i];

                        if (NpcHashMapSingleton.NpcMultiHashMap.TryGetFirstValue(key, out var npcDataEntity, out var nativeMultiHashMapIterator))
                        {
                            do
                            {
                                bool canHit = bulletComponent.FactionType == FactionType.All || bulletComponent.FactionType != npcDataEntity.FactionType;

                                if (!canHit)
                                    continue;

                                float3 localNpcPosition = npcDataEntity.Position.Flat();

                                float distance = math.distance(localNpcPosition, bulletPosition);

                                if (distance <= npcDataEntity.ColliderRadius)
                                {
                                    PoolEntityUtils.DestroyEntity(in chunk, ref PooledEventTagHandle, entityIndex);

                                    var bulletHitDataComponent = new DamageHitData()
                                    {
                                        DamagedEntity = npcDataEntity.Entity,
                                        Damage = bulletStatsComponent.Damage,
                                        HitPosition = position,
                                        HitDirection = forward
                                    };

                                    Writer.Write(bulletHitDataComponent);
                                    hitted = true;

                                    break;
                                }

                            } while (NpcHashMapSingleton.NpcMultiHashMap.TryGetNextValue(out npcDataEntity, ref nativeMultiHashMapIterator));
                        }

                        if (hitted)
                        {
                            break;
                        }
                    }

                    keys.Dispose();
                }

                Writer.EndForEachIndex();
            }
        }
    }
}