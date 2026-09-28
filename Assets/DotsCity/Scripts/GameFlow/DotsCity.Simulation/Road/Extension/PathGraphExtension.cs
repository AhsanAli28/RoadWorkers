using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Road
{
    public static class PathGraphExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCreated(in this PathGraphSystem.Singleton graph) => graph.allPaths.IsCreated;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Count(in this PathGraphSystem.Singleton graph)
        {
            return graph.allPaths.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasOption(in this PathGraphSystem.PathData pathData, PathOptions option)
        {
            return DotsEnumExtension.HasFlagUnsafe(pathData.Options, option);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAvailable(in this PathGraphSystem.Singleton graph, int pathIndex, in TrafficTypeComponent trafficTypeComponent)
        {
            ref readonly var pathData = ref graph.GetPathData(pathIndex);
            return IsAvailable(in pathData, trafficTypeComponent.TrafficGroup);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAvailable(in this PathGraphSystem.PathData pathData, in TrafficTypeComponent trafficTypeComponent)
        {
            return IsAvailable(in pathData, trafficTypeComponent.TrafficGroup);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAvailable(in this PathGraphSystem.PathData pathData, TrafficGroupType trafficGroupType)
        {
            return DotsEnumExtension.HasFlagUnsafe(pathData.TrafficGroup, trafficGroupType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAvailable(in this PathGraphSystem.Singleton graph, int pathIndex, int nodeIndex, in TrafficTypeComponent trafficTypeComponent)
        {
            ref readonly var routeNode = ref graph.GetPathNodeData(pathIndex, nodeIndex);
            return IsAvailable(in routeNode, trafficTypeComponent.TrafficGroup);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAvailable(in this PathGraphSystem.Singleton graph, in PathGraphSystem.PathData pathData, int nodeIndex, in TrafficTypeComponent trafficTypeComponent)
        {
            ref readonly var routeNode = ref graph.GetPathNodeData(in pathData, nodeIndex);
            return IsAvailable(in routeNode, trafficTypeComponent.TrafficGroup);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAvailable(in this RouteNodeData routeNode, in TrafficTypeComponent trafficTypeComponent)
        {
            return IsAvailable(in routeNode, trafficTypeComponent.TrafficGroup);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAvailable(in this RouteNodeData routeNode, TrafficGroupType trafficGroupType)
        {
            return DotsEnumExtension.HasFlagUnsafe(routeNode.TrafficGroup, trafficGroupType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 GetPositionOnRoad(in this PathGraphSystem.Singleton graph, int pathIndex, float targetPathLength)
        {
            GetPositionOnRoad(in graph, pathIndex, targetPathLength, out var spawnPosition, out var spawnDirection, out var pathNodeIndex);
            return spawnPosition;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetPositionOnRoad(in this PathGraphSystem.Singleton graph, int pathIndex, float targetPathLength, out float3 spawnPosition, out float3 spawnDirection, out int pathNodeIndex)
        {
            var pathNodes = graph.GetRouteNodes(pathIndex);

            spawnPosition = default;
            spawnDirection = default;
            pathNodeIndex = -1;

            float currentDistance = 0;
            float prevCurrentDistance = 0;

            var minIndex = 0;
            var maxIndex = pathNodes.Length - 1;

            for (int index = minIndex; index < maxIndex; index++)
            {
                float3 nodePosition = pathNodes[index].Position;
                float3 nextNodePosition = pathNodes[index + 1].Position;

                float distance = math.distance(nodePosition, nextNodePosition);

                currentDistance += distance;

                if (currentDistance >= targetPathLength)
                {
                    var spawnOffset = targetPathLength - prevCurrentDistance;
                    spawnDirection = math.normalize(nextNodePosition - nodePosition);
                    spawnPosition = nodePosition + spawnDirection * spawnOffset;
                    pathNodeIndex = index;
                    return;
                }

                prevCurrentDistance = currentDistance;
            }

            if (pathNodeIndex == -1 && pathNodes.Length >= 2)
            {
                pathNodeIndex = maxIndex;
                spawnPosition = pathNodes[maxIndex].Position;
                spawnDirection = math.normalize(pathNodes[maxIndex].Position - pathNodes[maxIndex - 1].Position);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetTargetWaypointIndexByPoint(in this PathGraphSystem.Singleton graph, int pathIndex, float3 sourcePoint)
        {
            var pathNodes = graph.GetRouteNodes(pathIndex);

            for (int i = 0; i < pathNodes.Length - 1; i++)
            {
                var wayPoint = pathNodes[i];
                var wayPoint2 = pathNodes[i + 1];

                if (VectorExtensions.IsBetween3DSpace(wayPoint.Position, wayPoint2.Position, sourcePoint))
                {
                    return i + 1;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 GetClosestPoint(in this PathGraphSystem.Singleton graph, int pathIndex, float3 sourcePosition)
        {
            var p = sourcePosition;

            var pathNodes = graph.GetRouteNodes(pathIndex);

            float3 point = default;

            for (int i = 0; i < pathNodes.Length - 1; i++)
            {
                var a = pathNodes[i].Position;
                var b = pathNodes[i + 1].Position;

                float3 ab = b - a;
                float3 ap = p - a;
                float3 ar = math.project(ap, ab);

                bool found = true;

                if (a.Equals(b))
                {
                    found = false;
                }

                else if (math.dot(ab, ar) < 0)
                {
                    found = false;
                }
                else if (UnityEngine.Vector3.SqrMagnitude(ar) > UnityEngine.Vector3.SqrMagnitude(ab))
                {
                    found = false;
                }

                if (!found)
                {
                    continue;
                }

                var tempPoint = a + ar;

                if (point.Equals(float3.zero))
                {
                    point = tempPoint;
                }
                else
                {
                    float dist1 = math.distancesq(point, sourcePosition);
                    float dist2 = math.distancesq(tempPoint, sourcePosition);

                    if (dist2 < dist1)
                    {
                        point = tempPoint;
                    }
                }
            }

            if (point.Equals(float3.zero))
            {
                var a1 = pathNodes[0].Position;
                var a2 = pathNodes[pathNodes.Length - 1].Position;

                float val1 = math.distancesq(p, a1);
                float val2 = math.distancesq(p, a2);

                if (val1 < val2)
                {
                    return a1;
                }
                else
                {
                    return a2;
                }
            }

            return point;
        }
    }
}
