using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.Traffic;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Spirit604.DotsCity.Debug
{
    [UpdateInGroup(typeof(InitGroup))]
    public partial class TrafficRoadSpawnDebuggerSystem : BeginInitSystemBase
    {
        public const float HashCellSize = 0.25f;

        private class SpawnInfo
        {
            public int Index;
            public float SpawnTimeStamp;

            public SpawnInfo(int index, float spawnTimeStamp)
            {
                Index = index;
                SpawnTimeStamp = spawnTimeStamp;
            }
        }

        private NativeHashMap<int, Entity> hashContainer;
        private bool initialized;
        private bool registered;
        private PathGraphSystem.Singleton graph;

        private TrafficSpawnerSystem trafficSpawnerSystem;
        private Dictionary<Entity, Queue<int>> currentIndexes = new Dictionary<Entity, Queue<int>>();

        protected override void OnCreate()
        {
            base.OnCreate();
            trafficSpawnerSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<TrafficSpawnerSystem>();
            RequireForUpdate<SpawnCarDelayData>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (hashContainer.IsCreated)
            {
                hashContainer.Dispose();
            }

            currentIndexes.Clear();
        }

        protected override void OnUpdate()
        {
            var commandBuffer = GetCommandBuffer();

            var time = (float)SystemAPI.Time.ElapsedTime;

            Entities
            .WithoutBurst()
            .ForEach((
                Entity entity,
                ref DynamicBuffer<SpawnCarDelayData> spawnCarDelayDatas,
                in DynamicBuffer<DebugRoadLaneElement> lanes) =>
            {
                int index = 0;

                while (index < spawnCarDelayDatas.Length)
                {
                    var spawnCarDelayData = spawnCarDelayDatas[index];
                    bool shouldSpawn = time >= spawnCarDelayData.SpawnTimestamp;

                    if (shouldSpawn)
                    {
                        var hash = GetHash(entity);
                        Spawn(entity, spawnCarDelayData.Index, lanes[spawnCarDelayData.Index], hash);
                        spawnCarDelayDatas.RemoveAt(index);
                    }
                    else
                    {
                        index++;
                    }
                }

                if (spawnCarDelayDatas.Length == 0)
                {
                    commandBuffer.RemoveComponent<SpawnCarDelayData>(entity);
                }
            }).Run();

            AddCommandBufferForProducer();
        }

        public void AddDebugger(Entity entity, bool allowOverwrite = false)
        {
            if (!registered)
            {
                registered = true;
                DefaultWorldUtils.CreateAndAddSystemManaged<TrafficRoadDebuggerCleanupSystem, CleanupGroup>();
            }

            if (!hashContainer.IsCreated)
            {
                hashContainer = new NativeHashMap<int, Entity>(100, Allocator.Persistent);
            }

            var hash = GetHash(entity);

            if (!hashContainer.ContainsKey(hash))
            {
                hashContainer.Add(hash, entity);
            }
            else
            {
                if (!allowOverwrite)
                {
                    var prevEntity = hashContainer[hash];
                    UnityEngine.Debug.Log($"TrafficRoadSpawnDebuggerSystem. Hash {hash} overwrite found. Prev entity {prevEntity.Index} New entity {entity.Index}. Make sure the debugger has a unique position");
                }
                else
                {
                    if (currentIndexes.ContainsKey(hashContainer[hash]))
                    {
                        currentIndexes.Remove(hashContainer[hash]);
                    }

                    hashContainer[hash] = entity;
                }
            }

            currentIndexes.Add(entity, new Queue<int>());
        }

        public void RemoveDebugger(Entity entity)
        {
            currentIndexes.Remove(entity);
        }

        public void Spawn(int hash)
        {
            if (hashContainer.TryGetValue(hash, out var entity))
            {
                Spawn(entity);
            }
            else
            {
                UnityEngine.Debug.Log($"TrafficRoadSpawnDebuggerSystem. Trying to spawn. Hash {hash} not found");
            }
        }

        public void Spawn(Entity entity, int carModel = -1)
        {
            if (!initialized)
            {
                graph = EntityManager.CreateEntityQuery(typeof(PathGraphSystem.Singleton)).GetSingleton<PathGraphSystem.Singleton>();
            }

            var lanes = EntityManager.GetBuffer<DebugRoadLaneElement>(entity);
            var hash = GetHash(entity);
            var count = lanes.Length;

            for (int i = 0; i < count; i++)
            {
                lanes = EntityManager.GetBuffer<DebugRoadLaneElement>(entity);

                if (lanes[i].SpawnDelay > 0)
                {
                    float timeStamp = (float)SystemAPI.Time.ElapsedTime + lanes[i].SpawnDelay;

                    DynamicBuffer<SpawnCarDelayData> delayBuffer = default;

                    if (EntityManager.HasBuffer<SpawnCarDelayData>(entity))
                    {
                        delayBuffer = EntityManager.GetBuffer<SpawnCarDelayData>(entity);
                    }
                    else
                    {
                        delayBuffer = EntityManager.AddBuffer<SpawnCarDelayData>(entity);
                    }

                    delayBuffer.Add(new SpawnCarDelayData()
                    {
                        Index = i,
                        SpawnTimestamp = timeStamp,
                    });
                }
                else
                {
                    Spawn(entity, i, lanes[i], hash, carModel);
                }
            }
        }

        public Entity GetDebugger(int hash)
        {
            if (hashContainer.TryGetValue(hash, out var debuggerEntity))
            {
                return debuggerEntity;
            }

            return default;
        }

        public void AddCar(ref EntityCommandBuffer commandBuffer, Entity carEntity, int hashID)
        {
            if (hashContainer.TryGetValue(hashID, out var debuggerEntity))
            {
                var spawnedCarDataElement = GetSpawnedCars(debuggerEntity);
                var trafficRoadDebuggerInfo = EntityManager.GetComponentData<TrafficRoadDebuggerInfo>(debuggerEntity);

                spawnedCarDataElement.Add(new SpawnedCarDataElement()
                {
                    CarEntity = carEntity,
                });

                if (trafficRoadDebuggerInfo.DisableLaneChanging)
                {
                    if (EntityManager.HasComponent<TrafficChangeLaneComponent>(carEntity))
                    {
                        commandBuffer.RemoveComponent<TrafficChangeLaneComponent>(carEntity);
                    }
                }

                var queue = currentIndexes[debuggerEntity];

                if (queue.Count > 0)
                {
                    var index = queue.Dequeue();
                    var lanes = EntityManager.GetBuffer<DebugRoadLaneElement>(debuggerEntity);

                    if (lanes[index].IdleCar)
                    {
                        var trafficStateComponent = EntityManager.GetComponentData<TrafficStateComponent>(carEntity);
                        TrafficStateExtension.AddIdleState(ref commandBuffer, carEntity, ref trafficStateComponent, TrafficIdleState.UserCreated);
                        commandBuffer.SetComponentEnabled<TrafficIdleTag>(carEntity, true);
                        commandBuffer.SetComponent(carEntity, trafficStateComponent);
                    }
                }
                else
                {
                    UnityEngine.Debug.Log($"TrafficRoadSpawnDebuggerSystem. Trying to dequeue debugger entity {debuggerEntity.Index} hash {hashID}, but its empty.");
                }
            }
            else
            {
                UnityEngine.Debug.Log($"TrafficRoadSpawnDebuggerSystem. Hash {hashID} not found");
            }
        }

        public void Clear(Entity debuggerEntity)
        {
            var spawnedCarDataElement = EntityManager.GetBuffer<SpawnedCarDataElement>(debuggerEntity);

            NativeList<Entity> destroyList = new NativeList<Entity>(Allocator.TempJob);
            for (int i = 0; i < spawnedCarDataElement.Length; i++)
            {
                var carEntity = spawnedCarDataElement[i].CarEntity;

                if (EntityManager.HasComponent<PoolableTag>(carEntity))
                {
                    destroyList.Add(carEntity);
                }
            }

            spawnedCarDataElement.Clear();

            if (destroyList.Length > 0)
            {
                var destroyArray = destroyList.ToArray(Allocator.TempJob);

                var entityManager = EntityManager;
                PoolEntityUtils.DestroyEntity(ref entityManager, destroyArray);
                destroyArray.Dispose();
            }

            destroyList.Dispose();
        }

        public DynamicBuffer<SpawnedCarDataElement> GetSpawnedCars(Entity debuggerEntity) => EntityManager.GetBuffer<SpawnedCarDataElement>(debuggerEntity);

        public int GetHash(Entity entity)
        {
            var pos = EntityManager.GetComponentData<LocalTransform>(entity).Position;
            return GetHash(pos);
        }

        public static int GetHash(float3 pos) => HashMapHelper.GetHashMapPosition(pos, cellRadius: HashCellSize);

        public static int GetHash(TrafficRoadDebugger debugger) => GetHash(debugger.transform.position);

        private void Spawn(Entity debuggerEntity, int index, DebugRoadLaneElement lane, int hash, int carModel = -1)
        {
            var sourceNodeEntity = lane.TrafficNodeEntity;

            if (sourceNodeEntity == Entity.Null)
                return;

            currentIndexes[debuggerEntity].Enqueue(index);

            if (carModel == -1)
            {
                carModel = lane.SpawnCarModel;
            }

            TrafficSpawnParams trafficSpawnParams = TrafficSpawnUtils.GetSpawnParams(in graph, EntityManager, sourceNodeEntity, carModel, TrafficCustomInitType.RoadSegmentDebug, lane.LocalPathIndex, lane.NormalizedPathPosition);
            trafficSpawnParams.customInitIndex = hash;

            trafficSpawnerSystem.Spawn(trafficSpawnParams, true);
        }
    }
}
