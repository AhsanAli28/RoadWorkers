using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Npc;
using Spirit604.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateAfter(typeof(NpcHashMapSystem))]
    [UpdateInGroup(typeof(HashMapGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct TrafficNpcCalculateObstacleSystem : ISystem
#if UNITY_EDITOR
        , ISystemStartStop
#endif
    {

#if UNITY_EDITOR
        private const int MaxEntityCount = 200;

        public struct NpcObstacleInfo
        {
            public Entity Entity;
            public float3 Position;
            public int IsObstacle;
        }

        public struct AreaInfo
        {
            public Vector3 LeftTopPoint;
            public Vector3 LeftTopPoint2;
            public Vector3 RightTopPoint;
            public Vector3 RightTopPoint2;
        }

        private NativeParallelMultiHashMap<Entity, NpcObstacleInfo> debugObstacleNpcMultiHashMap;
        private NativeParallelHashMap<Entity, AreaInfo> debugAreaInfo;
        private EntityQuery debugEntityQuery;

        public static NativeParallelMultiHashMap<Entity, NpcObstacleInfo> DebugObstacleNpcMultiHashMapStaticRef { get; private set; }
        public static NativeParallelHashMap<Entity, AreaInfo> DebugAreaInfoStaticRef { get; private set; }
#endif

        private SystemHandle npcHashMapSystem;
        private EntityQuery updateQuery;

        void ISystem.OnCreate(ref SystemState state)
        {
            npcHashMapSystem = state.WorldUnmanaged.GetExistingUnmanagedSystem<NpcHashMapSystem>();

            updateQuery = SystemAPI.QueryBuilder()
                .WithAll<TrafficNpcObstacleComponent>()
                .Build();

            var configQuery = SystemAPI.QueryBuilder()
                .WithAll<TrafficSpawnerConfigBlobReference>()
                .Build();

#if UNITY_EDITOR

            debugEntityQuery = SystemAPI.QueryBuilder()
                .WithAll<TrafficNpcObstacleConfigDebug>()
                .Build();
#endif

            state.RequireForUpdate(updateQuery);
            state.RequireForUpdate(configQuery);
            state.RequireForUpdate<NpcHashMapSystem.Singleton>();
        }

#if UNITY_EDITOR
        void ISystem.OnDestroy(ref SystemState state)
        {
            if (debugObstacleNpcMultiHashMap.IsCreated)
            {
                debugObstacleNpcMultiHashMap.Dispose();
            }

            if (debugAreaInfo.IsCreated)
            {
                debugAreaInfo.Dispose();
            }
        }

        void ISystemStartStop.OnStartRunning(ref SystemState state)
        {
            if (debugObstacleNpcMultiHashMap.IsCreated)
                return;

            var trafficSpawnerConfig = SystemAPI.GetSingleton<TrafficSpawnerConfigBlobReference>().Reference;

            debugObstacleNpcMultiHashMap = new NativeParallelMultiHashMap<Entity, NpcObstacleInfo>(trafficSpawnerConfig.Value.HashMapCapacity * 50, Allocator.Persistent);
            debugAreaInfo = new NativeParallelHashMap<Entity, AreaInfo>(trafficSpawnerConfig.Value.HashMapCapacity * 50, Allocator.Persistent);

            DebugObstacleNpcMultiHashMapStaticRef = debugObstacleNpcMultiHashMap;
            DebugAreaInfoStaticRef = debugAreaInfo;
        }

        void ISystemStartStop.OnStopRunning(ref SystemState state) { }
#endif

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
#if UNITY_EDITOR
            if (!debugObstacleNpcMultiHashMap.IsCreated)
                return;

            debugObstacleNpcMultiHashMap.Clear();
            debugAreaInfo.Clear();
#endif

            ref var npcHashMapSystemRef = ref state.WorldUnmanaged.ResolveSystemStateRef(npcHashMapSystem);

            var calcNpcObstacleJob = new CalcNpcObstacleJob()
            {
                NpcHashMapSingleton = SystemAPI.GetSingleton<NpcHashMapSystem.Singleton>(),
                TrafficNpcObstacleConfigReference = SystemAPI.GetSingleton<TrafficNpcObstacleConfigReference>(),

#if UNITY_EDITOR
                DebugObstacleNpcMultiHashMap = debugObstacleNpcMultiHashMap.AsParallelWriter(),
                DebugAreaInfo = debugAreaInfo.AsParallelWriter(),
                RecordDebug = debugEntityQuery.CalculateEntityCount() > 0 && updateQuery.CalculateEntityCount() < MaxEntityCount
#endif
            };

            state.Dependency = calcNpcObstacleJob.ScheduleParallel(npcHashMapSystemRef.Dependency);
        }

        [WithAll(typeof(HasDriverTag))]
        [BurstCompile]
        public partial struct CalcNpcObstacleJob : IJobEntity
        {
            [ReadOnly]
            public NpcHashMapSystem.Singleton NpcHashMapSingleton;

            [ReadOnly]
            public TrafficNpcObstacleConfigReference TrafficNpcObstacleConfigReference;

#if UNITY_EDITOR
            [NativeDisableContainerSafetyRestriction]
            public NativeParallelMultiHashMap<Entity, NpcObstacleInfo>.ParallelWriter DebugObstacleNpcMultiHashMap;

            [NativeDisableContainerSafetyRestriction]
            public NativeParallelHashMap<Entity, AreaInfo>.ParallelWriter DebugAreaInfo;

            [ReadOnly]
            public bool RecordDebug;
#endif

            void Execute(
                Entity entity,
                ref TrafficNpcObstacleComponent npcObstacleComponent,
                in BoundsComponent boundsComponent,
                in LocalTransform transform)
            {
                var localCarPosition = transform.Position.Flat();
                var carRotation = transform.Rotation;
                var size = boundsComponent.Extents;

                bool hasObstacle = false;

                var leftTopPoint = (localCarPosition + math.mul(carRotation, new float3(-size.x - TrafficNpcObstacleConfigReference.Config.Value.SideOffsetX, 0, size.z * TrafficNpcObstacleConfigReference.Config.Value.RateOffsetZ))).Flat();
                var rightTopPoint = (localCarPosition + math.mul(carRotation, new float3(size.x + TrafficNpcObstacleConfigReference.Config.Value.SideOffsetX, 0, size.z * TrafficNpcObstacleConfigReference.Config.Value.RateOffsetZ))).Flat();

                var forward = transform.Forward();
                var leftTopPoint2 = (leftTopPoint + forward * TrafficNpcObstacleConfigReference.Config.Value.SquareLength).Flat();
                var rightTopPoint2 = (rightTopPoint + forward * TrafficNpcObstacleConfigReference.Config.Value.SquareLength).Flat();

#if UNITY_EDITOR

                bool recordDebug = RecordDebug;

                if (recordDebug)
                {
                    DebugAreaInfo.TryAdd(entity, new AreaInfo()
                    {
                        LeftTopPoint = leftTopPoint,
                        LeftTopPoint2 = leftTopPoint2,
                        RightTopPoint = rightTopPoint,
                        RightTopPoint2 = rightTopPoint2,
                    });
                }
#endif

                NativeList<int> npcKeys = new NativeList<int>(5, Allocator.Temp);

                Vector3 center = (rightTopPoint2 + leftTopPoint) / 2;
                TryToAddKey(ref npcKeys, center);
                TryToAddKey(ref npcKeys, leftTopPoint);
                TryToAddKey(ref npcKeys, rightTopPoint);
                TryToAddKey(ref npcKeys, leftTopPoint2);
                TryToAddKey(ref npcKeys, rightTopPoint2);

                for (int i = 0; i < npcKeys.Length; i++)
                {
                    if (NpcHashMapSingleton.NpcMultiHashMap.TryGetFirstValue(npcKeys[i], out var npcHashEntity, out var nativeMultiHashMapIterator))
                    {
                        do
                        {
                            if (!npcHashEntity.IsObstacle)
                                continue;

                            float3 npcPosition = npcHashEntity.Position;
                            float3 npcPositionFlat = npcPosition.Flat();

                            float distance = math.distancesq(npcPositionFlat, localCarPosition);

#if UNITY_EDITOR
                            int isObstacle = 0;
#endif

                            if (distance < TrafficNpcObstacleConfigReference.Config.Value.CheckDistanceSQ)
                            {

                                bool tempHasObstacle = false;

                                float yDiff = math.abs(npcPosition.y - transform.Position.y);

                                if (yDiff < TrafficNpcObstacleConfigReference.Config.Value.MaxYDiff)
                                {
                                    tempHasObstacle = VectorExtensions.PointInRectangle3D(leftTopPoint, rightTopPoint, rightTopPoint2, leftTopPoint2, npcPositionFlat);
                                }

                                if (!hasObstacle)
                                {
                                    hasObstacle = tempHasObstacle;
                                }
#if !UNITY_EDITOR
                                if (tempHasObstacle)
                                {
                                    break;
                                }
#else
                                if (recordDebug)
                                {
                                    isObstacle = tempHasObstacle ? 1 : 0;
                                }
                                else
                                {
                                    if (tempHasObstacle)
                                    {
                                        break;
                                    }
                                }
#endif
                            }

#if UNITY_EDITOR
                            if (recordDebug)
                            {
                                DebugObstacleNpcMultiHashMap.Add(entity, new NpcObstacleInfo()
                                {
                                    Entity = npcHashEntity.Entity,
                                    Position = npcPosition,
                                    IsObstacle = isObstacle
                                });
                            }
#endif

                        } while (NpcHashMapSingleton.NpcMultiHashMap.TryGetNextValue(out npcHashEntity, ref nativeMultiHashMapIterator));
                    }

#if !UNITY_EDITOR
                    if (hasObstacle)
                    {
                        break;
                    }
#else
                    if (!recordDebug && hasObstacle)
                    {
                        break;
                    }
#endif
                }

                npcKeys.Dispose();

                npcObstacleComponent.HasObstacle = hasObstacle;
            }
        }

        private static void TryToAddKey(ref NativeList<int> keys, float3 position)
        {
            var key = HashMapHelper.GetHashMapPosition(position);

            if (!keys.Contains(key))
            {
                keys.Add(key);
            }
        }
    }
}