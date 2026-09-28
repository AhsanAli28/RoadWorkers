using Spirit604.DotsCity.Core;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [UpdateInGroup(typeof(SpawnerGroup))]
    public partial class PedestrianEntitySpawnerSystem : SystemBase
    {
        #region Helper types & consts

        public struct CaptureNodeInfo
        {
            public Entity CapturedNodeEntity;
            public Entity PedestrianEntity;
            public PedestrianNodeType PedestrianNodeType;
        }

        #endregion

        #region Variables

        private EntityQuery spawnPointGroup;
        private EntityQuery inViewOfCameraSpawnPointGroup;
        private EntityQuery permittedSpawnPointGroup;
        private EntityQuery pedestrianGroup;
        private EndInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;

        private BlobAssetReference<PedestrianSpawnSettings> pedestrianSpawnSettings;
        private BlobAssetReference<TalkSpawnSettings> pedestrianTalkSpawnSettings;
        private BlobAssetReference<PedestrianSettings> pedestrianSettings;

        private NativeParallelHashMap<Entity, int> CapacityNodeHashMap;

        private NativeList<Entity> talkGroupEntities;
        private NativeList<float3> spawnPositions;
        private NativeList<int> talkGroupIndexes;

        private float nextSpawnTime;
        private int pedestrianCount;
        private float currentTime;
        private bool containerCreated;
        private NativeArray<Entity> currentAvailableSpawnPointEntities;
        private NativeHashSet<Entity> tempSpawnPointEntities;

        #endregion

        #region Public properties

        public bool ShouldSpawn
        {
            get
            {
                return ((pedestrianCount < pedestrianSpawnSettings.Value.MinPedestrianCount) && (currentTime > nextSpawnTime)) || ForceSpawnCount > 0;
            }
        }

        public int ForceSpawnCount { get; set; }

        public bool ForceDisable { get; set; }

        public bool IsInitialized { get; private set; }

        public bool InitialSpawned { get; private set; }

        #endregion

        #region Private properties

        private bool HasPrefabEntity => containerCreated;

        #endregion

        #region Static events

        public static event Action OnInitialized = delegate { };

        #endregion

        #region Unity lifecycle

        protected override void OnCreate()
        {
            base.OnCreate();

            #region Queries

            spawnPointGroup = GetSpawnpointGroup(EntityManager);

            pedestrianGroup = new EntityQueryBuilder(Allocator.Temp)
                .WithNone<TalkAreaComponent>()
                .WithAll<DestinationComponent>()
                .Build(this);

            #endregion

            m_EntityCommandBufferSystem = World.GetOrCreateSystemManaged<EndInitializationEntityCommandBufferSystem>();

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

            pedestrianCount = pedestrianGroup.CalculateEntityCount();
            currentTime = (float)SystemAPI.Time.ElapsedTime;

            if (ShouldSpawn)
            {
                SetNextSpawnTime();

                int remainSpawnCount = 0;

                if (ForceSpawnCount == 0)
                {
                    remainSpawnCount = GetSpawnCount();
                }
                else
                {
                    remainSpawnCount = ForceSpawnCount;
                }

                ForceSpawnCount = 0;

                Spawn(false, remainSpawnCount);
            }
        }

        #endregion

        #region Methods  

        public void Spawn(bool isInitialSpawn, int spawnCount = 1)
        {
            Spawn(isInitialSpawn, Entity.Null, spawnCount);
        }

        public void Spawn(bool isInitialSpawn, Entity spawnNodeEntity, int spawnCount = 1)
        {
            if (ForceDisable || spawnCount <= 0 || !HasPrefabEntity)
                return;

            var commandBuffer = !isInitialSpawn ? m_EntityCommandBufferSystem.CreateCommandBuffer() : new EntityCommandBuffer(Allocator.TempJob);

            InitializeAvailableEntities(isInitialSpawn);

            CapacityNodeHashMap.Clear();
            var CapacityNodeHashMapParallelWriterLocal = CapacityNodeHashMap.AsParallelWriter();

            JobHandle fillCapacityHashMapHandle = Entities
            .WithBurst()
            .WithNativeDisableContainerSafetyRestriction(CapacityNodeHashMapParallelWriterLocal)
            .WithAny<InPermittedRangeTag, InViewOfCameraTag>()
            .WithAll<NodeHasCapacityOptionTag>()
            .ForEach((
                Entity Entity,
                in NodeCapacityComponent pedestrianNodeCapacity) =>
            {
                CapacityNodeHashMapParallelWriterLocal.TryAdd(Entity, pedestrianNodeCapacity.CurrentCount);
            }).ScheduleParallel(this.Dependency);

            var nodeConnectionBuffer = SystemAPI.GetBufferLookup<NodeConnectionDataElement>(isReadOnly: true);
            NativeArray<uint> seeds = GetRandomSeeds(spawnCount);

            talkGroupEntities.Clear();
            spawnPositions.Clear();
            talkGroupIndexes.Clear();

            JobHandle spawnJobHandle = new SpawnJob
            {
                CommandBuffer = commandBuffer,
                SpawnCount = spawnCount,
                SpawnNodeEntity = spawnNodeEntity,
                PrefabContainer = SystemAPI.GetSingletonBuffer<PedestrianEntityPrefabContainer>(true),
                TalkSpawnSettings = pedestrianTalkSpawnSettings,
                PedestrianSettings = pedestrianSettings,

                AvailableSpawnPointEntities = currentAvailableSpawnPointEntities,
                WorldTransformLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true),
                NodeSettingsLookup = SystemAPI.GetComponentLookup<NodeSettingsComponent>(true),
                NodeCapacityLookup = SystemAPI.GetComponentLookup<NodeCapacityComponent>(true),
                NodeLightSettingsLookup = SystemAPI.GetComponentLookup<NodeLightSettingsComponent>(true),
                NodeConnectionBufferLookup = nodeConnectionBuffer,

                PedestrianTalkGroupEntities = talkGroupEntities,
                SpawnPositions = spawnPositions,
                GroupIndexes = talkGroupIndexes,
                TempSpawnPointEntities = tempSpawnPointEntities,

                Seeds = seeds,

                CapacityNodeHashMap = CapacityNodeHashMap,
            }.Schedule(fillCapacityHashMapHandle);

            JobHandle finalJob = default;

            var setTalkSettingsJob = new SetTalkSettingsJob()
            {
                commandBuffer = commandBuffer,
                pedestrianTalkSpawnSettings = pedestrianTalkSpawnSettings,
                currentTime = SystemAPI.Time.ElapsedTime,

                talkGroupEntities = talkGroupEntities,
                spawnPositions = spawnPositions,
                talkGroupIndexes = talkGroupIndexes,

                seeds = seeds
            }.Schedule(spawnJobHandle);

            finalJob = setTalkSettingsJob;

            seeds.Dispose(finalJob);

            if (isInitialSpawn)
            {
                finalJob.Complete();
                commandBuffer.Playback(EntityManager);
                commandBuffer.Dispose();
            }
            else
            {
                Dependency = finalJob;
                m_EntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
            }
        }

        public static EntityQuery GetSpawnpointGroup(EntityManager entityManager)
        {
            return new EntityQueryBuilder(Allocator.Temp)
                .WithNone<CustomSpawnerTag>()
                .WithAll<
                    NodeSettingsComponent,
                    NodeConnectionDataElement,
                    NodeCapacityComponent,
                    LocalToWorld>()
                .Build(entityManager);
        }

        public void InitialSpawn()
        {
            var spawnCount = GetSpawnCount();
            Spawn(true, spawnCount);
            SetNextSpawnTime();
            InitialSpawned = true;
        }

        private NativeArray<uint> GetRandomSeeds(int spawnCount)
        {
            var seeds = new NativeArray<uint>(spawnCount, Allocator.TempJob);

            for (int i = 0; i < seeds.Length; i++)
            {
                seeds[i] = MathUtilMethods.GetRandomSeed();
            }

            return seeds;
        }

        private void InitializeAvailableEntities(bool isInitialSpawn = false)
        {
            if (isInitialSpawn)
            {
                currentAvailableSpawnPointEntities = inViewOfCameraSpawnPointGroup.ToEntityArray(Allocator.TempJob);
            }
            else
            {
                currentAvailableSpawnPointEntities = permittedSpawnPointGroup.ToEntityArray(Allocator.TempJob);
            }
        }

        private void SetNextSpawnTime()
        {
            nextSpawnTime = currentTime + UnityEngine.Random.Range(pedestrianSpawnSettings.Value.MinSpawnDelay, pedestrianSpawnSettings.Value.MaxSpawnDelay);
        }

        private int GetSpawnCount()
        {
            int maxCount = pedestrianSpawnSettings.Value.MinPedestrianCount;

            if (pedestrianSpawnSettings.Value.MaxPedestrianPerNode > 0)
            {
                var currentMax = (int)(pedestrianSpawnSettings.Value.MaxPedestrianPerNode * spawnPointGroup.CalculateEntityCount());

                maxCount = math.min(maxCount, currentMax);
            }

            return math.clamp(maxCount - pedestrianCount, 0, int.MaxValue);
        }

        private void Dispose()
        {
            if (talkGroupEntities.IsCreated) talkGroupEntities.Dispose();
            if (spawnPositions.IsCreated) spawnPositions.Dispose();
            if (talkGroupIndexes.IsCreated) talkGroupIndexes.Dispose();
            if (CapacityNodeHashMap.IsCreated) CapacityNodeHashMap.Dispose();
            if (tempSpawnPointEntities.IsCreated) tempSpawnPointEntities.Dispose();
        }

        #endregion
    }
}
