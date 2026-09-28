using Spirit604.Extensions;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Spirit604.Gameplay.Road
{
    public static class PathHelper
    {
        public static void GetShiftedPositionByLane(ref Vector3 sourcePosition, ref Vector3 targetPosition, float laneOffSet, int laneIndex)
        {
            if (laneIndex > 0)
            {
                Vector3 shiftedPos = Quaternion.Euler(0, 90, 0) * (targetPosition - sourcePosition).normalized * laneOffSet * laneIndex;

                sourcePosition = sourcePosition + shiftedPos;
                targetPosition = targetPosition + shiftedPos;
            }
        }

        public static void CreateStraightPath(Path path, List<Transform> nodes, bool createNewWaypoints, int index, bool recordUndo = false)
        {
            for (int i = 0; i < nodes.Count - 1; i++)
            {
                for (int j = 1; j < path.WayPointsCountPerCurve; j++)
                {
                    float t = (float)j / (path.WayPointsCountPerCurve - 1);
                    Vector3 wayPointPosition = Vector3.Lerp(nodes[i].transform.position, nodes[i + 1].transform.position, t);

                    path.AddWayPoint(wayPointPosition, createNewWaypoints, index, recordUndo: recordUndo);
                    index++;
                }
            }
        }

        public static void CreateCurvedPath(Path path, bool createNewWaypoints, int targetWaypointCount, bool recordUndo = false)
        {
            if (!createNewWaypoints && recordUndo)
            {
                var transforms = path.WaypointTransforms;

                if (transforms?.Length > 0)
                {
#if UNITY_EDITOR
                    Undo.RecordObjects(transforms, "transforms");
#endif
                }
            }

            int curveSegmentLength = path.PathCurveType == PathCurveType.BezierCube ? Path.CUBE_BEZIER_TYPE : Path.QUAD_BEZIER_TYPE;
            int segmentCount = Mathf.FloorToInt((float)(path.Nodes.Count - 1) / (curveSegmentLength - 1));
            int drawedCount = 0;

            if (path.Nodes.Count > curveSegmentLength - 1)
            {
                int wayPointCount = path.WayPointsCountPerCurve;

                int curveIndex = 0;

                while (curveIndex < segmentCount)
                {
                    int nextIndex1 = curveIndex * (curveSegmentLength - 1);
                    int nextIndex2 = nextIndex1 + 1;
                    int nextIndex3 = nextIndex1 + 2;
                    int nextIndex4 = nextIndex1 + 3;

                    for (int j = 0; j < wayPointCount; j++)
                    {
                        float t = (float)j / (wayPointCount - 1);

                        float y = Mathf.Lerp(path.Nodes[0].transform.position.y, path.Nodes[segmentCount * (curveSegmentLength - 1)].transform.position.y, t);

                        Vector3 offset = new Vector3(0, y).Flat();
                        Vector3 waypointPosition = Vector3.zero;

                        if (path.PathCurveType == PathCurveType.BezierCube)
                        {
                            waypointPosition = Bezier.CalculateCubeBezierPoint(t, path.Nodes[nextIndex1].position, path.Nodes[nextIndex2].position, path.Nodes[nextIndex3].position) + offset;
                        }
                        else
                        {
                            waypointPosition = Bezier.CalculateQuadBezierPoint(t, path.Nodes[nextIndex1].position, path.Nodes[nextIndex2].position, path.Nodes[nextIndex3].position, path.Nodes[nextIndex4].position) + offset;
                        }

                        int wayPointIndex = drawedCount;

                        path.AddWayPoint(waypointPosition, createNewWaypoints, wayPointIndex);
                        drawedCount++;
                    }

                    curveIndex++;
                }
            }

            int remainNodes = targetWaypointCount - drawedCount;

            if (remainNodes > 0)
            {
                int nodeIndex = 0;

                nodeIndex = curveSegmentLength * segmentCount;

                while (remainNodes > 0)
                {
                    int wayPointIndex = drawedCount;

                    path.AddWayPoint(path.Nodes[nodeIndex].transform.position, createNewWaypoints, wayPointIndex);

                    nodeIndex++;
                    drawedCount++;
                    remainNodes--;
                }
            }
        }

        public static Path CreateConnectedStraightPath(Path path, TrafficNode sourceTrafficNode, TrafficNode targetTrafficNode, int sourceLaneIndex = -1, int connectedLaneIndex = -1, float wayPointStepOffset = 0)
        {
            path.SourceTrafficNode = sourceTrafficNode;
            path.ConnectedTrafficNode = targetTrafficNode;
            path.ConnectedLaneIndex = connectedLaneIndex;

            var node1 = path.AddNode(false);
            var node2 = path.AddNode(false);

            if (sourceLaneIndex != -1)
            {
                sourceTrafficNode.AddPath(path, sourceLaneIndex, true, true);
                path.transform.localPosition = Vector3.zero;

                node1.transform.position = sourceTrafficNode.GetLanePosition(sourceLaneIndex, true);
            }

            if (connectedLaneIndex != -1)
            {
                node2.transform.position = targetTrafficNode.GetLanePosition(connectedLaneIndex);
            }

            if (wayPointStepOffset != 0)
            {
                float distance = Vector3.Distance(node1.transform.position, node2.transform.position);

                var newNodeCount = Mathf.FloorToInt(distance / wayPointStepOffset);
                var dir = (node2.transform.position - node1.transform.position).normalized;

                for (int i = 0; i < newNodeCount; i++)
                {
                    var startPos = node1.transform.position + dir * wayPointStepOffset * (i + 1);
                    bool addNode = true;

                    if (i == newNodeCount - 1)
                    {
                        var lastNodeDistance = Vector3.Distance(startPos, node2.transform.position);

                        if (lastNodeDistance < wayPointStepOffset / 2)
                        {
                            addNode = false;
                        }
                    }

                    if (addNode)
                    {
                        path.InsertNode(startPos, i + 1, recordUndo: false);
                    }
                }
            }

            path.WayPointsCountPerCurve = 2;
            path.PathCurveType = PathCurveType.StraightLine;
            path.PathRoadType = PathRoadType.StraightRoad;

            path.CreatePath(true);

            EditorSaver.SetObjectDirty(path);

            return path;
        }

        public static Vector3 GetPathIntersection(Vector3[] points1, Vector3[] points2, float maxProjectDistance = 0.2f)
        {
            return GetPathIntersection(points1, points2, out var index1, out var index2, maxProjectDistance);
        }

        public static Vector3 GetPathIntersection(Vector3[] points1, Vector3[] points2, out int index1, out int index2, float maxProjectDistance = 0.2f, bool flat = false)
        {
            index1 = -1;
            index2 = -1;

            for (int i = 0; i < points1.Length - 1; i++)
            {
                for (int j = 0; j < points2.Length - 1; j++)
                {
                    Vector3 A1 = points1[i];
                    Vector3 A2 = points1[i + 1];
                    Vector3 B1 = points2[j];
                    Vector3 B2 = points2[j + 1];

                    var intersectionPoint = VectorExtensions.GetIntersectionPointCoordinates3DSpaceWithCheckIntersection(A1, A2, B1, B2, flat);

                    if (intersectionPoint != Vector3.zero)
                    {
                        index1 = i;
                        index2 = j;

                        return intersectionPoint;
                    }
                    else
                    {
                        if ((points1[0] - points2[0]).sqrMagnitude < 0.1f)
                        {
                            return default;
                        }
                        if ((points1[0] - points2[points2.Length - 1]).sqrMagnitude < 0.1f)
                        {
                            return default;
                        }
                        if ((points1[points1.Length - 1] - points2[0]).sqrMagnitude < 0.1f)
                        {
                            return default;
                        }

                        if (i == 0)
                        {
                            var projectPoint1 = Math3d.ProjectPointOnLineSegment(B1, B2, A1);

                            var distance1 = Vector3.Distance(projectPoint1, A1);

                            if (distance1 < maxProjectDistance)
                            {
                                index1 = i;
                                index2 = j;
                                return A1;
                            }
                        }

                        var projectPoint2 = Math3d.ProjectPointOnLineSegment(B1, B2, A2);

                        var distance2 = Vector3.Distance(projectPoint2, A2);

                        if (distance2 < maxProjectDistance)
                        {
                            index1 = i;
                            index2 = j;
                            return A2;
                        }

                        var projectPoint3 = Math3d.ProjectPointOnLineSegment(A1, A2, B1);

                        var distance3 = Vector3.Distance(projectPoint3, B1);

                        if (distance3 < maxProjectDistance)
                        {
                            index1 = i;
                            index2 = j;
                            return B1;
                        }

                        var projectPoint4 = Math3d.ProjectPointOnLineSegment(A1, A2, B2);

                        var distance4 = Vector3.Distance(projectPoint4, B2);

                        if (distance4 < maxProjectDistance)
                        {
                            index1 = i;
                            index2 = j;
                            return B2;
                        }
                    }
                }
            }

            return Vector3.zero;
        }

        public static Vector3 GetPathIntersection(List<Vector3> points1, List<Vector3> points2, out int index1, out int index2, float maxProjectDistance = 0.2f)
        {
            index1 = -1;
            index2 = -1;

            for (int i = 0; i < points1.Count - 1; i++)
            {
                for (int j = 0; j < points2.Count - 1; j++)
                {
                    Vector3 A1 = points1[i];
                    Vector3 A2 = points1[i + 1];
                    Vector3 B1 = points2[j];
                    Vector3 B2 = points2[j + 1];

                    var intersectionPoint = VectorExtensions.GetIntersectionPointCoordinates3DSpaceWithCheckIntersection(A1, A2, B1, B2, false);

                    if (intersectionPoint != Vector3.zero)
                    {
                        index1 = i;
                        index2 = j;

                        return intersectionPoint;
                    }
                    else
                    {
                        if ((points1[0] - points2[0]).sqrMagnitude < 0.1f)
                        {
                            return default;
                        }
                        if ((points1[0] - points2[points2.Count - 1]).sqrMagnitude < 0.1f)
                        {
                            return default;
                        }
                        if ((points1[points1.Count - 1] - points2[0]).sqrMagnitude < 0.1f)
                        {
                            return default;
                        }

                        if (i == 0)
                        {
                            var projectPoint1 = Math3d.ProjectPointOnLineSegment(B1, B2, A1);

                            var distance1 = Vector3.Distance(projectPoint1, A1);

                            if (distance1 < maxProjectDistance)
                            {
                                index1 = i;
                                index2 = j;
                                return A1;
                            }
                        }

                        var projectPoint2 = Math3d.ProjectPointOnLineSegment(B1, B2, A2);

                        var distance2 = Vector3.Distance(projectPoint2, A2);

                        if (distance2 < maxProjectDistance)
                        {
                            index1 = i;
                            index2 = j;
                            return A2;
                        }

                        var projectPoint3 = Math3d.ProjectPointOnLineSegment(A1, A2, B1);

                        var distance3 = Vector3.Distance(projectPoint3, B1);

                        if (distance3 < maxProjectDistance)
                        {
                            index1 = i;
                            index2 = j;
                            return B1;
                        }

                        var projectPoint4 = Math3d.ProjectPointOnLineSegment(A1, A2, B2);

                        var distance4 = Vector3.Distance(projectPoint4, B2);

                        if (distance4 < maxProjectDistance)
                        {
                            index1 = i;
                            index2 = j;
                            return B2;
                        }
                    }
                }
            }

            return Vector3.zero;
        }

        public static Vector3 GetAttachPoint(Path targetPath, Vector3 sourcePosition)
        {
            return GetAttachPoint(targetPath, sourcePosition, out var index);
        }

        public static Vector3 GetAttachPoint(Path targetPath, Vector3 sourcePosition, out int index)
        {
            index = -1;
            var p = sourcePosition;

            Vector3 point = default;

            for (int i = 0; i < targetPath.WayPoints.Count - 1; i++)
            {
                var a = targetPath.WayPoints[i].transform.position;
                var b = targetPath.WayPoints[i + 1].transform.position;

                Vector3 ab = b - a;
                Vector3 ap = p - a;
                Vector3 ar = Vector3.Project(ap, ab);

                bool found = true;

                if (a == b)
                {
                    found = false;
                }
                else if (Vector3.Dot(ab, ar) < 0)
                {
                    found = false;
                }
                else if (ar.sqrMagnitude > ab.sqrMagnitude)
                {
                    found = false;
                }

                if (!found)
                    continue;

                var tempPoint = a + ar;

                if (point == Vector3.zero)
                {
                    index = i;
                    point = tempPoint;
                }
                else
                {
                    float dist1 = Vector3.Distance(point, sourcePosition);
                    float dist2 = Vector3.Distance(tempPoint, sourcePosition);

                    if (dist2 < dist1)
                    {
                        index = i;
                        point = tempPoint;
                    }
                }
            }

            if (point.Equals(Vector3.zero))
            {
                var a1 = targetPath.WayPoints[0].transform.position;
                var a2 = targetPath.WayPoints[targetPath.WayPoints.Count - 1].transform.position;

                float val1 = Vector3.SqrMagnitude(p - a1);
                float val2 = Vector3.SqrMagnitude(p - a2);

                if (val1 < val2)
                {
                    index = 0;
                    return a1;
                }
                else
                {
                    index = targetPath.WayPoints.Count - 2;
                    return a2;
                }
            }

            return point;
        }

        public static void DestroyPath(this Path path, bool recordUndo = true)
        {
            if (path.SourceTrafficNode)
            {
                if (recordUndo)
                {
#if UNITY_EDITOR
                    Undo.RegisterCompleteObjectUndo(path.SourceTrafficNode, "Undo destroy path");

                    if (path.CustomLightHandler && path.CustomLightHandler.TrafficLightCrossroad)
                    {
                        Undo.RegisterCompleteObjectUndo(path.CustomLightHandler.TrafficLightCrossroad, "Undo destroy path");
                    }
#endif
                }
            }

            if (recordUndo)
            {
#if UNITY_EDITOR
                Undo.DestroyObjectImmediate(path.gameObject);
                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
#endif
            }
            else
            {
                GameObject.DestroyImmediate(path.gameObject);
            }
        }
    }
}