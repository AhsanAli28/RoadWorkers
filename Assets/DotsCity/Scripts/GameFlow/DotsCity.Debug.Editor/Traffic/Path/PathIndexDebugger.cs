using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Debug;
using Spirit604.DotsCity.Simulation.Level.Streaming;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.Traffic.Obstacle;
using Spirit604.Extensions;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Road.Debug
{
    public struct PathDebugInfo
    {
        public int Index;
        public Vector3 IndexPosition;
    }

    public class PathIndexDebugger : MonoBehaviourBase
    {
        private enum PathDebugMode { Default, ParallellPaths, NeighborPaths, NextConnectedPaths, ConnectedByPaths, IntersectedPaths, ConnectedNodes, CarCount, Settings }

#pragma warning disable 0414

        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/pathDebug.html#path-index-debugger")]
        [SerializeField] private string link;

        [OnValueChanged(nameof(EnabledStateChanged))]
        [SerializeField] private bool shouldDebug;

        [ShowIf(nameof(shouldDebug))]
        [OnValueChanged(nameof(Unselect))]
        [SerializeField] private bool selectPath;

        [ShowIf(nameof(selectPath))]
        [OnValueChanged(nameof(Select))]
        [SerializeField] private int selectedPathIndex = -1;

        [ShowIf(nameof(shouldDebug))]
        [OnValueChanged(nameof(Select))]
        [SerializeField] private PathDebugMode pathDebugMode;

#pragma warning restore 0414

#if UNITY_EDITOR

        private PathIndexDebuggerSystem pathIndexDebuggerSystem;

        private Color defaultColor = Color.white;
        private StringBuilder tempSb = new StringBuilder();
        private Entity entity;
        private bool registered;

        private EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

        private NativeParallelMultiHashMap<int, CarHashData> CarHashMap
        {
            get
            {
                var system = World.DefaultGameObjectInjectionWorld.Unmanaged.GetExistingUnmanagedSystem<TrafficObstacleSystem>();

                if (EntityManager.HasComponent<TrafficObstacleSystem.Singleton>(system))
                {
                    return EntityManager.GetComponentData<TrafficObstacleSystem.Singleton>(system).CarHashMap;
                }

                return default;
            }
        }

        private void Awake()
        {
            EnabledStateChanged();
        }

#endif

        [Button]
        private void Select()
        {
#if UNITY_EDITOR
            Unselect();

            if (!Application.isPlaying)
            {
                return;
            }

            if (selectPath)
            {
                var graph = pathIndexDebuggerSystem.Graph;

                if (!graph.Exist(selectedPathIndex))
                    return;

                SelectHightlightPath(true, selectedPathIndex, Color.green);

                ref readonly var pathInfo = ref graph.GetPathData(selectedPathIndex);

                switch (pathDebugMode)
                {
                    case PathDebugMode.Default:
                        {
                            break;
                        }
                    case PathDebugMode.ParallellPaths:
                        {
                            var selectedPaths = graph.GetParallelPaths(in pathInfo);
                            SelectHightlightPath(true, ref selectedPaths);
                            break;
                        }
                    case PathDebugMode.NeighborPaths:
                        {
                            var selectedPaths = graph.GetNeighbourPaths(in pathInfo);
                            SelectHightlightPath(true, ref selectedPaths);
                            break;
                        }
                    case PathDebugMode.NextConnectedPaths:
                        {
                            var selectedPaths = graph.GetConnectedPaths(in pathInfo);
                            SelectHightlightPath(true, ref selectedPaths);
                            break;
                        }
                    case PathDebugMode.ConnectedByPaths:
                        {
                            var selectedPaths = graph.GetConnectedByPaths(in pathInfo);
                            SelectHightlightPath(true, ref selectedPaths);
                            break;
                        }
                    case PathDebugMode.IntersectedPaths:
                        {
                            var intersectedPaths = graph.GetIntersectedPaths(in pathInfo);

                            for (int i = 0; i < intersectedPaths.Length; i++)
                            {
                                var index = intersectedPaths[i].IntersectedPathIndex;

                                SelectHightlightPath(true, index);
                            }

                            break;
                        }
                    case PathDebugMode.CarCount:
                        {
                            break;
                        }
                }
            }
#endif
        }

#if UNITY_EDITOR

        private void SelectHightlightPath(bool isHighlighted,
#if !RUNTIME_ROAD
            ref NativeSlice<int> indexes
#else
            ref NativeArray<int>.ReadOnly indexes
#endif
            )
        {
            for (int i = 0; i < indexes.Length; i++)
            {
                SelectHightlightPath(isHighlighted, indexes[i]);
            }
        }

        private void SelectHightlightPath(bool isHighlighted, int index)
        {
            SelectHightlightPath(isHighlighted, index, Color.white);
        }

        private void SelectHightlightPath(bool isHighlighted, int index, Color color)
        {
            if (!pathIndexDebuggerSystem.SelectedIndexes.ContainsKey(index))
            {
                pathIndexDebuggerSystem.SelectedIndexes.Add(index, new PathIndexDebuggerSystem.SelectedPathData());
            }

            pathIndexDebuggerSystem.SelectedIndexes[index].Color = color;
            pathIndexDebuggerSystem.SelectedIndexes[index].Highliten = isHighlighted;
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !shouldDebug)
            {
                return;
            }

            var paths = pathIndexDebuggerSystem.Paths;

            for (int i = 0; i < paths.Length; i++)
            {
                Vector3 yOffset = new Vector3(0, 2f);
                Vector3 position = paths[i].IndexPosition + yOffset;

                var graph = pathIndexDebuggerSystem.Graph;

                ref readonly var pathInfo = ref graph.GetPathData(paths[i].Index);

                if (selectPath && (selectedPathIndex >= 0 && selectedPathIndex != paths[i].Index))
                {
                    continue;
                }

                string indexInfo = paths[i].Index.ToString();

                switch (pathDebugMode)
                {
                    case PathDebugMode.Default:
                        {
                            break;
                        }
                    case PathDebugMode.ParallellPaths:
                        {
                            if (pathInfo.ParallelCount > 0)
                            {
                                var selectedPaths = graph.GetParallelPaths(in pathInfo);
                                indexInfo = FillString(indexInfo, ref selectedPaths);
                            }

                            break;
                        }
                    case PathDebugMode.NeighborPaths:
                        {
                            if (pathInfo.NeighbourCount > 0)
                            {
                                var selectedPaths = graph.GetNeighbourPaths(in pathInfo);
                                indexInfo = FillString(indexInfo, ref selectedPaths);
                            }

                            break;
                        }
                    case PathDebugMode.NextConnectedPaths:
                        {
                            if (pathInfo.ConnectedPathDataCount > 0)
                            {
                                var selectedPaths = graph.GetConnectedPaths(in pathInfo);
                                indexInfo = FillString(indexInfo, ref selectedPaths);
                            }

                            break;
                        }
                    case PathDebugMode.ConnectedByPaths:
                        {
                            if (pathInfo.ConnectedPathDataByCount > 0)
                            {
                                var selectedPaths = graph.GetConnectedByPaths(in pathInfo);
                                indexInfo = FillString(indexInfo, ref selectedPaths);
                            }

                            break;
                        }
                    case PathDebugMode.IntersectedPaths:
                        {
                            if (pathInfo.IntersectedCount > 0)
                            {
                                var selectedPaths = graph.GetIntersectedPaths(in pathInfo);
                                indexInfo = FillString2(indexInfo, ref selectedPaths);
                            }
                            break;
                        }
                    case PathDebugMode.ConnectedNodes:
                        {
                            int sourceTrafficNodeIndex = -1;
                            int connectedTrafficNodeIndex = -1;

                            var pathDataHashMap = TrafficNodeResolverSystem.PathDataHashMapStaticRef;

                            if (pathDataHashMap.TryGetValue(paths[i].Index, out var pathData))
                            {
                                sourceTrafficNodeIndex = pathData.SourceNode.Index;
                                connectedTrafficNodeIndex = pathData.ConnectedNode.Index;
                            }

                            indexInfo = $"{indexInfo} ({sourceTrafficNodeIndex} {connectedTrafficNodeIndex})";
                            break;
                        }
                    case PathDebugMode.CarCount:
                        {
                            int carCount = 0;

                            var hashMap = CarHashMap;

                            if (hashMap.IsCreated)
                            {
                                carCount = hashMap.CountValuesForKey(paths[i].Index);
                            }

                            indexInfo += $" (Car count = {carCount})";
                            break;
                        }
                    case PathDebugMode.Settings:
                        {
                            var pathDataHashMap = TrafficNodeResolverSystem.PathDataHashMapStaticRef;

                            if (pathDataHashMap.TryGetValue(paths[i].Index, out var pathData))
                            {
                                indexInfo +=
                                    $" Priority {pathInfo.Priority}\n" +
                                    $" PathCurveType {pathInfo.PathCurveType}\n" +
                                    $" TrafficGroup {pathInfo.TrafficGroup}\n" +
                                    $" EnterOfCrossroad {pathInfo.HasOption(PathOptions.EnterOfCrossroad)}\n" +
                                    $" HasCustomNode {pathInfo.HasOption(PathOptions.HasCustomNode)}\n";
                            }

                            break;
                        }
                }

                EditorExtension.DrawWorldString(indexInfo, position);
            }
        }

        private string FillString(string source,
#if !RUNTIME_ROAD
            ref NativeSlice<int> array
#else
            ref NativeArray<int>.ReadOnly array
#endif
            )
        {
            tempSb.Clear();
            tempSb.Append(source);

            for (int i = 0; i < array.Length; i++)
            {
                if (i == 0)
                {
                    tempSb.Append(" (");
                }

                tempSb.Append(array[i].ToString());

                if (i == array.Length - 1)
                {
                    tempSb.Append(")");
                }
                else
                {
                    tempSb.Append(", ");
                }
            }

            return tempSb.ToString();
        }

        private string FillString2(string source,

#if !RUNTIME_ROAD
            ref NativeSlice<IntersectPathInfo> array
#else
            ref NativeArray<IntersectPathInfo>.ReadOnly array
#endif
            )
        {
            tempSb.Clear();
            tempSb.Append(source);

            for (int i = 0; i < array.Length; i++)
            {
                if (i == 0)
                {
                    tempSb.Append(" (");
                }

                tempSb.Append(array[i].IntersectedPathIndex.ToString());

                if (i == array.Length - 1)
                {
                    tempSb.Append(")");
                }
                else
                {
                    tempSb.Append(", ");
                }
            }

            return tempSb.ToString();
        }

#endif

        public void EnabledStateChanged()
        {
            if (!Application.isPlaying) return;

#if UNITY_EDITOR
            try
            {
                if (shouldDebug)
                {
                    if (entity == Entity.Null)
                    {
                        entity = EntityManager.CreateEntity(typeof(PathIndexDebuggerSystem.EnabledTag));

                        if (!registered)
                        {
                            registered = true;
                            pathIndexDebuggerSystem = DefaultWorldUtils.CreateAndAddSystemManaged<PathIndexDebuggerSystem, DebugGroup>();

                            var pathDrawer = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TrafficPathDrawerSystem>();

                            pathDrawer.Initialize(pathIndexDebuggerSystem);
                        }
                    }
                }
                else
                {
                    if (entity != Entity.Null)
                    {
                        EntityManager.DestroyEntity(entity);
                        entity = Entity.Null;
                    }
                }
            }
            catch { }

            Unselect();
#endif
        }

        [Button]
        public void Unselect()
        {
#if UNITY_EDITOR
            if (pathIndexDebuggerSystem == null)
                return;

            if (!Application.isPlaying)
            {
                return;
            }

            foreach (var selectedPath in pathIndexDebuggerSystem.SelectedIndexes)
            {
                SelectHightlightPath(false, selectedPath.Key, Color.white);
            }

            pathIndexDebuggerSystem.SelectedIndexes.Clear();

#endif
        }

#if UNITY_EDITOR

        [Button("Focus On Selected Path")]
        private void Focus()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            var graph = pathIndexDebuggerSystem.Graph;

            if (graph.Exist(selectedPathIndex))
            {
                var focusPosition = graph.GetStartPosition(selectedPathIndex);

                SceneView.lastActiveSceneView.LookAt(focusPosition);
            }
        }

