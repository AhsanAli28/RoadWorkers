using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Traffic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.NavMesh
{
    [UpdateInGroup(typeof(MainThreadInitGroup))]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class NavMeshObstacleLoader : EndInitSystemBase
    {
        private NavMeshObstacleFactory navMeshObstacleFactory;
        private EntityQuery prefabQuery;
        private Entity prefabEntity;

        protected override void OnCreate()
        {
            base.OnCreate();

            prefabQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NavMeshObstaclePrefabTag>()
                .WithOptions(EntityQueryOptions.IncludePrefab)
                .Build(EntityManager);

            RequireForUpdate(prefabQuery);
            RequireForUpdate<NavMeshObstacleLoadTag>();
            Enabled = false;
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            prefabEntity = prefabQuery.GetSingletonEntity();
        }

        protected override void OnUpdate()
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;
            var commandBuffer = GetCommandBuffer();

            var navMeshLoaderConfig = SystemAPI.GetSingleton<TrafficNavMeshLoaderConfigReference>();

            if (navMeshLoaderConfig.Config.Value.LoadOnlyInView)
            {
                Entities
                .WithoutBurst()
                .WithStructuralChanges()
                .WithNone<InPermittedRangeTag>()
                .WithAll<NavMeshObstacleLoadTag>()
                .ForEach((
                    Entity entity,
                    ref NavMeshObstacleData navMeshObstacleData,
                    in LocalTransform transform) =>
                {
                    LoadNavmesh(ref commandBuffer, entity, ref navMeshObstacleData, in transform, in navMeshLoaderConfig, currentTime);
                }).Run();
            }
            else
            {
                Entities
                .WithoutBurst()
                .WithStructuralChanges()
                .WithAll<NavMeshObstacleLoadTag>()
                .ForEach((
                    Entity entity,
                    ref NavMeshObstacleData navMeshObstacleData,
                    in LocalTransform transform) =>
                {
                    LoadNavmesh(ref commandBuffer, entity, ref navMeshObstacleData, in transform, in navMeshLoaderConfig, currentTime);
                }).Run();
            }

            AddCommandBufferForProducer();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadNavmesh(
            ref EntityCommandBuffer commandBuffer,
            Entity entity,
            ref NavMeshObstacleData navMeshObstacleData,
            in LocalTransform transform,
            in TrafficNavMeshLoaderConfigReference navMeshLoaderConfig,
            float currentTime)
        {
            if ((currentTime - navMeshObstacleData.LoadTime) < navMeshLoaderConfig.Config.Value.LoadFrequency)
            {
                return;
            }

            if (navMeshObstacleData.ObstacleEntity != Entity.Null)
            {
                return;
            }

            navMeshObstacleData.LoadTime = currentTime;

            var navObstacle = navMeshObstacleFactory.Get();

            navObstacle.center = navMeshObstacleData.Center;

            var sizeOffset = navMeshLoaderConfig.Config.Value.SizeOffset;
            navObstacle.size = new Vector3(navMeshObstacleData.Size.x + sizeOffset, navMeshObstacleData.Size.y, navMeshObstacleData.Size.z + sizeOffset);
            navObstacle.carving = navMeshObstacleData.Carve;
            navObstacle.carvingMoveThreshold = navMeshObstacleData.MoveThreshold;
            navObstacle.carvingTimeToStationary = navMeshObstacleData.TimeToStationary;
            navObstacle.carveOnlyStationary = navMeshObstacleData.CarveOnlyStationary;

            var obstacleEntity = EntityManager.Instantiate(prefabEntity);

            navMeshObstacleData.ObstacleEntity = obstacleEntity;

            EntityManager.AddComponentObject(obstacleEntity, navObstacle.transform);
            EntityManager.SetComponentData(obstacleEntity, LocalTransform.FromPositionRotation(transform.Position, transform.Rotation));

            EntityManager.SetComponentData(obstacleEntity, new EntityTrackerComponent()
            {
                LinkedEntity = entity,
                TrackOnlyInView = navMeshLoaderConfig.Config.Value.LoadOnlyInView
            });

            commandBuffer.SetComponentEnabled<NavMeshObstacleLoadTag>(entity, false);
        }

        public void Initialize(NavMeshObstacleFactory navMeshObstacleFactory)
        {
            this.navMeshObstacleFactory = navMeshObstacleFactory;
            Enabled = true;
        }
    }
}