using Spirit604.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Spirit604.Gameplay.Road
{
    public static class TrafficNodeExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetLanePosition(this TrafficNode trafficNode, int laneIndex, bool leftLane = false) =>
            !leftLane ? GetRightSidePoint(trafficNode, laneIndex) : GetLeftSidePoint(trafficNode, laneIndex);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetRightSidePoint(this TrafficNode trafficNode, int laneIndex)
        {
            Vector3 lanePoint = Vector3.zero;
            var transform = trafficNode.transform;

            var custom = trafficNode.LaneCountType == LaneCountType.Custom;
            var oneway = trafficNode.IsOneWay || custom;

            if (!oneway)
            {
                lanePoint = transform.position + GetLaneOffset(transform.right, trafficNode.LaneWidth, laneIndex, true, trafficNode.DividerWidth);
            }
            else
            {
                if (custom)
                {
                    laneIndex += trafficNode.ExternalLaneCount;
                }

                lanePoint = GetOneWaypoint(trafficNode, laneIndex);

                if (custom)
                {
                    lanePoint -= transform.right * (trafficNode.DividerWidth / 2);
                }
            }

            return lanePoint;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetLeftSidePoint(this TrafficNode trafficNode, int laneIndex)
        {
            Vector3 lanePoint = default;
            var transform = trafficNode.transform;

            var custom = trafficNode.LaneCountType == LaneCountType.Custom;
            var oneway = trafficNode.IsOneWay || custom;

            if (!oneway)
            {
                lanePoint = transform.position + GetLaneOffset(transform.right, trafficNode.LaneWidth, laneIndex, false, trafficNode.DividerWidth);
            }
            else
            {
                if (custom)
                {
                    laneIndex = trafficNode.ExternalLaneCount - laneIndex - 1;
                }

                lanePoint = GetOneWaypoint(trafficNode, laneIndex);

                if (custom)
                {
                    lanePoint += transform.right * (trafficNode.DividerWidth / 2);
                }
            }

            return lanePoint;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetLaneOffset(Vector3 right, float laneWidth, int laneIndex, bool rightLane, float dividerWidth = 0)
        {
            int side = rightLane ? -1 : 1;
            return side * ProjectConstants.LaneHandDirection * right * (laneWidth / 2 + laneWidth * laneIndex + dividerWidth / 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetOneWaypoint(this TrafficNode trafficNode, int laneIndex)
        {
            return GetOneWaypoint(trafficNode, laneIndex, trafficNode.IsEndOfOneWay);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetOneWaypoint(this TrafficNode trafficNode, int laneIndex, bool flipDirection)
        {
            var transform = trafficNode.transform;
            return GetOneWaypoint(transform.position, transform.right, trafficNode.TotalLaneCount, trafficNode.LaneWidth, laneIndex, flipDirection);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetOneWaypoint(Vector3 position, Vector3 right, int laneCount, float laneWidth, int laneIndex, bool isEndOfOneWay)
        {
            int side = !isEndOfOneWay ? -1 : 1;
            var offset = GetOneWaypointOffset(laneCount, laneWidth, laneIndex);
            return position + side * right * offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetOneWaypointOffset(int laneCount, float laneWidth, int laneIndex)
        {
            float offset = default;

            if (laneCount % 2 == 0)
            {
                offset = (-((laneCount / 2 - 1) * laneWidth + laneWidth / 2) + laneWidth * laneIndex) * ProjectConstants.LaneHandDirection;
            }
            else
            {
                int sideOffset = Mathf.FloorToInt(((float)laneCount) / 2);

                offset = (laneWidth * (-sideOffset + laneIndex)) * ProjectConstants.LaneHandDirection;
            }

            return offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion GetNodeRotation(this TrafficNode trafficNode, int side)
        {
            return Quaternion.LookRotation(GetNodeForward(trafficNode, side));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetNodeForward(this TrafficNode trafficNode, bool rightSide)
        {
            int side;

            if (!trafficNode.IsOneWay)
            {
                side = rightSide ? 1 : -1;
            }
            else
            {
                side = !trafficNode.IsEndOfOneWay ? 1 : -1;
            }

            return GetNodeForward(trafficNode, side);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetNodeForward(this TrafficNode trafficNode, int side)
        {
            return -trafficNode.transform.forward * side;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetLaneDirection(this TrafficNode trafficNode, int side)
        {
            return -trafficNode.transform.forward * side * ProjectConstants.LaneHandDirection;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCorrectDirConnection(TrafficNode trafficNode, TrafficNode dstConnection, bool sourceRight, bool targetRight)
        {
            var forward1 = trafficNode.GetNodeForward(sourceRight);
            var forward2 = dstConnection.GetNodeForward(!targetRight);

            var connectionDir = (dstConnection.transform.position - trafficNode.transform.position).normalized;
            var dot = Vector3.Dot(forward1, connectionDir);
            var dot2 = Vector3.Dot(forward2, connectionDir);

            return dot > 0 && dot2 > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Path GetPathByAbsoluteIndex(this TrafficNode trafficNode, int absoluteIndex)
        {
            int index = 0;

            var lanes = trafficNode.Lanes;

            for (int laneIndex = 0; laneIndex < lanes?.Count; laneIndex++)
            {
                for (int i = 0; i < lanes[laneIndex].paths?.Count; i++)
                {
                    if (index == absoluteIndex)
                    {
                        return lanes[laneIndex].paths[i];
                    }

                    index++;
                }
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Path GetPathByLocalIndex(this TrafficNode trafficNode, int laneIndex, int localIndex, bool externalLanes = false)
        {
            var lanes = !externalLanes ? trafficNode.Lanes : trafficNode.ExternalLanes;

            if (lanes.Count > laneIndex)
            {
                if (lanes[laneIndex].paths.Count > localIndex)
                {
                    return lanes[laneIndex].paths[localIndex];
                }
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetPathCountAtLane(this TrafficNode trafficNode, int laneIndex, bool externalLanes = false)
        {
            var paths = GetAllPathsAtLane(trafficNode, laneIndex, externalLanes);

            if (paths != null)
                return paths.Count;

            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IList<Path> GetAllPathsAtLane(this TrafficNode trafficNode, int laneIndex, bool externalLanes = false)
        {
            if (!trafficNode.LaneExist(laneIndex, externalLanes, true))
                return null;

            if (!externalLanes)
                return trafficNode.Lanes[laneIndex].paths.AsReadOnly();
            else
                return trafficNode.ExternalLanes[laneIndex].paths.AsReadOnly();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IterateAllLanes(this TrafficNode trafficNode, Action<int, bool> callback, bool includeExternal = false, bool checkForExist = true)
        {
            if (trafficNode.HasRightLanes)
            {
                for (int laneIndex = 0; laneIndex < trafficNode.GetLaneCount(); laneIndex++)
                {
                    if (trafficNode.LaneExist(laneIndex, false) || !checkForExist)
                        callback?.Invoke(laneIndex, false);
                }
            }

            if (includeExternal)
            {
                IterateExternalLanes(trafficNode, callback, checkForExist);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IterateExternalLanes(this TrafficNode trafficNode, Action<int, bool> callback, bool checkForExist = true)
        {
            if (!trafficNode.HasLeftLanes)
                return;

            for (int laneIndex = 0; laneIndex < trafficNode.GetLaneCount(true); laneIndex++)
            {
                if (trafficNode.LaneExist(laneIndex, true) || !checkForExist)
                    callback?.Invoke(laneIndex, true);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IterateAllLanes(this TrafficNode trafficNode, Action<LaneArray, int, bool> callback, bool includeExternal = false, bool includeNull = false)
        {
            if (trafficNode.HasRightLanes)
            {
                for (int laneIndex = 0; laneIndex < trafficNode.GetLaneCount(); laneIndex++)
                {
                    var data = trafficNode.TryToGetLaneData(laneIndex);

                    if (data != null || includeNull)
                        callback?.Invoke(data, laneIndex, false);
                }
            }

            if (includeExternal)
            {
                IterateExternalLanes(trafficNode, callback, includeNull: includeNull);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IterateExternalLanes(this TrafficNode trafficNode, Action<LaneArray, int, bool> callback, bool includeNull = false)
        {
            if (!trafficNode.HasLeftLanes)
                return;

            for (int laneIndex = 0; laneIndex < trafficNode.GetLaneCount(true); laneIndex++)
            {
                var data = trafficNode.TryToGetLaneData(laneIndex, true);

                if (data != null || includeNull)
                    callback?.Invoke(data, laneIndex, true);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IterateAllPathsOnLane(this TrafficNode trafficNode, int laneIndex, Action<Path> callback, bool includeExternal = false)
        {
            var lanes = trafficNode.Lanes;

            if (trafficNode.HasRightLanes && lanes?.Count > laneIndex)
            {
                var paths = lanes[laneIndex].paths;

                for (int i = 0; i < paths?.Count; i++)
                {
                    var path = lanes[laneIndex].paths[i];

                    if (path != null)
                        callback(path);
                }
            }

            if (includeExternal)
            {
                IterateExternalPathsOnLane(trafficNode, laneIndex, callback);
            }
        }

        public static void IterateAllPaths(this TrafficNode trafficNode, Action<Path> callback, bool includeExternal = false)
        {
            var lanes = trafficNode.Lanes;

            for (int laneIndex = 0; laneIndex < lanes?.Count; laneIndex++)
            {
                var paths = lanes[laneIndex].paths;

                for (int i = 0; i < paths?.Count; i++)
                {
                    var path = lanes[laneIndex].paths[i];

                    if (path != null)
                        callback(path);
                }
            }

            if (includeExternal)
            {
                IterateExternalPaths(trafficNode, callback);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IterateAllPaths(this TrafficNode trafficNode, Action<Path, int> callback, bool includeExternal = false)
        {
            var lanes = trafficNode.Lanes;

            for (int laneIndex = 0; laneIndex < lanes?.Count; laneIndex++)
            {
                for (int i = 0; i < lanes[laneIndex].paths?.Count; i++)
                {
                    var path = lanes[laneIndex].paths[i];

                    if (path != null)
                        callback(path, laneIndex);
                }
            }

            if (includeExternal)
            {
                IterateExternalPaths(trafficNode, callback);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IterateExternalPaths(this TrafficNode trafficNode, Action<Path> callback)
        {
            var externalLanes = trafficNode.ExternalLanes;

            for (int laneIndex = 0; laneIndex < externalLanes?.Count; laneIndex++)
            {
                for (int i = 0; i < externalLanes[laneIndex].paths?.Count; i++)
                {
                    var path = externalLanes[laneIndex].paths[i];

                    if (path != null)
                        callback(path);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IterateExternalPathsOnLane(this TrafficNode trafficNode, int laneIndex, Action<Path> callback)
        {
            var externalLanes = trafficNode.ExternalLanes;

            if (!trafficNode.HasLeftLanes || externalLanes == null || externalLanes.Count <= laneIndex)
                return;

            for (int i = 0; i < externalLanes[laneIndex].paths?.Count; i++)
            {
                var path = externalLanes[laneIndex].paths[i];

                if (path != null)
                    callback(path);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IterateExternalPaths(this TrafficNode trafficNode, Action<Path, int> callback)
        {
            var externalLanes = trafficNode.ExternalLanes;

            for (int laneIndex = 0; laneIndex < externalLanes?.Count; laneIndex++)
            {
                for (int i = 0; i < externalLanes[laneIndex].paths?.Count; i++)
                {
                    var path = externalLanes[laneIndex].paths[i];

                    if (path != null)
                        callback(path, laneIndex);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IterateAllOuterPaths(this TrafficNode trafficNode, Action<Path> callback)
        {
#if UNITY_EDITOR
            if (trafficNode.AllConnectedOuterPaths == null)
            {
                trafficNode.OnInspectorEnabled();
            }

            for (int i = 0; i < trafficNode.AllConnectedOuterPaths?.Count; i++)
            {
                Path path = trafficNode.AllConnectedOuterPaths[i];

                if (!path) continue;

                callback(path);
            }
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<Path> GetAllConnectedOuterPaths(this TrafficNode trafficNode, bool allowCrossroadPath = false)
        {
            if (!trafficNode.IsOneWay)
            {
                var paths = new List<Path>();

                trafficNode.IterateExternalPaths((path) =>
                {
                    if (path.ConnectedTrafficNode != null)
                    {
                        var connectedTrafficNode = path.ConnectedTrafficNode;

                        connectedTrafficNode.IterateExternalPaths((path2) =>
                        {
                            if (path2.ConnectedTrafficNode == trafficNode)
                            {
                                paths.TryToAdd(path2);
                            }
                        });
                    }
                });

                if (allowCrossroadPath && trafficNode.TrafficLightCrossroad)
                {
                    foreach (var currentTrafficNode in trafficNode.TrafficLightCrossroad.TrafficNodes)
                    {
                        if (currentTrafficNode == trafficNode || currentTrafficNode == null)
                            continue;

                        currentTrafficNode.IterateAllPaths((path) =>
                        {
                            if (path.ConnectedTrafficNode == trafficNode)
                            {
                                paths.TryToAdd(path);
                            }
                        });
                    }
                }

                return paths;
            }
            else
            {
                List<Path> paths = null;
                GameObject root = null;

#if UNITY_EDITOR
                root = EditorExtension.GetPrefabStageRoot();
#endif

                if (root == null)
                {
                    if (!allowCrossroadPath && trafficNode.TrafficLightCrossroad)
                    {
                        paths = ObjectUtils.FindObjectsOfType<Path>().Where(a => a.ConnectedTrafficNode == trafficNode && a.SourceTrafficNode != null && !trafficNode.TrafficLightCrossroad.TrafficNodes.Contains(a.SourceTrafficNode)).ToList();
                    }
                    else
                    {
                        paths = ObjectUtils.FindObjectsOfType<Path>().Where(a => a.ConnectedTrafficNode == trafficNode && a.SourceTrafficNode != null).ToList();
                    }
                }
                else
                {
                    if (!allowCrossroadPath && trafficNode.TrafficLightCrossroad)
                    {
                        paths = root.GetComponentsInChildren<Path>().Where(a => a.ConnectedTrafficNode == trafficNode && a.SourceTrafficNode != null && !trafficNode.TrafficLightCrossroad.TrafficNodes.Contains(a.SourceTrafficNode)).ToList();
                    }
                    else
                    {
                        paths = root.GetComponentsInChildren<Path>().Where(a => a.ConnectedTrafficNode == trafficNode && a.SourceTrafficNode != null).ToList();
                    }
                }

                return paths;
            }
        }

        public static void CloneSettings(this TrafficNode trafficNode, TrafficNode sourceNode, bool includeTRS = false, bool inverse = false)
        {
            trafficNode.LaneCount = sourceNode.LaneCount;
            trafficNode.LaneCountType = sourceNode.LaneCountType;
            trafficNode.ExternalLaneCount = sourceNode.ExternalLaneCount;
            trafficNode.LaneWidth = sourceNode.LaneWidth;
            trafficNode.HasCrosswalk = sourceNode.HasCrosswalk;
            trafficNode.LightType = sourceNode.LightType;
            trafficNode.TrafficNodeType = sourceNode.TrafficNodeType;
            trafficNode.Weight = sourceNode.Weight;
            trafficNode.ChanceToSpawn = sourceNode.ChanceToSpawn;
            trafficNode.CustomAchieveDistance = sourceNode.CustomAchieveDistance;
            trafficNode.DividerWidth = sourceNode.DividerWidth;

            trafficNode.IsOneWay = sourceNode.IsOneWay;
            trafficNode.IsEndOfOneWay = sourceNode.IsEndOfOneWay;

            if (includeTRS)
            {
                trafficNode.transform.position = sourceNode.transform.position;
                trafficNode.transform.rotation = sourceNode.transform.rotation;
            }

            trafficNode.TrafficNodeCrosswalk.SwitchConnectionState(sourceNode.TrafficNodeCrosswalk.HasConnection);
            trafficNode.TrafficNodeCrosswalk.SwitchEnabledState(sourceNode.TrafficNodeCrosswalk.Enabled);

            if (inverse)
            {
                if (trafficNode.IsOneWay)
                {
                    trafficNode.IsEndOfOneWay = !sourceNode.IsEndOfOneWay;
                }

                if (includeTRS)
                {
                    trafficNode.transform.rotation *= Quaternion.Euler(0, 180, 0);
                }

                if (trafficNode.LaneCountType == LaneCountType.Custom)
                {
                    var temp = trafficNode.GetLaneCount(false);
                    trafficNode.LaneCount = trafficNode.GetLaneCount(true);
                    trafficNode.ExternalLaneCount = temp;
                }
            }

            trafficNode.Resize();

            EditorSaver.SetObjectDirty(trafficNode);
        }

        public static (int, bool) GetClosestLane(this TrafficNode trafficNode, Vector3 worldPos)
        {
            return GetClosestLane(trafficNode, worldPos, out var pos);
        }

        public static (int, bool) GetClosestLane(this TrafficNode trafficNode, Vector3 worldPos, out Vector3 position)
        {
            if (worldPos == default)
                throw new ArgumentException("GetClosestLane worldPos is Vector.zero");

            float maxDistance = float.MaxValue;

            int closestLaneIndex = -1;
            bool closestLeftLane = false;
            Vector3 localPosition = default;

            trafficNode.IterateAllLanes((laneIndex, external) =>
            {
                var lanePos = trafficNode.GetLanePosition(laneIndex, external);

                var distance = Vector3.SqrMagnitude(lanePos - worldPos);

                if (maxDistance > distance)
                {
                    localPosition = lanePos;
                    maxDistance = distance;
                    closestLaneIndex = laneIndex;
                    closestLeftLane = external;
                }

            }, true, false);

            position = localPosition;

            return (closestLaneIndex, closestLeftLane);
        }

        public static float CalculatedRouteWidth(int laneCount, float laneWidth, float dividerWidth, bool isOneWay)
        {
            return !isOneWay ? laneCount * laneWidth + dividerWidth / 2 : laneCount * (laneWidth / 2);
        }

        public static Vector3 CalculateCrosswalkOffset(
            Vector3 nodePosition,
            Quaternion nodeRotation,
            int laneCount,
            float laneWidth,
            float dividerWidth,
            bool isOneWay,
            Vector3 worldPoint)
        {
            var forward = nodeRotation * -Vector3.forward;
            var routeWidth = CalculatedRouteWidth(laneCount, laneWidth, dividerWidth, isOneWay);

            var right = nodeRotation * Vector3.right;

            float dotProduct = Vector3.Dot(right, Vector3.Normalize(nodePosition - worldPoint));

            var side = dotProduct < 0 ? -1 : 1;

            var originPoint = nodePosition - side * right * routeWidth;

            var crosswalk = (Quaternion.Inverse(nodeRotation) * (worldPoint - originPoint).Flat());

            crosswalk.x *= -side;
            return crosswalk;
        }
    }
}