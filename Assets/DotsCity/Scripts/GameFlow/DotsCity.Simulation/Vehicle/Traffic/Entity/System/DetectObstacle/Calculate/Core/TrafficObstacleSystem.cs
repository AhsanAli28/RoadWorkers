using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.TrafficArea;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic.Obstacle
{
    [UpdateInGroup(typeof(TrafficSimulationGroup))]
    [BurstCompile]
    public partial struct TrafficObstacleSystem : ISystem, ISystemStartStop
    {
        #region Singleton

        public struct Singleton : IComponentData
        {
            public NativeParallelMultiHashMap<int, CarHashData> CarHashMap;
            public NativeParallelHashMap<Entity, CarHashData> CarObstacleHashMap;
        }

        #endregion

        #region Variables

        // Global path index / CarHashData
        private NativeParallelMultiHashMap<int, CarHashData> carHashMap;
        private NativeParallelMultiHashMap<int, CarChangeLaneEntityComponent> carChangeLaneHashMap;
        private NativeParallelHashMap<Entity, CarHashData> carObstacleHashMap;
        private bool changeLaneSupported;
        private EntityQuery defaultLaneCarQuery;
        private EntityQuery changeLaneCarQuery;
        private bool isInitialized;

        #endregion

        #region Unity lifecycle

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            defaultLaneCarQuery = SystemAPI.QueryBuilder()
                .WithAll<TrafficObstacleComponent>()
                .Build();

            var configQuery = SystemAPI.QueryBuilder()
                .WithAll<TrafficChangeLaneConfigReference>()
                .Build();

            changeLaneCarQuery = SystemAPI.QueryBuilder()
                .WithAll<TrafficChangingLaneEventTag>()
                .Build();

            state.RequireForUpdate(defaultLaneCarQuery);
            state.RequireForUpdate(configQuery);
        }

        [BurstDiscard]
        void ISystem.OnDestroy(ref SystemState state)
        {
            Dispose();
        }

        void ISystemStartStop.OnStartRunning(ref SystemState state)
        {
            Initialize(ref state);
        }

        void ISystemStartStop.OnStopRunning(ref SystemState state) { }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var carHashMapLocal = carHashMap;
            var carHashMapChangeLaneLocal = carChangeLaneHashMap;

            carHashMapLocal.Clear();
            carObstacleHashMap.Clear();

            int changeLaneCarCount = 0;

            if (changeLaneSupported)
            {
                carHashMapChangeLaneLocal.Clear();

                changeLaneCarCount = changeLaneCarQuery.CalculateEntityCount();

                if (carChangeLaneHashMap.Capacity < changeLaneCarCount)
                {
                    carChangeLaneHashMap.Capacity = changeLaneCarCount;
                }
            }

            int defaultTrafficCount = defaultLaneCarQuery.CalculateEntityCount();
            int obstacleEntitiesCount = defaultTrafficCount + changeLaneCarCount;

            if (carHashMapLocal.Capacity < obstacleEntitiesCount)
            {
                carHashMapLocal.Capacity = obstacleEntitiesCount;
            }

            NativeParallelMultiHashMap<int, CarHashData>.ParallelWriter carHashMapLocalParallel = carHashMapLocal.AsParallelWriter();
            NativeParallelMultiHashMap<int, CarChangeLaneEntityComponent>.ParallelWriter carHashMapChangeLaneLocalParallel = carHashMapChangeLaneLocal.AsParallelWriter();

            var trafficChangeLaneLookup = SystemAPI.GetComponentLookup<TrafficChangeLaneComponent>(true);
            var trafficChangingLaneEventLookup = SystemAPI.GetComponentLookup<TrafficChangingLaneEventTag>(true);
            var trafficAreaAlignedLookup = SystemAPI.GetComponentLookup<TrafficAreaAlignedTag>(true);

            var trafficCommonSettingsConfigBlobReference = SystemAPI.GetSingleton<TrafficCommonSettingsConfigBlobReference>();

            var fillHashMapJob = new FillHashMapJob()
            {
                CarHashMapParallel = carHashMapLocalParallel,
                CarHashMapObstacleParallel = carObstacleHashMap.AsParallelWriter(),
                CarHashMapChangeLaneParallel = carHashMapChangeLaneLocalParallel,
                TrafficChangeLaneLookup = trafficChangeLaneLookup,
                TrafficRaycastObstacleLookup = SystemAPI.GetComponentLookup<TrafficRaycastObstacleComponent>(true),
                TrafficChangingLaneEventLookup = trafficChangingLaneEventLookup,
                TrafficAreaAlignedLookup = trafficAreaAlignedLookup,
                TrafficNodeLinkedComponentLookup = SystemAPI.GetComponentLookup<TrafficNodeLinkedComponent>(true),
            };

            var fillHashMapJobHandle = fillHashMapJob.ScheduleParallel(state.Dependency);

            var obstacleCalcJob = new ObstacleCalcJob()
            {
                CarHashMapParallel = carHashMapLocal,
                TrafficChangeLaneLookup = trafficChangeLaneLookup,
                TrafficCarChangingLaneEventLookup = trafficChangingLaneEventLookup,
                TrafficAreaAlignedLookup = trafficAreaAlignedLookup,
                TrafficNodeLinkedComponentLookup = SystemAPI.GetComponentLookup<TrafficNodeLinkedComponent>(true),
                Graph = SystemAPI.GetSingleton<PathGraphSystem.Singleton>(),
                TrafficObstacleConfigReference = SystemAPI.GetSingleton<TrafficObstacleConfigReference>(),
                TrafficCommonSettingsConfigBlobReference = trafficCommonSettingsConfigBlobReference,
                TrafficAvoidanceConfigReference = SystemAPI.GetSingleton<TrafficAvoidanceConfigReference>(),
                ChangeLaneSupported = changeLaneSupported
            };

            var obstacleCalcJobHandle = obstacleCalcJob.ScheduleParallel(fillHashMapJobHandle);

            if (changeLaneSupported)
            {
                var changeLaneObstacleCalcJob = new ChangeLaneObstacleCalcJob()
                {
                    CarHashMap = carHashMapLocal,
                    CarHashMapChangeLane = carHashMapChangeLaneLocal,
                    TrafficObstacleConfigReference = SystemAPI.GetSingleton<TrafficObstacleConfigReference>(),
                };

                state.Dependency = changeLaneObstacleCalcJob.ScheduleParallel(obstacleCalcJobHandle);
            }
            else
            {
                state.Dependency = obstacleCalcJobHandle;
            }
        }

        #endregion

        #region Initialization

        public void Initialize(ref SystemState state)
        {
            if (!isInitialized)
            {
                isInitialized = true;

                var trafficSpawnerConfig = SystemAPI.GetSingleton<TrafficSpawnerConfigBlobReference>().Reference.Value;

                if (!carHashMap.IsCreated)
                {
                    carHashMap = new NativeParallelMultiHashMap<int, CarHashData>(trafficSpawnerConfig.HashMapCapacity, Allocator.Persistent);
                    carObstacleHashMap = new NativeParallelHashMap<Entity, CarHashData>(trafficSpawnerConfig.HashMapCapacity, Allocator.Persistent);
                }

                UpdateLaneConfig();

                state.EntityManager.AddComponentData(state.SystemHandle, new Singleton()
                {
                    CarHashMap = carHashMap,
                    CarObstacleHashMap = carObstacleHashMap
                });
            }
        }

        public void UpdateLaneConfig()
        {
            var trafficGeneralSettingsReference = SystemAPI.GetSingleton<TrafficGeneralSettingsReference>().Config.Value;
            var trafficChangeLaneConfig = SystemAPI.GetSingleton<TrafficChangeLaneConfigReference>().Config.Value;

            changeLaneSupported = trafficGeneralSettingsReference.ChangeLaneSupport;

            if (changeLaneSupported)
            {
                if (!carChangeLaneHashMap.IsCreated)
                {
                    carChangeLaneHashMap = new NativeParallelMultiHashMap<int, CarChangeLaneEntityComponent>(trafficChangeLaneConfig.ChangeLaneHashMapCapacity, Allocator.Persistent);
                }
            }
            else
            {
                if (carChangeLaneHashMap.IsCreated)
                {
                    carChangeLaneHashMap.Dispose();
                }
            }
        }

        private void Dispose()
        {
            if (carHashMap.IsCreated)
            {
                carHashMap.Dispose();
            }

            if (carChangeLaneHashMap.IsCreated)
            {
                carChangeLaneHashMap.Dispose();
            }

            if (carObstacleHashMap.IsCreated)
            {
                carObstacleHashMap.Dispose();
            }

            isInitialized = false;
        }

        #endregion
    }
}