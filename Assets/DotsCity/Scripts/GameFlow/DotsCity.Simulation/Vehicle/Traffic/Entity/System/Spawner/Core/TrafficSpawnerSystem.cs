using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Config;
using Spirit604.DotsCity.Simulation.Level.Streaming;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [UpdateInGroup(typeof(SpawnerGroup))]
    public partial class TrafficSpawnerSystem : EndInitSystemBase
    {
        private struct SpawnSettings
        {
            public float SumSpawnWeight;
        }

        private struct PrefabEntityData
        {
            public PrefabEntityData(
                Entity entity,
                BoundsComponent boundsComponent,
                TrafficGroupType trafficGroup,
                float weight,
                bool availableForSpawnByDefault,
                bool interpolation,
                float offset = 0)
            {
                PrefabEntity = entity;
                Bounds = new Bounds()
                {
                    center = boundsComponent.Center,
                    extents = boundsComponent.Extents
                };
                TrafficGroup = trafficGroup;
                Weight = weight;
                AvailableForSpawnByDefault = availableForSpawnByDefault;
                Interpolation = interpolation;
                OffsetY = offset;
            }

            public Entity PrefabEntity { get; private set; }
            public Bounds Bounds { get; private set; }
            public TrafficGroupType TrafficGroup { get; private set; }
            public float Weight { get; private set; }
            public bool AvailableForSpawnByDefault { get; private set; }
            public bool Interpolation { get; private set; }
            public float OffsetY { get; private set; }
        }

        private EntityQuery trafficGroup;
        private EntityQuery trafficNodeGroup;
        private EntityQuery permittedTrafficNodeGroup;
        private EntityQuery cullPointGroup;
        private EntityQuery inViewOfCameraTrafficNodeGroup;
        private EntityQuery parkingTrafficGroup;
        private EntityQuery trafficPrefabGroup;

        private int currentTrafficsCount;
        private EntityType entityType;

        private float nextSpawnTime;

        private SpawnSettings spawnSettings;
        private TrafficSpawnerConfigBlobReference spawnerConfig;
        private DetectObstacleMode trafficDetectObstacleMode;

        private NativeParallelHashMap<int, PrefabEntityData> prefabEntities;
        private NativeParallelMultiHashMap<int, SpawnJob.CarSpawnInfo> TempSpawnCarHashMap;

        public bool ForceDisable { get; set; }
        public int CurrentParkingCarsCount { get; private set; }
        public static bool IsInitialized { get; private set; }
        public bool InitialSpawned { get; private set; }
        public EntityQuery PermittedTrafficNodeGroup => permittedTrafficNodeGroup;

        public static Action OnConfigInitialized = delegate { };
        public static Action OnInitialized = delegate { };

        protected override void OnCreate()
        {
            base.OnCreate();

            trafficNodeGroup = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<
                    TrafficNodeComponent,
                    TrafficNodeSettingsComponent,
                    TrafficNodeCapacityComponent,
                    LocalToWorld>()
                .Build(this);

            cullPointGroup = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CullPointTag, LocalToWorld>()
                .Build(this);

            parkingTrafficGroup = new EntityQueryBuilder(Allocator.Temp)
                .WithNone<CulledEventTag>()
                .WithAll<TrafficTag, TrafficNodeLinkedComponent>()
                .Build(this);

            trafficGroup = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<TrafficDestinationComponent>()
                .Build(this);

            trafficPrefabGroup = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<TrafficPrefabData, TrafficPrefabSort>()
                .WithOptions(EntityQueryOptions.IncludePrefab)
                .Build(this);

            RequireForUpdate<TrafficNodeResolverSystem.RuntimePathDataRef>();
            RequireForUpdate<PathGraphSystem.Singleton>();

            Enabled = false;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Dispose();
        }

        protected override void OnUpdate()
        {
            if (!IsInitialized)
                return;

            currentTrafficsCount = trafficGroup.CalculateEntityCount();
            int spawnPointCount = trafficNodeGroup.CalculateEntityCount();

            if (SystemAPI.Time.ElapsedTime > nextSpawnTime)
            {
                int spawnCount = GetSpawnCount(false);
                CreateSpawnJob(spawnCount);
            }
        }

        private JobHandle CreateSpawnJob(int spawnCount, bool initialSpawn = false, bool userCreated = false)
        {
            TrafficSpawnParams customTrafficSpawnParams = new TrafficSpawnParams()
            {
                carModelIndex = -1,
                spawnNodeEntity = Entity.Null,
            };

            return CreateSpawnJob(spawnCount, initialSpawn, customTrafficSpawnParams, userCreated);
        }

        private JobHandle CreateSpawnJob(int spawnCount, bool initialSpawn, TrafficSpawnParams customTrafficSpawnParams, bool userCreated = false)
        {
            if (!IsInitialized)
            {
                UnityEngine.Debug.Log("TrafficSpawnerSystem. Trying to spawn but not initialized.");
            }

            if (spawnCount <= 0 || !TempSpawnCarHashMap.IsCreated)
            {
                return Dependency;
            }

            EntityQuery currentAvailableNodesQuery = GetNodeQuery(initialSpawn);

            if (currentAvailableNodesQuery.CalculateEntityCount() == 0)
            {
                return Dependency;
            }

            NativeArray<Entity> TrafficNodeAvailableEntities = currentAvailableNodesQuery.ToEntityArray(Allocator.TempJob);

            if (initialSpawn || !userCreated)
            {
                nextSpawnTime = GetNextRandomSpawnTime();
            }

            EntityCommandBuffer commandBuffer = default;

            if (!userCreated)
            {
                commandBuffer = GetCommandBuffer();
            }
            else
            {
                commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            }

            float3 cullPointPosition = cullPointGroup.GetSingleton<LocalToWorld>().Position;
            TempSpawnCarHashMap.Clear();

            CurrentParkingCarsCount = parkingTrafficGroup.CalculateEntityCount();

            NativeKeyValueArrays<int, PrefabEntityData> entitiesData = prefabEntities.GetKeyValueArrays(Allocator.TempJob);

            var spawnJob = new SpawnJob()
            {
                TrafficNodeAvailableEntities = TrafficNodeAvailableEntities,
                TrafficNodeEntities = trafficNodeGroup.ToEntityArray(Allocator.TempJob),
                EntitiesData = entitiesData,
                TrafficNodeLookup = SystemAPI.GetComponentLookup<TrafficNodeComponent>(true),
                TrafficNodeSettingsLookup = SystemAPI.GetComponentLookup<TrafficNodeSettingsComponent>(true),
                TrafficNodeCapacityLookup = SystemAPI.GetComponentLookup<TrafficNodeCapacityComponent>(true),
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                PathConnectionLookup = SystemAPI.GetBufferLookup<PathConnectionElement>(true),
                CommonGeneralSettingsData = SystemAPI.GetSingleton<CommonGeneralSettingsReference>(),
                Graph = SystemAPI.GetSingleton<PathGraphSystem.Singleton>(),
                CarHashMapSingleton = SystemAPI.GetSingleton<CarHashMapSystem.Singleton>(),
                RuntimePathDataRef = SystemAPI.GetSingleton<TrafficNodeResolverSystem.RuntimePathDataRef>(),
                TempSpawnCarHashMap = TempSpawnCarHashMap,
                TrafficSpawnerConfigBlobReference = spawnerConfig,
                TrafficCommonSettingsConfigBlobReference = SystemAPI.GetSingleton<TrafficCommonSettingsConfigBlobReference>(),
                SpawnSettings = spawnSettings,
                EntityType = entityType,
                trafficDetectObstacleMode = trafficDetectObstacleMode,
                CullSystemConfigReference = SystemAPI.GetSingleton<CullSystemConfigReference>(),
                CitySpawnConfigReference = SystemAPI.GetSingleton<CitySpawnConfigReference>(),
                SpawnCount = spawnCount,
                PlayerPosition = cullPointPosition,
                commandBuffer = commandBuffer,
                IsInitialSpawn = initialSpawn,
                trafficSpawnParams = customTrafficSpawnParams,
                RandomSeed = MathUtilMethods.GetRandomSeed(),
                UserCreated = userCreated,
                currentParkingCarsCount = CurrentParkingCarsCount,
            };

            var depJob = Dependency;
            var jobHandle = spawnJob.Schedule(depJob);

            if (!userCreated)
            {
                Dependency = jobHandle;
                EntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
            }
            else
            {
                jobHandle.Complete();
                commandBuffer.Playback(base.EntityManager);
                commandBuffer.Dispose();
            }

            return jobHandle;
        }

        public void AddCars(bool isInitialSpawn = false)
        {
            nextSpawnTime = GetNextRandomSpawnTime();
            int remainCars = spawnerConfig.Reference.Value.PreferableCount - currentTrafficsCount;
            int spawnCount = math.min(remainCars, spawnerConfig.Reference.Value.MaxSpawnCountByIteration);

            CreateSpawnJob(spawnCount, isInitialSpawn);
        }

        private void Dispose()
        {
            if (prefabEntities.IsCreated)
            {
                prefabEntities.Dispose();
            }

            if (TempSpawnCarHashMap.IsCreated)
            {
                TempSpawnCarHashMap.Dispose();
            }

            IsInitialized = false;
            Enabled = false;
            spawnSettings.SumSpawnWeight = 0;
        }

        private float GetNextRandomSpawnTime()
        {
            return (float)SystemAPI.Time.ElapsedTime + UnityEngine.Random.Range(spawnerConfig.Reference.Value.MinSpawnDelay, spawnerConfig.Reference.Value.MaxSpawnDelay);
        }

        public void Spawn(TrafficSpawnParams trafficSpawnParams, bool initialSpawn = false)
        {
            CreateSpawnJob(1, initialSpawn, trafficSpawnParams, true);
        }

        public void Spawn(int carModelIndex, Vector3 spawnPosition, Quaternion spawnRotation, float3 velocity, int currentHealth, bool hasDriver = false, bool hasStoppingEngine = false)
        {
            TrafficSpawnParams trafficSpawnParams = new TrafficSpawnParams(spawnPosition, spawnRotation)
            {
                carModelIndex = carModelIndex,
                velocity = velocity,
                hasDriver = hasDriver,
                hasStoppingEngine = hasStoppingEngine,
                customInitialHealth = currentHealth,
                customSpawnData = true
            };

            CreateSpawnJob(1, false, trafficSpawnParams, true);
        }

        public void Launch()
        {
            if (!ForceDisable)
            {
                Enabled = true;
                IsInitialized = true;
                InitialSpawn();
                OnInitialized();
            }
        }

        public void InitialSpawn()
        {
            int spawnCount = GetSpawnCount(true);
            var jobHandle = CreateSpawnJob(spawnCount, true, true);
            jobHandle.Complete();
            InitialSpawned = true;
        }

        private int GetSpawnCount(bool initialSpawn)
        {
            int maxCount = spawnerConfig.Reference.Value.PreferableCount;

            if (spawnerConfig.Reference.Value.MaxCarsPerNode > 0)
            {
                var nodeQuery = GetNodeQuery(initialSpawn);
                var currentMax = (int)(spawnerConfig.Reference.Value.MaxCarsPerNode * nodeQuery.CalculateEntityCount());

                maxCount = math.min(maxCount, currentMax);
            }

            int maxSpawnCountByIteration = !initialSpawn ? spawnerConfig.Reference.Value.MaxSpawnCountByIteration : int.MaxValue;

            return math.clamp(maxCount - currentTrafficsCount, 0, maxSpawnCountByIteration);
        }

        private EntityQuery GetNodeQuery(bool initialSpawn)
        {
            return initialSpawn ? inViewOfCameraTrafficNodeGroup : permittedTrafficNodeGroup;
        }
    }
}