#endif
    }

#if UNITY_EDITOR
    [UpdateInGroup(typeof(DebugGroup))]
    [RequireMatchingQueriesForUpdate]
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class PathIndexDebuggerSystem : SystemBase
    {
        public struct EnabledTag : IComponentData { }

        public class SelectedPathData
        {
            public Color Color;
            public bool Highliten;
        }

        private const int PathHashMapCapacity = 10000;

        private Dictionary<int, SelectedPathData> selectedIndexes = new Dictionary<int, SelectedPathData>();

        public NativeList<PathDebugInfo> Paths;
        public PathGraphSystem.Singleton Graph;

        public Dictionary<int, SelectedPathData> SelectedIndexes { get => selectedIndexes; set => selectedIndexes = value; }

        protected override void OnCreate()
        {
            base.OnCreate();
            Paths = new NativeList<PathDebugInfo>(PathHashMapCapacity, Allocator.Persistent);
            RequireForUpdate<PathGraphSystem.Singleton>();
            RequireForUpdate<EnabledTag>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (Paths.IsCreated)
            {
                Paths.Dispose();
            }
        }

        protected override void OnUpdate()
        {
            Dependency.Complete();
            Paths.Clear();

            Graph = SystemAPI.GetSingleton<PathGraphSystem.Singleton>();
            var pathGraphReferenceLocal = Graph;

            if (pathGraphReferenceLocal.Count() == 0) return;

            var localPaths = Paths.AsParallelWriter();

            Entities
            .WithNativeDisableParallelForRestriction(localPaths)
            .WithReadOnly(pathGraphReferenceLocal)
            .ForEach((
                in DynamicBuffer<PathConnectionElement> pathConnections) =>
            {
                for (int i = 0; i < pathConnections.Length; i++)
                {
                    var pathIndex = pathConnections[i].GlobalPathIndex;

                    var nodes = pathGraphReferenceLocal.GetRouteNodes(pathIndex);

                    Vector3 position;

                    if (nodes.Length > 2)
                    {
                        int middleIndex = Mathf.FloorToInt((float)(nodes.Length) / 2);
                        position = nodes[middleIndex].Position;
                    }
                    else
                    {
                        position = (nodes[0].Position + nodes[1].Position) / 2;
                    }

                    localPaths.AddNoResize(new PathDebugInfo()
                    {
                        Index = pathIndex,
                        IndexPosition = position
                    });
                }
            }).ScheduleParallel();
        }

        public bool IsHighliten(int index)
        {
            return selectedIndexes.ContainsKey(index);
        }

        public SelectedPathData GetSelectedPathData(int index)
        {
            if (selectedIndexes.ContainsKey(index))
            {
                return selectedIndexes[index];
            }

            return null;
        }
    }

#endif
}
