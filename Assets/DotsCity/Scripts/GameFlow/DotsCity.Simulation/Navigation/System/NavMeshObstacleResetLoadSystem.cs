using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.NavMesh
{
    [UpdateInGroup(typeof(PreEarlyJobGroup))]
    [BurstCompile]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial struct NavMeshObstacleResetLoadSystem : ISystem
    {
        private EntityQuery inViewOfCameraGroup;
        private EntityQuery allGroup;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            inViewOfCameraGroup = SystemAPI.QueryBuilder()
                .WithDisabledRW<NavMeshObstacleLoadTag>()
                .WithAll<InViewOfCameraTag>()
                .WithAllRW<NavMeshObstacleData>()
                .Build();

            allGroup = SystemAPI.QueryBuilder()
                .WithDisabledRW<NavMeshObstacleLoadTag>()
                .WithAny<InViewOfCameraTag, InPermittedRangeTag>()
                .WithAllRW<NavMeshObstacleData>()
                .Build();

            state.RequireForUpdate(allGroup);
            state.RequireForUpdate<TrafficNavMeshLoaderConfigReference>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var navMeshLoaderConfig = SystemAPI.GetSingleton<TrafficNavMeshLoaderConfigReference>();

            var query = navMeshLoaderConfig.Config.Value.LoadOnlyInView ? inViewOfCameraGroup : allGroup;

            var navMeshObstacleResetLoadJob = new NavMeshObstacleResetLoadJob()
            {
                EntityTrackerLookup = SystemAPI.GetComponentLookup<EntityTrackerComponent>(true)
            };

            navMeshObstacleResetLoadJob.Schedule(query);
        }

        [BurstCompile]
        public partial struct NavMeshObstacleResetLoadJob : IJobEntity
        {
            [ReadOnly]
            public ComponentLookup<EntityTrackerComponent> EntityTrackerLookup;

            void Execute(
                ref NavMeshObstacleData navMeshObstacleData,
                EnabledRefRW<NavMeshObstacleLoadTag> navMeshObstacleLoadTagRW)
            {
                if (navMeshObstacleData.ObstacleEntity == Entity.Null || !EntityTrackerLookup.HasComponent(navMeshObstacleData.ObstacleEntity))
                {
                    navMeshObstacleData.ObstacleEntity = Entity.Null;
                    navMeshObstacleLoadTagRW.ValueRW = true;
                }
            }
        }
    }
}