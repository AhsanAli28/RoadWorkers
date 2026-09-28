using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Spirit604.DotsCity.Debug
{
    public class TrafficRoadDebugger : MonoBehaviour
    {
        [Serializable]
        public class TrafficSpawnTestInfo
        {
            public TrafficLightCrossroad RelatedTrafficLightCrossroad;
            public Path Path;
            public bool HighLight = true;
            public bool ShowInfo = true;
            public bool IdleCar;
            [Range(0, 1f)] public float NormalizedPathPosition;
            [Range(0, 10f)] public float SpawnDelay;

            // For null path resolving indexes
            [HideInInspector] public int TrafficNodeIndex = -1;
            [HideInInspector] public int PathLaneIndex = -1;
            [HideInInspector] public int PathIndexInLane = -1;
        }

        private const float ViewPortOffset = 0.6f;

        public VehicleDataCollection vehicleDataCollection;
        public bool enableVisualDebug = true;
        public Color fontColor = Color.white;
        public bool spawnOnPlay;
        public bool spawnOnView;
        public bool autoClearOnSpawn;
        public int spawnCarModel;
        public bool showButtons = true;
        public bool disableLaneChanging = true;
        public bool highlightPathAfterAdd = true;
        public TrafficDebugMode TrafficDebugMode;
        public List<TrafficSpawnTestInfo> TrafficSpawnTestInfos = new List<TrafficSpawnTestInfo>();
        public bool customDescription;
        [TextArea] public string description;
        public bool editorInitialized;
        public int debuggerHash;
        private EntityManager entityManager;
        private TrafficRoadSpawnDebuggerSystem trafficRoadSpawnDebuggerSystem;

        private bool spawned;
        private bool subscribed;
        private Entity debuggerEntity;
        private NativeArray<DebugRoadLaneElement> laneBuffer;

        public int RuntimeCarModel { get; set; }
        public bool IsInitialized { get; private set; }

        public void Init(Entity entity)
        {
            Initialize();
            debuggerEntity = entity;
            debuggerHash = trafficRoadSpawnDebuggerSystem.GetHash(entity);

            if (laneBuffer.IsCreated)
            {
                laneBuffer.Dispose();
            }

            laneBuffer = entityManager.GetBuffer<DebugRoadLaneElement>(entity).ToNativeArray(Allocator.Persistent);

            if (laneBuffer.Length > 0)
                RuntimeCarModel = laneBuffer[0].SpawnCarModel;
        }

        public void Spawn()
        {
            var spawnedCars = GetSpawnedCars();

            if (autoClearOnSpawn && spawnedCars.Length > 0)
            {
                StartCoroutine(SpawnClearRoutine());
            }
            else
            {
                SpawnInternal();
            }
        }

        public static void Spawn(int hash)
        {
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<TrafficRoadSpawnDebuggerSystem>().Spawn(hash);
        }

        public void Clear()
        {
            trafficRoadSpawnDebuggerSystem.Clear(debuggerEntity);
        }

        public void AddPath(Path path)
        {
            GetPathIndexes(path, out var trafficNodeIndex, out var laneIndex, out var localPathIndex);

            TrafficLightCrossroad trafficLightCrossroad = null;

            if (path && path.SourceTrafficNode)
            {
                trafficLightCrossroad = path.SourceTrafficNode.TrafficLightCrossroad;
            }

            TrafficSpawnTestInfos.Add(new TrafficSpawnTestInfo()
            {
                RelatedTrafficLightCrossroad = trafficLightCrossroad,
                Path = path,
                TrafficNodeIndex = trafficNodeIndex,
                PathLaneIndex = laneIndex,
                PathIndexInLane = localPathIndex,
                HighLight = highlightPathAfterAdd
            });

#if UNITY_EDITOR
            path.Highlighted = highlightPathAfterAdd;
#endif
        }

        public Vector3 GetSpawnData(Path path, float normalizedLength, ref int targetWaypointIndex, ref Quaternion spawnRotation)
        {
            float pathLength = path.GetPathLength();
            float currentDistance = 0;
            float prevCurrentDistance = 0;

            for (int i = 0; i < path.WayPoints.Count - 1; i++)
            {
                Vector3 A1point = path.WayPoints[i].transform.position;
                Vector3 A2point = path.WayPoints[i + 1].transform.position;

                currentDistance += Vector3.Distance(A1point, A2point);

                if (currentDistance >= normalizedLength * pathLength)
                {
                    var offsetDistance = normalizedLength * pathLength - prevCurrentDistance;

                    var offsetPoint = A1point + (A2point - A1point).normalized * offsetDistance;

                    var dir = !path.WayPoints[i].BackwardDirection ? A2point - A1point : A1point - A2point;

                    spawnRotation = quaternion.LookRotationSafe((dir.normalized), math.up());

                    targetWaypointIndex = i + 1;

                    return offsetPoint;
                }

                prevCurrentDistance = currentDistance;
            }

            return default;
        }

        private IEnumerator SpawnClearRoutine()
        {
            Clear();

            yield return new WaitForEndOfFrame();

            SpawnInternal();
        }

        private void SpawnInternal()
        {
            trafficRoadSpawnDebuggerSystem.Spawn(debuggerEntity, RuntimeCarModel);
        }

        private void CheckForNullPaths()
        {
            bool changed = false;

            for (int i = 0; i < TrafficSpawnTestInfos.Count; i++)
            {
                var trafficSpawnTestInfo = TrafficSpawnTestInfos[i];

                if (!trafficSpawnTestInfo.RelatedTrafficLightCrossroad)
                {
                    trafficSpawnTestInfo.RelatedTrafficLightCrossroad = GetComponentInChildren<TrafficLightCrossroad>();
                    changed = true;
                }

                if (trafficSpawnTestInfo.Path == null && trafficSpawnTestInfo.TrafficNodeIndex != -1 && trafficSpawnTestInfo.PathIndexInLane != -1)
                {
                    var trafficLightCrossroad = trafficSpawnTestInfo.RelatedTrafficLightCrossroad;

                    if (trafficLightCrossroad)
                    {
                        var nodes = trafficLightCrossroad.TrafficNodes;

                        if (nodes.Count > trafficSpawnTestInfo.TrafficNodeIndex)
                        {
                            var node = nodes[trafficSpawnTestInfo.TrafficNodeIndex];

                            var newPath = node.GetPathByLocalIndex(trafficSpawnTestInfo.PathLaneIndex, trafficSpawnTestInfo.PathIndexInLane);

                            if (newPath)
                            {
                                trafficSpawnTestInfo.Path = newPath;
                                changed = true;
                            }
                        }
                    }
                }
            }

            if (changed)
            {
                EditorSaver.SetObjectDirty(this);
            }
        }

        private void CheckForNotResolvedPath()
        {
            var changed = false;

            for (int i = 0; i < TrafficSpawnTestInfos.Count; i++)
            {
                var trafficSpawnTestInfo = TrafficSpawnTestInfos[i];

                if (trafficSpawnTestInfo.Path != null && trafficSpawnTestInfo.TrafficNodeIndex == -1)
                {
                    GetPathIndexes(trafficSpawnTestInfo.Path, out var trafficNodeIndex, out var laneIndex, out var localPathIndex);

                    trafficSpawnTestInfo.TrafficNodeIndex = trafficNodeIndex;
                    trafficSpawnTestInfo.PathLaneIndex = laneIndex;
                    trafficSpawnTestInfo.PathIndexInLane = localPathIndex;

                    changed = true;
                }
            }

            if (changed)
            {
                EditorSaver.SetObjectDirty(this);
            }
        }

        private void CheckForCollection()
        {
            if (vehicleDataCollection)
            {
                return;
            }

            var vehicleDataHolder = ObjectUtils.FindObjectOfType<VehicleDataHolder>();

            if (vehicleDataHolder)
            {
                vehicleDataCollection = vehicleDataHolder.VehicleDataCollection;
                EditorSaver.SetObjectDirty(this);
            }
        }

        public void ResolvePathIndexes()
        {
            for (int i = 0; i < TrafficSpawnTestInfos.Count; i++)
            {
                var trafficSpawnTestInfo = TrafficSpawnTestInfos[i];

                if (trafficSpawnTestInfo.Path != null)
                {
                    var sourceTrafficNode = trafficSpawnTestInfo.Path.SourceTrafficNode;

                    if (sourceTrafficNode)
                    {
                        GetPathIndexes(trafficSpawnTestInfo.Path, out var trafficNodeIndex, out var laneIndex, out var localPathIndex);

                        trafficSpawnTestInfo.TrafficNodeIndex = trafficNodeIndex;
                        trafficSpawnTestInfo.PathLaneIndex = laneIndex;
                        trafficSpawnTestInfo.PathIndexInLane = localPathIndex;
                    }
                }
            }

            EditorSaver.SetObjectDirty(this);
        }

        public void OnListChanged()
        {
            if (Application.isPlaying && debuggerEntity != Entity.Null)
            {
                if (entityManager.HasBuffer<DebugRoadLaneElement>(debuggerEntity))
                {
                    entityManager.RemoveComponent<DebugRoadLaneElement>(debuggerEntity);
                }

                var buff = entityManager.AddBuffer<DebugRoadLaneElement>(debuggerEntity);

                for (int i = 0; i < TrafficSpawnTestInfos.Count; i++)
                {
                    if (laneBuffer.Length > i)
                    {
                        var bufferElement = laneBuffer[i];
                        bufferElement.NormalizedPathPosition = TrafficSpawnTestInfos[i].NormalizedPathPosition;
                        laneBuffer[i] = bufferElement;

                        buff.Add(bufferElement);
                    }
                }
            }
        }

        private void GetPathIndexes(Path path, out int trafficNodeIndex, out int laneIndex, out int localPathIndex)
        {
            trafficNodeIndex = -1;
            laneIndex = -1;
            localPathIndex = -1;

            if (path && path.SourceTrafficNode)
            {
                var sourceNode = path.SourceTrafficNode;

                if (sourceNode.TrafficLightCrossroad)
                {
                    var trafficLightCrossroad = sourceNode.TrafficLightCrossroad;
                    var nodes = trafficLightCrossroad.TrafficNodes;

                    trafficNodeIndex = nodes.IndexOf(sourceNode);
                    laneIndex = sourceNode.GetLaneIndexOfPath(path);
                    localPathIndex = sourceNode.GetLocalLaneIndexOfPath(path);
                }
            }
        }

        public void Initialize()
        {
            if (IsInitialized)
                return;

            IsInitialized = true;

            trafficRoadSpawnDebuggerSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<TrafficRoadSpawnDebuggerSystem>();

            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        public DynamicBuffer<SpawnedCarDataElement> GetSpawnedCars() => trafficRoadSpawnDebuggerSystem.GetSpawnedCars(debuggerEntity);

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;

            if (!subscribed)
            {
                subscribed = true;

                if (!TrafficSpawnerSystem.IsInitialized)
                {
                    TrafficSpawnerSystem.OnInitialized += TrafficSpawnerSystem_OnInitialized;
                }
                else
                {
                    TrafficSpawnerSystem_OnInitialized();
                }

                Application.quitting += Application_quitting;
            }

            if (!spawned && IsInitialized && spawnOnView)
            {
#if UNITY_EDITOR
                var viewPortPos = Camera.main.WorldToViewportPoint(transform.position);

                if (viewPortPos.x >= 0 - ViewPortOffset && viewPortPos.y >= 0 - ViewPortOffset && viewPortPos.x <= 1 + ViewPortOffset && viewPortPos.y <= 1 + ViewPortOffset)
                {
                    spawned = true;
                    Spawn();
                }
#endif
            }
        }

        public void OnInspectorEnabled()
        {
            CheckForNullPaths();
            CheckForNotResolvedPath();
            CheckForCollection();
        }

        private void TrafficSpawnerSystem_OnInitialized()
        {
            TrafficSpawnerSystem.OnInitialized -= TrafficSpawnerSystem_OnInitialized;

            Initialize();

            if (spawnOnPlay)
            {
                SpawnInternal();
                spawned = true;
            }
        }

        private void Application_quitting()
        {
            Application.quitting -= Application_quitting;

            if (laneBuffer.IsCreated)
            {
                laneBuffer.Dispose();
            }

            TrafficSpawnerSystem.OnInitialized -= TrafficSpawnerSystem_OnInitialized;
        }
    }
}
