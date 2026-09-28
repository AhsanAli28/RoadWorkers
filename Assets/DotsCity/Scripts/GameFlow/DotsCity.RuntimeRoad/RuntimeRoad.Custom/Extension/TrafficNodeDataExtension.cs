using Spirit604.CityEditor.Utils;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad.Custom
{
    public static class TrafficNodeDataExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (int, bool) GetClosestLane(this TrafficNodeData trafficNode, Vector3 worldPos, out Vector3 position)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetSourcePathsNonAlloc(this TrafficNodeData trafficNodeData, int sourceLaneIndex, List<PathData> tempPaths)
        {
            var owner = trafficNodeData.Owner;

            owner.IterateAllNodePaths(trafficNodeData, path =>
            {
                if (path.SourceLaneIndex == sourceLaneIndex)
                {
                    tempPaths.Add(path);
                }
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetSourcePathsNonAlloc(this TrafficNodeData trafficNodeData, int sourceLaneIndex, List<PathData> tempPaths, bool external)
        {
            var owner = trafficNodeData.Owner;

            owner.IterateAllNodePaths(trafficNodeData, path =>
            {
                if (path.SourceLaneIndex == sourceLaneIndex && path.External == external)
                {
                    tempPaths.Add(path);
                }
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetAllPathsConnectedToNonAlloc(this TrafficNodeData trafficNodeData, int sourceLaneIndex, List<PathData> tempPaths)
        {
            var owner = trafficNodeData.Owner;
            var nodeIndex = owner.GetNodeIndex(trafficNodeData);

            owner.IterateAllPaths(path =>
            {
                if (path.ConnectedNodeIndex == nodeIndex && path.ConnectedLaneIndex == sourceLaneIndex)
                {
                    tempPaths.Add(path);
                }
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IterateAllLanes(this TrafficNodeData trafficNode, Action<int, bool> callback, bool includeExternal = false, bool checkForExist = true)
        {
            if (trafficNode.HasRightLanes())
            {
                for (int laneIndex = 0; laneIndex < trafficNode.LaneCount; laneIndex++)
                {
                    callback?.Invoke(laneIndex, false);
                }
            }

            if (includeExternal)
            {
                if (trafficNode.HasLeftLanes())
                {
                    for (int laneIndex = 0; laneIndex < trafficNode.LaneCount; laneIndex++)
                    {
                        callback?.Invoke(laneIndex, true);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasRightLanes(this TrafficNodeData trafficNode) => !trafficNode.IsOneWay || trafficNode.IsOneWay && !trafficNode.IsEndOfOneWay;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasLeftLanes(this TrafficNodeData trafficNode) => !trafficNode.IsOneWay || trafficNode.IsOneWay && trafficNode.IsEndOfOneWay;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetLanePosition(this TrafficNodeData trafficNode, int laneIndex, bool leftLane = false) =>
            !leftLane ? GetRightSidePoint(trafficNode, laneIndex) : GetLeftSidePoint(trafficNode, laneIndex);

        public static Quaternion GetNodeRotation(this TrafficNodeData trafficNode, int side) //=> side == -1 && (!trafficNode.IsOneWay || trafficNode.IsOneWay && trafficNode.IsEndOfOneWay) ? trafficNode.Rotation : trafficNode.Rotation * Quaternion.Euler(0, 180, 0);
        {
            if (!trafficNode.IsOneWay)
            {
                return side == -1 ? trafficNode.Rotation : trafficNode.Rotation * Quaternion.Euler(0, 180, 0);
            }
            else
            {
                return trafficNode.IsEndOfOneWay ? trafficNode.Rotation : trafficNode.Rotation * Quaternion.Euler(0, 180, 0);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetRightSidePoint(this TrafficNodeData trafficNode, int laneIndex)
        {
            Vector3 lanePoint = Vector3.zero;

            if (!trafficNode.IsOneWay)
            {
                lanePoint = trafficNode.Position + TrafficNodeExtension.GetLaneOffset(trafficNode.Right, trafficNode.LaneWidth, laneIndex, true, trafficNode.DividerWidth);
            }
            else
            {
                lanePoint = GetOneWaypoint(trafficNode, laneIndex);
            }

            return lanePoint;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetLeftSidePoint(this TrafficNodeData trafficNode, int laneIndex)
        {
            Vector3 lanePoint = default;

            if (!trafficNode.IsOneWay)
            {
                lanePoint = trafficNode.Position + TrafficNodeExtension.GetLaneOffset(trafficNode.Right, trafficNode.LaneWidth, laneIndex, false, trafficNode.DividerWidth);
            }
            else
            {
                lanePoint = GetOneWaypoint(trafficNode, laneIndex);
            }

            return lanePoint;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetOneWaypoint(this TrafficNodeData trafficNode, int laneIndex)
        {
            return GetOneWaypoint(trafficNode, laneIndex, trafficNode.IsEndOfOneWay);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetOneWaypoint(this TrafficNodeData trafficNode, int laneIndex, bool flipDirection)
        {
            return TrafficNodeExtension.GetOneWaypoint(trafficNode.Position, trafficNode.Right, trafficNode.LaneCount, trafficNode.LaneWidth, laneIndex, flipDirection);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RoadSceneUtils.NodeSettings GetSettings(this TrafficNodeData trafficNode)
        {
            return new RoadSceneUtils.NodeSettings()
            {
                Position = trafficNode.Position,
                Rotation = trafficNode.Rotation,
                Settings = new RoadSceneUtils.BaseSettings()
                {
                    LaneCount = trafficNode.LaneCount,
                    LaneWidth = trafficNode.LaneWidth,
                    DividerWidth = trafficNode.DividerWidth,
                    IsOneWay = trafficNode.IsOneWay,
                    IsEndOfOneWay = trafficNode.IsEndOfOneWay,
                    AddPedestrianNodes = trafficNode.AddPedestrianNodes,
                    AddCrosswalk = trafficNode.AddCrosswalk,
                    CrosswalkOffset = trafficNode.CrosswalkOffset
                }
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRight(this TrafficNodeData trafficNode, Vector3 worldPos, bool inverse = false)
        {
            int side = !inverse ? -1 : 1;
            return Vector3.SignedAngle(side * trafficNode.Forward, Vector3.Normalize(worldPos - trafficNode.Position), Vector3.up) > 0;
        }
    }
}