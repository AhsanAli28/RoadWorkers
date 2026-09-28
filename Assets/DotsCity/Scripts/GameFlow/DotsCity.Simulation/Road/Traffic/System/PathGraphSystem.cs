using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

#if RUNTIME_ROAD
using Unity.Collections.LowLevel.Unsafe;
#endif

namespace Spirit604.DotsCity.Simulation.Road
{
    public partial struct PathGraphSystem : ISystem, ISystemStartStop
    {

#if !RUNTIME_ROAD

        // Converted from blob static graph
        private NativeArray<PathData> allPaths;
        private NativeArray<int> allConnectedPaths;
        private NativeArray<int> allConnectedByPaths;
        private NativeArray<IntersectPathInfo> allIntersectedPaths;
        private NativeArray<int> allNeighbourPaths;
        private NativeArray<int> allParallelPaths;
        private NativeArray<RouteNodeData> allRouteNodesPath;
#else

        internal struct NativeArrayIndexContainer
        {
            public NativeList<int> Data;
        }

        internal struct NativeRouteContainer
        {
            public NativeList<RouteNodeData> Data;
        }

        internal struct NativeIntersectContainer
        {
            public NativeList<IntersectPathInfo> Data;
        }

        // Runtime graph
        private NativeList<PathData> allPaths;
        private UnsafeHashMap<int, NativeArrayIndexContainer> allConnectedPaths;
        private UnsafeHashMap<int, NativeArrayIndexContainer> allConnectedByPaths;
        private UnsafeHashMap<int, NativeIntersectContainer> allIntersectedPaths;
        private UnsafeHashMap<int, NativeArrayIndexContainer> allNeighbourPaths;
        private UnsafeHashMap<int, NativeArrayIndexContainer> allParallelPaths;
        private UnsafeHashMap<int, NativeRouteContainer> allRouteNodesPath;
        private UnsafeHashSet<int> removedPathsIndexes;

#endif

        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PathGraphReference>();
        }

        void ISystem.OnDestroy(ref SystemState state)
        {
            Dispose();
        }

