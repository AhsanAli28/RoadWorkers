#if UNITY_EDITOR
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public static class PathAttachHelper
    {
        public struct PathAttachInfo
        {
            public TrafficNode SourceTrafficNode;
            public TrafficNode TargetTrafficNode;
            public Path SelectedPath;
            public int SourceLaneIndex;
            public int TargetLaneIndex;
            public bool ShouldReparent;
            public bool AttachToNodes;
            public bool IsRightSide;
            public bool ConnectSameIndex;
        }

        public static void Attach(PathAttachInfo pathAttachInfo)
        {
            var sourceTrafficNode = pathAttachInfo.SourceTrafficNode;
            var targetTrafficNode = pathAttachInfo.TargetTrafficNode;
            var selectedPath = pathAttachInfo.SelectedPath;

            if (sourceTrafficNode == null || targetTrafficNode == null || selectedPath == null)
                return;

            Undo.RegisterCompleteObjectUndo(sourceTrafficNode, "Revert Source Node");
            Undo.RegisterCompleteObjectUndo(targetTrafficNode, "Revert Target Node");
            Undo.RegisterCompleteObjectUndo(selectedPath, "Revert Path");

            if (selectedPath.SourceTrafficNode != null)
            {
                Undo.RegisterCompleteObjectUndo(selectedPath.SourceTrafficNode, "Revert Path Source Node");
                selectedPath.SourceTrafficNode.TryToRemovePath(pathAttachInfo.SelectedPath);
            }

            selectedPath.SaveMovementUndo();

            int connectedLaneIndex = pathAttachInfo.SourceLaneIndex;

            if (!pathAttachInfo.ConnectSameIndex)
            {
                connectedLaneIndex = pathAttachInfo.TargetLaneIndex;
            }

            if (selectedPath.Nodes.Count < 2)
            {
                int nodesToAdd = 2 - selectedPath.Nodes.Count;

                for (int i = 0; i < nodesToAdd; i++)
                {
                    selectedPath.AddNode(true);
                }
            }

            Vector3 oldPosition = selectedPath.transform.position;

            if (pathAttachInfo.AttachToNodes)
            {
                selectedPath.transform.localPosition = Vector3.zero;

                if (pathAttachInfo.IsRightSide)
                {
                    if (selectedPath.Nodes.Count > 0)
                    {
                        selectedPath.Nodes[0].transform.position = sourceTrafficNode.GetLanePosition(pathAttachInfo.SourceLaneIndex);
                    }

                    if (selectedPath.Nodes.Count > 1)
                    {
                        selectedPath.Nodes.Last().transform.position = targetTrafficNode.GetLanePosition(connectedLaneIndex, true);
                    }
                }
                else
                {
                    if (selectedPath.Nodes.Count > 0)
                    {
                        selectedPath.Nodes[0].transform.position = sourceTrafficNode.GetLanePosition(pathAttachInfo.SourceLaneIndex, true);
                    }

                    if (selectedPath.Nodes.Count > 1)
                    {
                        selectedPath.Nodes.Last().transform.position = targetTrafficNode.GetLanePosition(connectedLaneIndex);
                    }
                }
            }

            selectedPath.SourceTrafficNode = sourceTrafficNode;
            selectedPath.ConnectedTrafficNode = targetTrafficNode;
            selectedPath.ConnectedLaneIndex = connectedLaneIndex;

            bool saveUndo = true;
            sourceTrafficNode.AddPath(selectedPath, pathAttachInfo.SourceLaneIndex, pathAttachInfo.ShouldReparent, !pathAttachInfo.IsRightSide, saveUndo);

            if (pathAttachInfo.ShouldReparent)
            {
                Vector3 offset = selectedPath.transform.position - oldPosition;

                for (int i = 1; i < selectedPath.Nodes.Count - 1; i++)
                {
                    selectedPath.Nodes[i].transform.position = selectedPath.Nodes[i].transform.position - offset;
                }
            }

            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

            selectedPath.CreatePath(false);

            EditorSaver.SetObjectDirty(selectedPath);
        }

        public static Vector3 GetSourceAttachPosition(this Path path, int laneIndex, bool sourceExternal)
        {
            if (!path.SourceTrafficNode)
                return default;

            return path.SourceTrafficNode.GetLanePosition(laneIndex, sourceExternal);
        }

        public static Vector3 GetTargetAttachPosition(this Path path, int laneIndex, bool connectedExternal)
        {
            if (!path.ConnectedTrafficNode)
                return default;

            var defaultConnection = true;

            if (path.CanUseReverseConnection)
                defaultConnection = !path.ReversedConnectionSide;

            if (connectedExternal)
            {
                defaultConnection = !defaultConnection;
            }

            return path.ConnectedTrafficNode.GetLanePosition(laneIndex, defaultConnection);
        }

        public static List<Vector3> GetPointsAlongSurface(Vector3 srcPoint, Vector3 dstPoint, LayerMask layerMask, float castOffset, float angleThreshold, float surfaceOffset = 0, bool debug = false)
        {
            List<Vector3> points = new List<Vector3>();
            RaycastHit hit;

            var dist = Vector3.Distance(srcPoint, dstPoint);
            var castCount = Mathf.FloorToInt(dist / castOffset);

            Vector3 normal = Vector3.up;

            if (CastPoint(srcPoint, out hit, layerMask, debug))
            {
                normal = hit.normal;
            }

            float castStep = castOffset / dist;

            for (int j = 0; j < castCount - 1; j++)
            {
                var pos = Vector3.Lerp(srcPoint, dstPoint, (j + 1) * castStep);

                if (CastPoint(pos, out hit, layerMask, debug))
                {
                    var newNormal = hit.normal;
                    var angle = Vector3.Angle(normal, newNormal);

                    if (angle > angleThreshold)
                    {
                        normal = newNormal;
                        points.Add(hit.point + hit.normal * surfaceOffset);
                    }
                }
            }

            return points;
        }

        public static void SnapToSurface(this Path path, LayerMask layerMask, float surfaceOffset = 0, bool recordUndo = true, GameObject customSnapObject = null)
        {
            bool connected = path.HasConnection;

            var skipNodes = connected ? 1 : 0;

            for (int i = 1; i < path.Nodes?.Count - skipNodes; i++)
            {
                Transform node = path.Nodes[i];

                if (node == null) continue;

                SnapUtils.SnapToSurface(node, layerMask, surfaceOffset, recordUndo: recordUndo);
            }

            var firstNode = path.Nodes[0].transform;

            if (recordUndo)
            {
#if UNITY_EDITOR
                Undo.RegisterCompleteObjectUndo(firstNode, "Change Node Position");
#endif
            }

            firstNode.localPosition = firstNode.localPosition.SetY(0);

            if (connected)
            {
                var lastNode = path.Nodes.Last().transform;

                if (recordUndo)
                {
#if UNITY_EDITOR
                    Undo.RegisterCompleteObjectUndo(lastNode, "Change Node Position");
#endif
                }

                switch (path.PathConnectionType)
                {
                    case PathConnectionType.TrafficNode:
                        {
                            lastNode.position = new Vector3(lastNode.position.x, path.ConnectedTrafficNode.transform.position.y, lastNode.position.z);
                            break;
                        }
                    case PathConnectionType.PathPoint:
                        {
                            lastNode.position = PathHelper.GetAttachPoint(path.ConnectedPath, lastNode.position);
                            break;
                        }
                }
            }

            path.CreatePath(false);
        }

        /// <summary> Corner point for Cubic Bezier. </summary> 
        public static Vector3 GetSplineCornerPoint(Vector3 sourcePoint, Vector3 targetPoint, TrafficNode sourceNode, TrafficNode targetNode, bool allowClosestPoint = false)
        {
            return GetSplineCornerPoint(sourcePoint, targetPoint, -sourceNode.transform.forward, -targetNode.transform.forward, allowClosestPoint);
        }

        /// <summary> Two corner points for Quad Bezier. </summary> 
        public static (Vector3, Vector3) GetSplineTwoCornerPoint(Vector3 sourcePoint, Vector3 targetPoint, TrafficNode sourceNode, TrafficNode targetNode, bool allowClosestPoint = false)
        {
            return GetSplineTwoCornerPoint(sourcePoint, targetPoint, -sourceNode.transform.forward, -targetNode.transform.forward, allowClosestPoint);
        }

        public static (Vector3, Vector3) GetArcControlPoints(Vector3 startPos, Vector3 endPos, float stretchFactor = 1f, float angle = 0, int direction = 1)
        {
            // Calculate center and radius
            Vector3 center = (startPos + endPos) / 2f;
            float radius = Vector3.Distance(startPos, endPos) / 2;

            // Calculate the vector from start to end
            Vector3 startToEnd = endPos - startPos;

            // Calculate the perpendicular vector in the plane of the circle
            Vector3 perpendicular;
            if (Vector3.Angle(startToEnd, Vector3.up) > 1f)
            {
                perpendicular = ProjectConstants.LaneHandDirection * Vector3.Cross(startToEnd, Vector3.up).normalized;
            }
            else
            {
                perpendicular = ProjectConstants.LaneHandDirection * Vector3.Cross(startToEnd, Vector3.forward).normalized;
            }

            // Apply stretch
            float stretchedRadius = radius * stretchFactor * 4;

            // Calculate control points. These are offset from the center by the radius
            // along the perpendicular vector. The magic number 0.55191502449 is an approximation
            // of (4/3) * (sqrt(2) - 1), which is used to approximate a circular arc with a Bezier curve.
            Vector3 controlPoint1 = startPos - Quaternion.Euler(0, angle, 0) * perpendicular * direction * stretchedRadius * 0.55191502449f;
            Vector3 controlPoint2 = endPos - Quaternion.Euler(0, -angle, 0) * perpendicular * direction * stretchedRadius * 0.55191502449f;

            return (controlPoint1, controlPoint2);
        }

        /// <summary> Two corner points for Quad Bezier. </summary> 
        public static (Vector3, Vector3) GetSplineTwoCornerPoint(Vector3 sourcePoint, Vector3 targetPoint, Vector3 dir, Vector3 dir2, bool allowClosestPoint = false)
        {
            var cornerPoint = GetSplineCornerPoint(sourcePoint, targetPoint, dir, dir2, allowClosestPoint);
            var corner1 = (cornerPoint + sourcePoint) / 2;
            var corner2 = (cornerPoint + targetPoint) / 2;

            return (corner1, corner2);
        }

        /// <summary> Corner point for Cubic Bezier. </summary> 
        public static Vector3 GetSplineCornerPoint(Vector3 sourcePoint, Vector3 targetPoint, Vector3 dir, Vector3 dir2, bool allowClosestPoint = false)
        {
            Vector3 cornerPoint;

            var found = VectorExtensions.LineLineIntersection(sourcePoint, dir, targetPoint, dir2, out cornerPoint);

            if (cornerPoint != default)
            {
                var dir11 = Vector3.Normalize(sourcePoint - cornerPoint);
                var dir12 = Vector3.Normalize(cornerPoint - targetPoint);
                var dir22 = Vector3.Normalize(cornerPoint - targetPoint);
                var dir23 = Vector3.Normalize(targetPoint - cornerPoint);

                var dot = Vector3.Dot(dir11, dir12);
                var dot2 = Vector3.Dot(-dir11, dir23);

                if (dot < -0.8f || dot2 < -0.8f)
                {
                    cornerPoint = default;
                }
            }

            if (cornerPoint == default || (!found && !allowClosestPoint))
            {
                cornerPoint = (sourcePoint + targetPoint) / 2;

                var crossDir = Vector3.Cross(dir, -Vector3.up);

                VectorExtensions.LineLineIntersection(cornerPoint, crossDir, targetPoint, dir2, out cornerPoint);
            }

            return cornerPoint;
        }

        private static bool CastPoint(Vector3 sourcePoint, out RaycastHit hit, LayerMask layerMask, bool debug = false)
        {
            const float castOffset = 3f;
            const float castDistance = 6f;

            var origin = sourcePoint + new Vector3(0, castOffset);

            var found = Physics.Raycast(origin, Vector3.down, out hit, castDistance, layerMask, QueryTriggerInteraction.Collide);

            if (debug)
            {
                if (found)
                {
                    Debug.DrawLine(origin, hit.point, Color.magenta, 5f);
                }
                else
                {
                    Debug.DrawLine(origin, origin - Vector3.down * castDistance, Color.red, 5f);
                }
            }

            return found;
        }
    }
}
#endif