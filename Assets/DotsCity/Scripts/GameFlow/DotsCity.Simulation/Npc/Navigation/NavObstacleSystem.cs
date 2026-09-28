//using System.Collections.Generic;
//using UnityEngine;
//using Unity.Entities;
//using UnityEngine.AI;
//using Unity.Transforms;
//using Unity.Collections;
//using Unity.Burst;
//using Unity.Burst.Intrinsics;

//namespace Spirit604.DotsCity.Simulation.Npc.Navigation
//{
//    public partial class NavObstacleSystem : SystemBase
//    {
//        private Bounds bounds;
//        private EntityQuery obstacleQuery;
//        private NativeQueue<NavMeshBuildSource> obstacleQueue;

//        private List<NavMeshBuildSource> sources;
//        private int agentId = 0;
//        private int areaIndex = 0;
//        private bool updating;
//        private NavMeshData navMeshData;
//        private NavMeshBuildSettings defaultBuildSettings;

//        protected override void OnCreate()
//        {
//            base.OnCreate();

//            obstacleQueue = new NativeQueue<NavMeshBuildSource>(Allocator.Persistent);
//            obstacleQuery = new EntityQueryBuilder(Allocator.Temp)
//                .WithAll<LocalToWorld, NavObstacleComponent>()
//                .Build(this);

//            sources = new List<NavMeshBuildSource>();

//            defaultBuildSettings = NavMesh.GetSettingsByID(agentId);
//            defaultBuildSettings.overrideVoxelSize = true;
//            defaultBuildSettings.voxelSize = 0.25f;
//            defaultBuildSettings.overrideTileSize = true;
//            defaultBuildSettings.tileSize = 64;

//            this.bounds = new Bounds(Vector3.zero, new Vector3(1024, 128, 1024));
//            navMeshData = NavMeshBuilder.BuildNavMeshData(defaultBuildSettings, new List<NavMeshBuildSource>(), bounds, Vector3.zero, Quaternion.identity);
//            NavMesh.AddNavMeshData(navMeshData);

//            RequireForUpdate(obstacleQuery);
//        }

//        protected override void OnDestroy()
//        {
//            base.OnDestroy();
//            obstacleQueue.Dispose();
//            sources.Clear();
//            sources = null;
//        }

//        protected override void OnUpdate()
//        {
//            if (updating)
//            {
//                return;
//            }

//            obstacleQueue.Clear();

//            var fillJob = new FillObtacleJob()
//            {
//                LocalToWorldTypeHandle = GetComponentTypeHandle<LocalToWorld>(true),
//                NavObstacleComponentTypeHandle = GetComponentTypeHandle<NavObstacleComponent>(true),
//                Queue = obstacleQueue.AsParallelWriter(),
//                AreaIndex = areaIndex
//            }.ScheduleParallel(obstacleQuery, Dependency);

//            fillJob.Complete();

//            sources.Clear();

//            while (this.obstacleQueue.TryDequeue(out var obstacleData))
//            {
//                if (obstacleData.shape == NavMeshBuildSourceShape.Box)
//                {
//                    sources.Add(obstacleData);
//                }
//            }

//            updating = true;

//            var asyncOperation = NavMeshBuilder.UpdateNavMeshDataAsync(navMeshData, defaultBuildSettings, sources, bounds);
//            asyncOperation.completed += UpdateNavMeshData_Completed;
//        }

//        private void UpdateNavMeshData_Completed(AsyncOperation obj)
//        {
//            if (obj.isDone)
//            {
//                updating = false;
//            }
//        }

//        [BurstCompile]
//        private struct FillObtacleJob : IJobChunk
//        {
//            [ReadOnly] public ComponentTypeHandle<LocalToWorld> LocalToWorldTypeHandle;
//            [ReadOnly] public ComponentTypeHandle<NavObstacleComponent> NavObstacleComponentTypeHandle;
//            [ReadOnly] public int AreaIndex;
//            [NativeDisableParallelForRestriction] public NativeQueue<NavMeshBuildSource>.ParallelWriter Queue;

//            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
//            {
//                var localToWorlds = chunk.GetNativeArray(ref LocalToWorldTypeHandle);
//                var navObstacles = chunk.GetNativeArray(ref NavObstacleComponentTypeHandle);

//                for (var index = 0; index < chunk.Count; index++)
//                {
//                    var localToWorld = localToWorlds[index];
//                    var navObstacle = navObstacles[index];

//                    Queue.Enqueue(new NavMeshBuildSource()
//                    {
//                        transform = localToWorld.Value,
//                        size = navObstacle.Size,
//                        shape = NavMeshBuildSourceShape.Box,
//                        area = AreaIndex,
//                    });
//                }
//            }
//        }
//    }
//}
