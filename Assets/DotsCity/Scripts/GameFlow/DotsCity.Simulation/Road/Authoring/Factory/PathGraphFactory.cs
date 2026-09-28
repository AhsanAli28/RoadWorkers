using Spirit604.Gameplay.Road;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Road.Authoring
{
    public static class PathGraphFactory
    {
        public static BlobAssetReference<PathBlobDataGraph> Create(
            EntityManager entityManager,
            Entity graphEntity,
            List<TrafficNodeConversionSystem.PathBlobDataTemp> pathConnectingMap,
            Dictionary<int, List<IntersectPathInfo>> pathIntersectMap,
            Dictionary<int, List<int>> pathNeighbourMap,
            Dictionary<int, List<int>> pathParallelMap)
        {
            var graphRef = CreatePathGraphInternal(pathConnectingMap, pathIntersectMap, pathNeighbourMap, pathParallelMap);

            // CalcMemorySize(in graphRef);

            entityManager.SetComponentData(graphEntity, new PathGraphReference()
            {
                Graph = graphRef
            });

            return graphRef;
        }

        private static BlobAssetReference<PathBlobDataGraph> CreatePathGraphInternal(
            List<TrafficNodeConversionSystem.PathBlobDataTemp> pathConnectingMap,
            Dictionary<int, List<IntersectPathInfo>> pathIntersectMap,
            Dictionary<int, List<int>> pathNeighbourMap,
            Dictionary<int, List<int>> pathParallelMap)
        {
            using (var builder = new BlobBuilder(Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<PathBlobDataGraph>();
                var nodeArray = builder.Allocate(ref root.Paths, pathConnectingMap.Count);

                for (int pathIndex = 0; pathIndex < pathConnectingMap.Count; pathIndex++)
                {
                    nodeArray[pathIndex].PathLength = pathConnectingMap[pathIndex].PathLength;
                    nodeArray[pathIndex].ConnectedPathIndex = pathConnectingMap[pathIndex].ConnectedPathIndex;
                    nodeArray[pathIndex].SourceLaneIndex = pathConnectingMap[pathIndex].SourceLaneIndex;
                    nodeArray[pathIndex].Priority = pathConnectingMap[pathIndex].Priority;

                    nodeArray[pathIndex].Options = pathConnectingMap[pathIndex].Options;
                    nodeArray[pathIndex].PathCurveType = pathConnectingMap[pathIndex].PathCurveType;
                    nodeArray[pathIndex].PathRoadType = pathConnectingMap[pathIndex].PathRoadType;
                    nodeArray[pathIndex].PathConnectionType = pathConnectingMap[pathIndex].PathConnectionType;
                    nodeArray[pathIndex].TrafficGroup = pathConnectingMap[pathIndex].TrafficGroup;

                    var connectedPaths = builder.Allocate(ref nodeArray[pathIndex].NextConnectedPathsIndexes, pathConnectingMap[pathIndex].ConnectedIndexes.Count);

                    for (int j = 0; j < pathConnectingMap[pathIndex].ConnectedIndexes.Count; j++)
                    {
                        connectedPaths[j] = pathConnectingMap[pathIndex].ConnectedIndexes[j];
                    }

                    var nodes = builder.Allocate(ref nodeArray[pathIndex].RouteNodes, pathConnectingMap[pathIndex].Nodes.Count);

                    for (int j = 0; j < pathConnectingMap[pathIndex].Nodes.Count; j++)
                    {
                        nodes[j] = pathConnectingMap[pathIndex].Nodes[j];
                    }

                    List<IntersectPathInfo> intersectPathInfos;

                    if (pathIntersectMap.TryGetValue(pathIndex, out intersectPathInfos))
                    {
                        var intersectedPaths = builder.Allocate(ref nodeArray[pathIndex].IntersectedPaths, intersectPathInfos.Count);

                        for (int j = 0; j < intersectPathInfos.Count; j++)
                        {
                            intersectedPaths[j] = intersectPathInfos[j];
                        }
                    }

                    List<int> neighbourIndexes;

                    if (pathNeighbourMap.TryGetValue(pathIndex, out neighbourIndexes))
                    {
                        var neighbourPaths = builder.Allocate(ref nodeArray[pathIndex].NeighbourPathsIndexes, neighbourIndexes.Count);

                        for (int j = 0; j < neighbourIndexes.Count; j++)
                        {
                            neighbourPaths[j] = neighbourIndexes[j];
                        }
                    }

                    List<int> parallelIndexes;

                    if (pathParallelMap.TryGetValue(pathIndex, out parallelIndexes))
                    {
                        var parallelPaths = builder.Allocate(ref nodeArray[pathIndex].ParallelPathsIndexes, parallelIndexes.Count);

                        for (int j = 0; j < parallelIndexes.Count; j++)
                        {
                            parallelPaths[j] = parallelIndexes[j];
                        }
                    }
                }

                return builder.CreateBlobAssetReference<PathBlobDataGraph>(Allocator.Persistent);
            }
        }

        private static void CalcMemorySize(in BlobAssetReference<PathBlobDataGraph> graphRef)
        {
            long totalMemorySize = 0;

            ref var paths = ref graphRef.Value.Paths;

            for (int i = 0; i < paths.Length; i++)
            {
                ref var path = ref paths[i];

                for (int j = 0; j < path.NextConnectedPathsIndexes.Length; j++)
                {
                    totalMemorySize += sizeof(int); // Index
                }

                for (int j = 0; j < path.IntersectedPaths.Length; j++)
                {
                    totalMemorySize += sizeof(int); // Index
                    totalMemorySize += sizeof(float) * 3; // Position
                    totalMemorySize += sizeof(byte); // Local Index
                }

                for (int j = 0; j < path.NeighbourPathsIndexes.Length; j++)
                {
                    totalMemorySize += sizeof(int); // Index
                }

                for (int j = 0; j < path.ParallelPathsIndexes.Length; j++)
                {
                    totalMemorySize += sizeof(int); // Index
                }

                for (int j = 0; j < path.RouteNodes.Length; j++)
                {
                    totalMemorySize += sizeof(bool); // ForwardNodeDirection
                    totalMemorySize += sizeof(float); // SpeedLimit
                    totalMemorySize += sizeof(float) * 3; // Position
                    totalMemorySize += sizeof(TrafficGroupType); // TrafficGroup
                }

                totalMemorySize += sizeof(PathCurveType); // PathCurveType
                totalMemorySize += sizeof(PathRoadType); // PathRoadType
                totalMemorySize += sizeof(PathConnectionType); // PathConnectionType
                totalMemorySize += sizeof(TrafficGroupType); // TrafficGroup

                totalMemorySize += sizeof(float); // PathLength
                totalMemorySize += sizeof(int); // ConnectedPathIndex
                totalMemorySize += sizeof(int); // SourceLaneIndex
                totalMemorySize += sizeof(int); // Priority
                totalMemorySize += sizeof(bool); // EnterOfCrossroad
                totalMemorySize += sizeof(bool); // HasCustomNode
                totalMemorySize += sizeof(bool); // Rail
            }

            decimal memorySizeMb = (decimal)(totalMemorySize) * 0.000001m; // Convert byte to mb

            UnityEngine.Debug.Log($"Graph memory consumption {memorySizeMb} mb");
        }
    }
}