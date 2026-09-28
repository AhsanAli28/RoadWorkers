using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

#if RUNTIME_ROAD
using System.Collections.Generic;
using System;
using Unity.Collections.LowLevel.Unsafe;
#endif

namespace Spirit604.DotsCity.Simulation.Road
{
    public partial struct PathGraphSystem
    {
        public enum RelationType { Neighbour, Connected, ConnectedBy, Parallel, Intersected }

        public struct Singleton : IComponentData
        {
#if !RUNTIME_ROAD

            internal NativeArray<PathData> allPaths;
            internal NativeArray<int> allConnectedPaths;
            internal NativeArray<int> allConnectedByPaths;
            internal NativeArray<IntersectPathInfo> allIntersectedPaths;
            internal NativeArray<int> allNeighbourPaths;
            internal NativeArray<int> allParallelPaths;
            internal NativeArray<RouteNodeData> allRouteNodesPath;
#else

            internal NativeList<PathData> allPaths;
            internal UnsafeHashMap<int, NativeArrayIndexContainer> allConnectedPaths;
            internal UnsafeHashMap<int, NativeArrayIndexContainer> allConnectedByPaths;
            internal UnsafeHashMap<int, NativeIntersectContainer> allIntersectedPaths;
            internal UnsafeHashMap<int, NativeArrayIndexContainer> allNeighbourPaths;
            internal UnsafeHashMap<int, NativeArrayIndexContainer> allParallelPaths;
            internal UnsafeHashMap<int, NativeRouteContainer> allRouteNodesPath;
            internal UnsafeHashSet<int> removedPaths;
#endif

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Exist(int pathIndex)
#if !RUNTIME_ROAD
                => allPaths.Length > pathIndex && pathIndex >= 0;
#else
                => allPaths.Length > pathIndex && pathIndex >= 0 && allPaths[pathIndex].PathIndex != -1;
#endif

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref readonly PathData GetPathData(int pathIndex)
                => ref allPaths.ElementAtRO(pathIndex);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref readonly RouteNodeData GetPathNodeData(int pathIndex, int pathNodeIndex)
            {
                ref readonly var pathData = ref GetPathData(pathIndex);
                return ref GetPathNodeData(in pathData, pathNodeIndex);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref readonly RouteNodeData GetPathNodeData(in PathData pathData, int pathNodeIndex)
#if !RUNTIME_ROAD
                => ref allRouteNodesPath.ElementAtRO(pathData.NodeIndex + pathNodeIndex);
#else
                => ref allRouteNodesPath[pathData.PathIndex].Data.ElementAtRO(pathNodeIndex);
#endif

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public
#if !RUNTIME_ROAD
                NativeSlice<int>
#else
                NativeArray<int>.ReadOnly
#endif
                GetConnectedPaths(int pathIndex)
            {
                ref readonly var pathData = ref GetPathData(pathIndex);
                return GetConnectedPaths(in pathData);
            }

            public
#if !RUNTIME_ROAD
                NativeSlice<int>
#else
                NativeArray<int>.ReadOnly
#endif

                GetConnectedPaths(in PathData pathData)
            {
                if (pathData.ConnectedPathDataCount > 0)
#if !RUNTIME_ROAD
                    return allConnectedPaths.Slice(pathData.ConnectedPathDataIndex, pathData.ConnectedPathDataCount);
#else
                    return allConnectedPaths[pathData.PathIndex].Data.AsReadOnly();
#endif

                return default;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public
#if !RUNTIME_ROAD
                NativeSlice<int>
#else
                NativeArray<int>.ReadOnly
#endif
                GetConnectedByPaths(int pathIndex)
            {
                ref readonly var pathData = ref GetPathData(pathIndex);
                return GetConnectedByPaths(in pathData);
            }

            public
#if !RUNTIME_ROAD
                NativeSlice<int>
#else
                NativeArray<int>.ReadOnly
#endif
                GetConnectedByPaths(in PathData pathData)
            {
                if (pathData.ConnectedPathDataByCount > 0)
#if !RUNTIME_ROAD
                    return allConnectedByPaths.Slice(pathData.ConnectedPathDataByIndex, pathData.ConnectedPathDataByCount);
#else
                    return allConnectedByPaths[pathData.PathIndex].Data.AsReadOnly();
#endif

                return default;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public
#if !RUNTIME_ROAD
                NativeSlice<IntersectPathInfo>
#else
                NativeArray<IntersectPathInfo>.ReadOnly
#endif
                GetIntersectedPaths(int pathIndex)
            {
                ref readonly var pathData = ref GetPathData(pathIndex);
                return GetIntersectedPaths(in pathData);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public
#if !RUNTIME_ROAD
                NativeSlice<IntersectPathInfo>
#else
                NativeArray<IntersectPathInfo>.ReadOnly
#endif
                GetIntersectedPaths(in PathData pathData)
            {
                if (pathData.IntersectedCount > 0)
#if !RUNTIME_ROAD
                    return allIntersectedPaths.Slice(pathData.IntersectedIndex, pathData.IntersectedCount);
#else
                    return allIntersectedPaths[pathData.PathIndex].Data.AsReadOnly();
#endif

                return default;
            }

#if RUNTIME_ROAD
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SortIntersection(int pathIndex)
            {
                var startPos = GetStartPosition(pathIndex);
                var container = allIntersectedPaths[pathIndex];

                container.Data.Sort(new DistanceComparer(startPos));

                allIntersectedPaths[pathIndex] = container;
            }

            struct DistanceComparer : IComparer<IntersectPathInfo>
            {
                private float3 referencePoint;

                public DistanceComparer(float3 referencePoint)
                {
                    this.referencePoint = referencePoint;
                }

                public int Compare(IntersectPathInfo x, IntersectPathInfo y)
                {
                    float distanceX = math.distancesq(x.IntersectPosition, referencePoint);
                    float distanceY = math.distancesq(y.IntersectPosition, referencePoint);
                    return distanceX.CompareTo(distanceY);
                }
            }
#endif

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public
#if !RUNTIME_ROAD
                NativeSlice<int>
#else
                NativeArray<int>.ReadOnly
#endif
                GetNeighbourPaths(int pathIndex)
            {
                ref readonly var pathData = ref GetPathData(pathIndex);
                return GetNeighbourPaths(in pathData);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public
#if !RUNTIME_ROAD
                NativeSlice<int>
#else
                NativeArray<int>.ReadOnly
#endif
                GetNeighbourPaths(in PathData pathData)
            {
                if (pathData.NeighbourCount > 0)
#if !RUNTIME_ROAD
                    return allNeighbourPaths.Slice(pathData.NeighbourIndex, pathData.NeighbourCount);
#else
                    return allNeighbourPaths[pathData.PathIndex].Data.AsReadOnly();
#endif

                return default;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public
#if !RUNTIME_ROAD
                NativeSlice<int>
#else
                NativeArray<int>.ReadOnly
#endif
                GetParallelPaths(int pathIndex)
            {
                ref readonly var pathData = ref GetPathData(pathIndex);
                return GetParallelPaths(in pathData);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public
#if !RUNTIME_ROAD
                NativeSlice<int>
#else
                NativeArray<int>.ReadOnly
#endif
                GetParallelPaths(in PathData pathData)
            {
                if (pathData.ParallelCount > 0)
#if !RUNTIME_ROAD
                    return allParallelPaths.Slice(pathData.ParallelIndex, pathData.ParallelCount);
#else
                    return allParallelPaths[pathData.PathIndex].Data.AsReadOnly();
#endif

                return default;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public
#if !RUNTIME_ROAD
                NativeSlice<RouteNodeData>
#else
                NativeArray<RouteNodeData>.ReadOnly
#endif
                GetRouteNodes(int pathIndex)
            {
                ref readonly var pathData = ref GetPathData(pathIndex);
                return GetRouteNodes(in pathData);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public
#if !RUNTIME_ROAD
                NativeSlice<RouteNodeData>
#else
                NativeArray<RouteNodeData>.ReadOnly
#endif
                GetRouteNodes(in PathData pathData)
            {
#if !RUNTIME_ROAD
                return allRouteNodesPath.Slice(pathData.NodeIndex, pathData.NodeCount);
#else
                return allRouteNodesPath[pathData.PathIndex].Data.AsReadOnly();
#endif
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float3 GetStartPosition(int pathIndex)
            {
                ref readonly var pathData = ref GetPathData(pathIndex);
                return GetStartPosition(in pathData);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float3 GetStartPosition(in PathData pathData)
            {
#if !RUNTIME_ROAD
                return allRouteNodesPath[pathData.NodeIndex].Position;
#else
                return allRouteNodesPath[pathData.PathIndex].Data[0].Position;
#endif
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float3 GetEndPosition(int pathIndex)
            {
                ref readonly var pathData = ref GetPathData(pathIndex);
                return GetEndPosition(in pathData);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float3 GetEndPosition(in PathData pathData)
            {
#if !RUNTIME_ROAD
                return allRouteNodesPath[pathData.NodeIndex + pathData.NodeCount - 1].Position;
#else
                return allRouteNodesPath[pathData.PathIndex].Data[pathData.NodeCount - 1].Position;
#endif
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool HasRelatedPath(int sourcePathIndex, int relatedPathIndex, RelationType relationType)
            {
                switch (relationType)
                {
                    case RelationType.Neighbour:
                        {
                            var paths = GetNeighbourPaths(sourcePathIndex);

                            for (int i = 0; i < paths.Length; i++)
                            {
                                if (paths[i] == relatedPathIndex)
                                {
                                    return true;
                                }
                            }

                            break;
                        }
                    case RelationType.Connected:
                        {
                            var paths = GetConnectedPaths(sourcePathIndex);

                            for (int i = 0; i < paths.Length; i++)
                            {
                                if (paths[i] == relatedPathIndex)
                                {
                                    return true;
                                }
                            }

                            break;
                        }
                    case RelationType.ConnectedBy:
                        {
                            var paths = GetConnectedByPaths(sourcePathIndex);

                            for (int i = 0; i < paths.Length; i++)
                            {
                                if (paths[i] == relatedPathIndex)
                                {
                                    return true;
                                }
                            }

                            break;
                        }
                    case RelationType.Parallel:
                        {
                            var paths = GetParallelPaths(sourcePathIndex);

                            for (int i = 0; i < paths.Length; i++)
                            {
                                if (paths[i] == relatedPathIndex)
                                {
                                    return true;
                                }
                            }

                            break;
                        }
                    case RelationType.Intersected:
                        {
                            var paths = GetIntersectedPaths(sourcePathIndex);

                            for (int i = 0; i < paths.Length; i++)
                            {
                                if (paths[i].IntersectedPathIndex == relatedPathIndex)
                                {
                                    return true;
                                }
                            }

                            break;
                        }
                }

                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool SetPriority(int pathIndex, int priority)
            {
                if (Exist(pathIndex))
                {
                    ref var pathData = ref allPaths.ElementAt(pathIndex);
                    pathData.Priority = priority;
                    allPaths[pathIndex] = pathData;

                    return true;
                }

                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool SetGroup(int pathIndex, TrafficGroupType trafficGroup)
            {
                if (Exist(pathIndex))
                {
                    ref var pathData = ref allPaths.ElementAt(pathIndex);
                    pathData.TrafficGroup = trafficGroup;
                    allPaths[pathIndex] = pathData;

                    return true;
                }

                return false;
            }

#if RUNTIME_ROAD

            public void AddPath(in PathData pathData)
            {
                if (removedPaths.Contains(pathData.PathIndex))
                {
                    removedPaths.Remove(pathData.PathIndex);
                    allPaths[pathData.PathIndex] = pathData;
                }
                else
                {
                    allPaths.Add(pathData);
                }
            }

            public void AddConnectedPath(int sourcePathIndex, int connectedPathIndex)
            {
                if (!allConnectedPaths.ContainsKey(sourcePathIndex))
                {
                    allConnectedPaths.Add(sourcePathIndex, new NativeArrayIndexContainer()
                    {
                        Data = new NativeList<int>(1, Allocator.Persistent)
                    });
                }

                var path = allPaths[sourcePathIndex];
                path.ConnectedPathDataCount++;
                allPaths[sourcePathIndex] = path;

                allConnectedPaths[sourcePathIndex].Data.Add(connectedPathIndex);
            }

            public void AddConnectedByPath(int connectedPathIndex, int sourcePathIndex)
            {
                if (!allConnectedByPaths.ContainsKey(connectedPathIndex))
                {
                    allConnectedByPaths.Add(connectedPathIndex, new NativeArrayIndexContainer()
                    {
                        Data = new NativeList<int>(1, Allocator.Persistent)
                    });
                }

                var path = allPaths[connectedPathIndex];
                path.ConnectedPathDataByCount++;
                allPaths[connectedPathIndex] = path;

                allConnectedByPaths[connectedPathIndex].Data.Add(sourcePathIndex);
            }

            public NativeList<int> GetEmptyPaths(int count, Allocator allocator)
            {
                NativeList<int> paths = new NativeList<int>(count, allocator);

                foreach (var removedPath in removedPaths)
                {
                    paths.Add(removedPath);
                    count--;

                    if (count == 0)
                        break;
                }

                for (int i = 0; i < count; i++)
                {
                    paths.Add(allPaths.Length + i);
                }

                return paths;
            }

            public void RemovePaths(NativeList<int> removePaths)
            {
                for (int i = 0; i < removePaths.Length; i++)
                {
                    RemovePath(removePaths[i]);
                }
            }

            public bool RemovePath(int pathIndex)
            {
                if (!Exist(pathIndex))
                    return false;

                if (allConnectedPaths.TryGetValue(pathIndex, out var connectedList))
                {
                    for (int i = 0; i < connectedList.Data.Length; i++)
                    {
                        var connectedIndex = connectedList.Data[i];

                        if (allConnectedByPaths.TryGetValue(connectedIndex, out var connectedByTempList))
                        {
                            var index = connectedByTempList.Data.IndexOf(pathIndex);

                            if (index >= 0)
                            {
                                var relatedPathIndex = connectedByTempList.Data[index];
                                var connectedTempPath = allPaths[relatedPathIndex];
                                connectedTempPath.ConnectedPathDataByCount--;
                                allPaths[relatedPathIndex] = connectedTempPath;

                                connectedByTempList.Data.RemoveAt(index);
                            }
                        }
                    }

                    connectedList.Data.Dispose();
                    allConnectedPaths.Remove(pathIndex);
                }

                if (allConnectedByPaths.TryGetValue(pathIndex, out var connectedByList))
                {
                    for (int i = 0; i < connectedByList.Data.Length; i++)
                    {
                        var connectedByIndex = connectedByList.Data[i];

                        if (allConnectedPaths.TryGetValue(connectedByIndex, out var connectedTempList))
                        {
                            var index = connectedTempList.Data.IndexOf(pathIndex);

                            if (index >= 0)
                            {
                                var relatedPathIndex = connectedTempList.Data[index];
                                var connectedTempPath = allPaths[relatedPathIndex];
                                connectedTempPath.ConnectedPathDataCount--;
                                allPaths[relatedPathIndex] = connectedTempPath;

                                connectedTempList.Data.RemoveAt(index);
                            }
                        }
                    }

                    connectedByList.Data.Dispose();
                    allConnectedByPaths.Remove(pathIndex);
                }

                if (allParallelPaths.TryGetValue(pathIndex, out var parallelList))
                {
                    parallelList.Data.Dispose();
                    allParallelPaths.Remove(pathIndex);
                }

                if (allIntersectedPaths.TryGetValue(pathIndex, out var intersectedList))
                {
                    intersectedList.Data.Dispose();
                    allIntersectedPaths.Remove(pathIndex);
                }

                if (allNeighbourPaths.TryGetValue(pathIndex, out var neighbourList))
                {
                    neighbourList.Data.Dispose();
                    allNeighbourPaths.Remove(pathIndex);
                }

                if (allRouteNodesPath.TryGetValue(pathIndex, out var nodeList))
                {
                    nodeList.Data.Dispose();
                    allRouteNodesPath.Remove(pathIndex);
                }

                var tempPath = allPaths[pathIndex];
                tempPath.PathIndex = -1;
                allPaths[pathIndex] = tempPath;

                removedPaths.Add(pathIndex);
                return true;
            }

            public void InitIntersection(int sourcePathIndex, int initialCapacity)
            {
                if (!allIntersectedPaths.ContainsKey(sourcePathIndex))
                    allIntersectedPaths.Add(sourcePathIndex, new NativeIntersectContainer()
                    {
                        Data = new NativeList<IntersectPathInfo>(initialCapacity, Allocator.Persistent)
                    });
            }

            public void AddIntersection(int sourcePathIndex, IntersectPathInfo intersectPathInfo)
            {
                var path = allPaths[sourcePathIndex];
                path.IntersectedCount++;
                allPaths[sourcePathIndex] = path;

                allIntersectedPaths[sourcePathIndex].Data.Add(intersectPathInfo);
            }

            public void InitRouteNodes(int sourcePathIndex, int initialCapacity)
            {
                if (!allRouteNodesPath.ContainsKey(sourcePathIndex))
                    allRouteNodesPath.Add(sourcePathIndex, new NativeRouteContainer()
                    {
                        Data = new NativeList<RouteNodeData>(initialCapacity, Allocator.Persistent)
                    });
            }

            public void AddRouteNode(int sourcePathIndex, RouteNodeData routeNodeData)
            {
                var path = allPaths[sourcePathIndex];
                path.NodeCount++;
                allPaths[sourcePathIndex] = path;

                allRouteNodesPath[sourcePathIndex].Data.Add(routeNodeData);
            }

            public void InitNeighbourPaths(int sourcePathIndex, int initialCapacity)
            {
                if (!allNeighbourPaths.ContainsKey(sourcePathIndex))
                    allNeighbourPaths.Add(sourcePathIndex, new NativeArrayIndexContainer()
                    {
                        Data = new NativeList<int>(initialCapacity, Allocator.Persistent)
                    });
            }

            public void AddNeighbourPath(int sourcePathIndex, int neighbourPathIndex)
            {
                var path = allPaths[sourcePathIndex];
                path.NeighbourCount++;
                allPaths[sourcePathIndex] = path;

                allNeighbourPaths[sourcePathIndex].Data.Add(neighbourPathIndex);
            }

            public void InitParallelPaths(int sourcePathIndex, int initialCapacity)
            {
                if (!allParallelPaths.ContainsKey(sourcePathIndex))
                    allParallelPaths.Add(sourcePathIndex, new NativeArrayIndexContainer()
                    {
                        Data = new NativeList<int>(initialCapacity, Allocator.Persistent)
                    });
            }

            public void AddParallelPath(int sourcePathIndex, int parallelPathIndex)
            {
                var path = allPaths[sourcePathIndex];
                path.ParallelCount++;
                allPaths[sourcePathIndex] = path;

                allParallelPaths[sourcePathIndex].Data.Add(parallelPathIndex);
            }

            public void ClearGraph()
            {
                if (!allPaths.IsCreated || allPaths.Length == 0)
                    return;

                for (int pathIndex = 0; pathIndex < allPaths.Length; pathIndex++)
                {
                    if (allConnectedPaths.TryGetValue(pathIndex, out var container))
                    {
                        if (container.Data.IsCreated)
                            container.Data.Dispose();
                    }

                    if (allConnectedByPaths.TryGetValue(pathIndex, out var container2))
                    {
                        if (container2.Data.IsCreated)
                            container2.Data.Dispose();
                    }

                    if (allIntersectedPaths.TryGetValue(pathIndex, out var container3))
                    {
                        if (container3.Data.IsCreated)
                            container3.Data.Dispose();
                    }

                    if (allNeighbourPaths.TryGetValue(pathIndex, out var container4))
                    {
                        if (container4.Data.IsCreated)
                            container4.Data.Dispose();
                    }

                    if (allParallelPaths.TryGetValue(pathIndex, out var container5))
                    {
                        if (container5.Data.IsCreated)
                            container5.Data.Dispose();
                    }

                    if (allRouteNodesPath.TryGetValue(pathIndex, out var container6))
                    {
                        if (container6.Data.IsCreated)
                            container6.Data.Dispose();
                    }
                }

                allPaths.Clear();
                allConnectedPaths.Clear();
                allConnectedByPaths.Clear();
                allIntersectedPaths.Clear();
                allNeighbourPaths.Clear();
                allParallelPaths.Clear();
                allRouteNodesPath.Clear();
                removedPaths.Clear();
            }
#endif
        }
    }
}