        public void OnStartRunning(ref SystemState state)
        {
            var graph = SystemAPI.GetSingleton<PathGraphReference>();

            ref var paths = ref graph.Graph.Value.Paths;

#if !RUNTIME_ROAD

            int connectPathIndex = 0;
            int connectPathByIndex = 0;
            int intersectedPathIndex = 0;
            int neighborIndex = 0;
            int parallelIndex = 0;
            int routeNodeIndex = 0;

            var allPathsTemp = new NativeList<PathData>(Allocator.TempJob);
            var allConnectedPathsTemp = new NativeList<int>(Allocator.TempJob);
            var allConnectedPathsByTemp = new NativeList<int>(Allocator.TempJob);
            var allIntersectedPathsTemp = new NativeList<IntersectPathInfo>(Allocator.TempJob);
            var allNeighbourPathsTemp = new NativeList<int>(Allocator.TempJob);
            var allParallelPathsTemp = new NativeList<int>(Allocator.TempJob);
            var allRouteNodesPathTemp = new NativeList<RouteNodeData>(Allocator.TempJob);

#else
            const int hashMapCapacity = 10000;

            allPaths = new NativeList<PathData>(hashMapCapacity, Allocator.Persistent);
            allConnectedPaths = new UnsafeHashMap<int, NativeArrayIndexContainer>(hashMapCapacity, Allocator.Persistent);
            allConnectedByPaths = new UnsafeHashMap<int, NativeArrayIndexContainer>(hashMapCapacity, Allocator.Persistent);
            allIntersectedPaths = new UnsafeHashMap<int, NativeIntersectContainer>(hashMapCapacity, Allocator.Persistent);
            allNeighbourPaths = new UnsafeHashMap<int, NativeArrayIndexContainer>(hashMapCapacity, Allocator.Persistent);
            allParallelPaths = new UnsafeHashMap<int, NativeArrayIndexContainer>(hashMapCapacity, Allocator.Persistent);
            allRouteNodesPath = new UnsafeHashMap<int, NativeRouteContainer>(hashMapCapacity, Allocator.Persistent);
#endif

            Dictionary<int, List<int>> allConnectedPathsByTempDict = new Dictionary<int, List<int>>();

#if UNITY_EDITOR
            bool reported = false;
#endif

            for (int pathIndex = 0; pathIndex < paths.Length; pathIndex++)
            {
                ref var path = ref paths[pathIndex];

                var pathData = new PathData()
                {
                    ConnectedPathDataCount = path.NextConnectedPathsIndexes.Length,
                    IntersectedCount = path.IntersectedPaths.Length,
                    NeighbourCount = path.NeighbourPathsIndexes.Length,
                    ParallelCount = path.ParallelPathsIndexes.Length,
                    NodeCount = path.RouteNodes.Length,

#if !RUNTIME_ROAD
                    ConnectedPathDataIndex = connectPathIndex,
                    IntersectedIndex = intersectedPathIndex,
                    NeighbourIndex = neighborIndex,
                    ParallelIndex = parallelIndex,
                    NodeIndex = routeNodeIndex,
#else
                    PathIndex = pathIndex,
#endif

                    PathLength = path.PathLength,
                    ConnectedPathIndex = path.ConnectedPathIndex,
                    SourceLaneIndex = path.SourceLaneIndex,
                    Priority = path.Priority,

                    Options = path.Options,
                    PathCurveType = path.PathCurveType,
                    PathRoadType = path.PathRoadType,
                    PathConnectionType = path.PathConnectionType,
                    TrafficGroup = path.TrafficGroup
                };

#if UNITY_EDITOR
                if (pathData.PathLength == 0 && !reported)
                {
                    reported = true;
                    UnityEngine.Debug.Log($"PathGraphSystem. Paths that haven't been baked found, bake the road data in RoadParent by pressing 'Bake Path Data' button. For more info:{System.Environment.NewLine}" +
                        $"<a href=\"https://dotstrafficcity.readthedocs.io/en/latest/bakingInfo.html\">https://dotstrafficcity.readthedocs.io/en/latest/bakingInfo.html</a>" +
                        $"\r\n\r\n\r\n\r\n");
                }
#endif

#if !RUNTIME_ROAD
                allPathsTemp.Add(pathData);

                for (int j = 0; j < path.NextConnectedPathsIndexes.Length; j++)
                {
                    var connectedIndex = path.NextConnectedPathsIndexes[j];

                    allConnectedPathsTemp.Add(connectedIndex);

                    if (!allConnectedPathsByTempDict.ContainsKey(connectedIndex))
                    {
                        allConnectedPathsByTempDict.Add(connectedIndex, new List<int>());
                    }

                    allConnectedPathsByTempDict[connectedIndex].Add(pathIndex);
                }

                for (int j = 0; j < path.IntersectedPaths.Length; j++)
                {
                    allIntersectedPathsTemp.Add(path.IntersectedPaths[j]);
                }

                for (int j = 0; j < path.NeighbourPathsIndexes.Length; j++)
                {
                    allNeighbourPathsTemp.Add(path.NeighbourPathsIndexes[j]);
                }

                for (int j = 0; j < path.ParallelPathsIndexes.Length; j++)
                {
                    allParallelPathsTemp.Add(path.ParallelPathsIndexes[j]);
                }

                for (int j = 0; j < path.RouteNodes.Length; j++)
                {
                    allRouteNodesPathTemp.Add(path.RouteNodes[j]);
                }

                connectPathIndex += path.NextConnectedPathsIndexes.Length;
                intersectedPathIndex += path.IntersectedPaths.Length;
                neighborIndex += path.NeighbourPathsIndexes.Length;
                parallelIndex += path.ParallelPathsIndexes.Length;
                routeNodeIndex += path.RouteNodes.Length;

                for (int j = 0; j < path.NextConnectedPathsIndexes.Length; j++)
                {
                    var connectedIndex = path.NextConnectedPathsIndexes[j];

                    allConnectedPathsTemp.Add(connectedIndex);

                    if (!allConnectedPathsByTempDict.ContainsKey(connectedIndex))
                    {
                        allConnectedPathsByTempDict.Add(connectedIndex, new List<int>());
                    }

                    allConnectedPathsByTempDict[connectedIndex].Add(pathIndex);
                }

                for (int j = 0; j < path.IntersectedPaths.Length; j++)
                {
                    allIntersectedPathsTemp.Add(path.IntersectedPaths[j]);
                }

                for (int j = 0; j < path.NeighbourPathsIndexes.Length; j++)
                {
                    allNeighbourPathsTemp.Add(path.NeighbourPathsIndexes[j]);
                }

                for (int j = 0; j < path.ParallelPathsIndexes.Length; j++)
                {
                    allParallelPathsTemp.Add(path.ParallelPathsIndexes[j]);
                }

                for (int j = 0; j < path.RouteNodes.Length; j++)
                {
                    allRouteNodesPathTemp.Add(path.RouteNodes[j]);
                }

                connectPathIndex += path.NextConnectedPathsIndexes.Length;
                intersectedPathIndex += path.IntersectedPaths.Length;
                neighborIndex += path.NeighbourPathsIndexes.Length;
                parallelIndex += path.ParallelPathsIndexes.Length;
                routeNodeIndex += path.RouteNodes.Length;

#else
                allPaths.Add(pathData);

                if (path.NextConnectedPathsIndexes.Length > 0)
                {
                    var container = new NativeArrayIndexContainer()
                    {
                        Data = new NativeList<int>(path.NextConnectedPathsIndexes.Length, Allocator.Persistent)
                    };

                    for (int j = 0; j < path.NextConnectedPathsIndexes.Length; j++)
                    {
                        var connectedIndex = path.NextConnectedPathsIndexes[j];

                        container.Data.Add(connectedIndex);

                        if (!allConnectedPathsByTempDict.ContainsKey(connectedIndex))
                        {
                            allConnectedPathsByTempDict.Add(connectedIndex, new List<int>());
                        }

                        allConnectedPathsByTempDict[connectedIndex].Add(pathIndex);
                    }

                    allConnectedPaths.Add(pathIndex, container);
                }

                if (path.IntersectedPaths.Length > 0)
                {
                    var container = new NativeIntersectContainer()
                    {
                        Data = new NativeList<IntersectPathInfo>(path.IntersectedPaths.Length, Allocator.Persistent)
                    };

                    for (int j = 0; j < path.IntersectedPaths.Length; j++)
                    {
                        container.Data.Add(path.IntersectedPaths[j]);
                    }

                    allIntersectedPaths.Add(pathIndex, container);
                }

                if (path.NeighbourPathsIndexes.Length > 0)
                {
                    var container = new NativeArrayIndexContainer()
                    {
                        Data = new NativeList<int>(path.NeighbourPathsIndexes.Length, Allocator.Persistent)
                    };

                    for (int j = 0; j < path.NeighbourPathsIndexes.Length; j++)
                    {
                        container.Data.Add(path.NeighbourPathsIndexes[j]);
                    }

                    allNeighbourPaths.Add(pathIndex, container);
                }

                if (path.ParallelPathsIndexes.Length > 0)
                {
                    var container = new NativeArrayIndexContainer()
                    {
                        Data = new NativeList<int>(path.ParallelPathsIndexes.Length, Allocator.Persistent)
                    };

                    for (int j = 0; j < path.ParallelPathsIndexes.Length; j++)
                    {
                        container.Data.Add(path.ParallelPathsIndexes[j]);
                    }

                    allParallelPaths.Add(pathIndex, container);
                }

                if (path.RouteNodes.Length > 0)
                {
                    var container = new NativeRouteContainer()
                    {
                        Data = new NativeList<RouteNodeData>(path.RouteNodes.Length, Allocator.Persistent)
                    };

                    for (int j = 0; j < path.RouteNodes.Length; j++)
                    {
                        container.Data.Add(path.RouteNodes[j]);
                    }

                    allRouteNodesPath.Add(pathIndex, container);
                }
#endif
            }

            for (int pathIndex = 0; pathIndex < paths.Length; pathIndex++)
            {
                if (!allConnectedPathsByTempDict.ContainsKey(pathIndex))
                    continue;

                var list = allConnectedPathsByTempDict[pathIndex];

                if (list.Count == 0)
                    continue;

#if !RUNTIME_ROAD
                var pathData = allPathsTemp[pathIndex];

                pathData.ConnectedPathDataByIndex = connectPathByIndex;
                pathData.ConnectedPathDataByCount += list.Count;

                for (int i = 0; i < list.Count; i++)
                {
                    allConnectedPathsByTemp.Add(list[i]);
                }

                connectPathByIndex += list.Count;

                allPathsTemp[pathIndex] = pathData;

#else
                var container = new NativeArrayIndexContainer()
                {
                    Data = new NativeList<int>(list.Count, Allocator.Persistent)
                };

                var pathData = allPaths[pathIndex];

                pathData.ConnectedPathDataByCount += list.Count;

                for (int i = 0; i < list.Count; i++)
                {
                    container.Data.Add(list[i]);
                }

                allPaths[pathIndex] = pathData;

                allConnectedByPaths.Add(pathIndex, container);
#endif
            }

            allConnectedPathsByTempDict.Clear();
            allConnectedPathsByTempDict = null;

#if !RUNTIME_ROAD

            allPaths = allPathsTemp.ToArray(Allocator.Persistent);
            allConnectedPaths = allConnectedPathsTemp.ToArray(Allocator.Persistent);
            allConnectedByPaths = allConnectedPathsByTemp.ToArray(Allocator.Persistent);
            allIntersectedPaths = allIntersectedPathsTemp.ToArray(Allocator.Persistent);
            allNeighbourPaths = allNeighbourPathsTemp.ToArray(Allocator.Persistent);
            allParallelPaths = allParallelPathsTemp.ToArray(Allocator.Persistent);
            allRouteNodesPath = allRouteNodesPathTemp.ToArray(Allocator.Persistent);

            var singleton = new PathGraphSystem.Singleton()
            {
                allPaths = this.allPaths,
                allConnectedPaths = this.allConnectedPaths,
                allConnectedByPaths = this.allConnectedByPaths,
                allIntersectedPaths = this.allIntersectedPaths,
                allNeighbourPaths = this.allNeighbourPaths,
                allParallelPaths = this.allParallelPaths,
                allRouteNodesPath = this.allRouteNodesPath
            };

            allPathsTemp.Dispose();
            allConnectedPathsTemp.Dispose();
            allConnectedPathsByTemp.Dispose();
            allIntersectedPathsTemp.Dispose();
            allNeighbourPathsTemp.Dispose();
            allParallelPathsTemp.Dispose();
            allRouteNodesPathTemp.Dispose();

#else

            removedPathsIndexes = new UnsafeHashSet<int>(100, Allocator.Persistent);

            var singleton = new PathGraphSystem.Singleton()
            {
                allPaths = this.allPaths,
                allConnectedPaths = this.allConnectedPaths,
                allConnectedByPaths = this.allConnectedByPaths,
                allIntersectedPaths = this.allIntersectedPaths,
                allNeighbourPaths = this.allNeighbourPaths,
                allParallelPaths = this.allParallelPaths,
                allRouteNodesPath = this.allRouteNodesPath,
                removedPaths = removedPathsIndexes
            };
#endif

            var entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, singleton);
        }

        public void OnStopRunning(ref SystemState state) { }

        private void Dispose()
        {
#if RUNTIME_ROAD

            if (allConnectedPaths.IsCreated)
                foreach (var item in allConnectedPaths)
                    if (item.Value.Data.IsCreated) item.Value.Data.Dispose();

            if (allConnectedByPaths.IsCreated)
                foreach (var item in allConnectedByPaths)
                    if (item.Value.Data.IsCreated) item.Value.Data.Dispose();

            if (allIntersectedPaths.IsCreated)
                foreach (var item in allIntersectedPaths)
                    if (item.Value.Data.IsCreated) item.Value.Data.Dispose();

            if (allNeighbourPaths.IsCreated)
                foreach (var item in allNeighbourPaths)
                    if (item.Value.Data.IsCreated) item.Value.Data.Dispose();

            if (allParallelPaths.IsCreated)
                foreach (var item in allParallelPaths)
                    if (item.Value.Data.IsCreated) item.Value.Data.Dispose();

            if (allRouteNodesPath.IsCreated)
                foreach (var item in allRouteNodesPath)
                    if (item.Value.Data.IsCreated) item.Value.Data.Dispose();

            if (removedPathsIndexes.IsCreated) removedPathsIndexes.Dispose();
#endif

            if (allPaths.IsCreated) allPaths.Dispose();
            if (allConnectedPaths.IsCreated) allConnectedPaths.Dispose();
            if (allConnectedByPaths.IsCreated) allConnectedByPaths.Dispose();
            if (allIntersectedPaths.IsCreated) allIntersectedPaths.Dispose();
            if (allNeighbourPaths.IsCreated) allNeighbourPaths.Dispose();
            if (allParallelPaths.IsCreated) allParallelPaths.Dispose();
            if (allRouteNodesPath.IsCreated) allRouteNodesPath.Dispose();
        }
    }
}
