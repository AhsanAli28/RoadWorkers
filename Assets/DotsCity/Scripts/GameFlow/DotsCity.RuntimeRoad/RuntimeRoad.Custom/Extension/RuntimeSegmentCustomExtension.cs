using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad.Custom
{
    public static class RuntimeSegmentCustomExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IterateAllNodes(this RuntimeSegmentCustom segment, Action<TrafficNodeData> method)
        {
            for (int i = 0; i < segment.Nodes.Count; i++)
            {
                method.Invoke(segment.Nodes[i]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IterateAllNodePaths(this RuntimeSegmentCustom segment, TrafficNodeData node, Action<PathData> action)
        {
            for (int i = node.MinLocalPathIndex; i < node.MaxLocalPathIndex; i++)
            {
                action.Invoke(segment.AllPaths[i]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IterateAllPaths(this RuntimeSegmentCustom segment, Action<PathData> action)
        {
            for (int i = 0; i < segment.AllPaths.Count; i++)
            {
                action.Invoke(segment.AllPaths[i]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IterateAllLanes(this RuntimeSegmentCustom segment, TrafficNodeData node, Action<int, bool> action, bool includeExternal = true)
        {
            if (node.HasRightLanes())
            {
                for (int i = 0; i < node.LaneCount; i++)
                {
                    action(i, false);
                }
            }

            if (includeExternal && node.HasLeftLanes())
            {
                for (int i = 0; i < node.LaneCount; i++)
                {
                    action(i, true);
                }
            }
        }

        public static TrafficNodeData GetClosestNode(this RuntimeSegmentCustom segment, Vector3 point, bool checkClosestLane = false)
        {
            if (point == default)
                throw new ArgumentException("GetClosestNode point is Vector.zero");

            var maxDistance = float.MaxValue;

            TrafficNodeData closest = null;

            segment.IterateAllNodes(node =>
            {
                var distance = 0f;

                if (checkClosestLane)
                {
                    node.GetClosestLane(point, out var pos);
                    distance = Vector3.SqrMagnitude(point - pos);
                }
                else
                {
                    distance = Vector3.SqrMagnitude(point - node.Position);
                }

                if (distance < maxDistance)
                {
                    maxDistance = distance;
                    closest = node;
                }
            });

            return closest;
        }
        
        public static TrafficNodeData GetClosestNode(IEnumerable<RuntimeSegmentCustom> segments, Vector3 point, bool checkClosestLane = false)
        {
            if (point == default)
                throw new ArgumentException("GetClosestNode point is Vector.zero");

            var maxDistance = float.MaxValue;

            TrafficNodeData closest = null;

            foreach (var segment in segments)
            {
                segment.IterateAllNodes(node =>
                {
                    var distance = 0f;

                    if (checkClosestLane)
                    {
                        node.GetClosestLane(point, out var pos);
                        distance = Vector3.SqrMagnitude(point - pos);
                    }
                    else
                    {
                        distance = Vector3.SqrMagnitude(point - node.Position);
                    }

                    if (distance < maxDistance)
                    {
                        maxDistance = distance;
                        closest = node;
                    }
                });
            }

            return closest;
        }

        public static void GetClosestLaneOnSegment(this RuntimeSegmentCustom segment, Vector3 point, out TrafficNodeData closestNode, out int laneIndex, out bool external, out Vector3 position)
        {
            if (point == default)
                throw new ArgumentException("GetClosestNode point is Vector.zero");

            var maxDistance = float.MaxValue;

            int laneIndexLocal = -1;
            bool externalLocal = false;
            Vector3 positionLocal = default;
            TrafficNodeData closestNodeLocal = null;

            segment.IterateAllNodes(node =>
            {
                var laneData = node.GetClosestLane(point, out var pos);

                var distance = Vector3.SqrMagnitude(point - pos);

                if (distance < maxDistance)
                {
                    maxDistance = distance;
                    closestNodeLocal = node;
                    laneIndexLocal = laneData.Item1;
                    externalLocal = laneData.Item2;
                    positionLocal = pos;
                }
            });

            laneIndex = laneIndexLocal;
            external = externalLocal;
            position = positionLocal;
            closestNode = closestNodeLocal;
        }
    }
}