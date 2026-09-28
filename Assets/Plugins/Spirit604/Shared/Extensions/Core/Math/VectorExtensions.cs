using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace Spirit604.Extensions
{
    public static class VectorExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 GetIntersectionPointCoordinates(Vector2 A1, Vector2 A2, Vector2 B1, Vector2 B2)//, out bool found)
        {
            //bool found;
            float tmp = (B2.x - B1.x) * (A2.y - A1.y) - (B2.y - B1.y) * (A2.x - A1.x);

            if (tmp == 0)
            {
                // No solution!
                //found = false;
                return Vector2.zero;
            }

            float mu = ((A1.x - B1.x) * (A2.y - A1.y) - (A1.y - B1.y) * (A2.x - A1.x)) / tmp;

            //found = true;

            return new Vector2(
                B1.x + (B2.x - B1.x) * mu,
                B1.y + (B2.y - B1.y) * mu
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetIntersectionPointCoordinates3DSpace(Vector3 A1, Vector3 A2, Vector3 B1, Vector3 B2, bool flat = true)//, out bool found)
        {
            //bool found;
            float tmp = (B2.x - B1.x) * (A2.z - A1.z) - (B2.z - B1.z) * (A2.x - A1.x);

            if (tmp == 0)
            {
                // No solution!
                //found = false;
                return Vector2.zero;
            }

            float mu = ((A1.x - B1.x) * (A2.z - A1.z) - (A1.z - B1.z) * (A2.x - A1.x)) / tmp;

            var y = 0f;

            if (!flat)
            {
                y = B1.y + (B2.y - B1.y) * mu;
            }

            var intersectPoint = new Vector3(
                B1.x + (B2.x - B1.x) * mu,
                y,
                B1.z + (B2.z - B1.z) * mu
            );

            //found = true;

            return intersectPoint;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetIntersectionPointCoordinates3DSpaceWithCheckIntersection(Vector3 A1, Vector3 A2, Vector3 B1, Vector3 B2, bool flat = true)//, out bool found)
        {
            Vector3 intersectPoint = GetIntersectionPointCoordinates3DSpace(A1, A2, B1, B2, flat);

            if (intersectPoint != Vector3.zero && IsBetween3DSpace(A1, A2, intersectPoint) && IsBetween3DSpace(B1, B2, intersectPoint))
            {
                return intersectPoint;
            }
            else
            {
                return Vector3.zero;
            }
        }

        //linePoint - point the line passes through
        //lineDirection - unit vector in direction of line, either direction works
        //sourcePoint - the point to find nearest on line for
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 NearestPointOnLine(Vector3 linePoint, Vector3 lineDirection, Vector3 sourcePoint)
        {
            lineDirection.Normalize();//this needs to be a unit vector
            var v = sourcePoint - linePoint;
            var d = Vector3.Dot(v, lineDirection);
            return linePoint + lineDirection * d;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 NearestPointOnLine(Line line, Vector3 sourcePoint)
        {
            return NearestPointOnLine(line.A1, line.A2 - line.A1, sourcePoint);
        }

        //Is a point c between 2 other points a and b?
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsBetween(Vector2 a, Vector2 b, Vector2 c)
        {
            bool isBetween = false;

            //Entire line segment
            Vector2 ab = b - a;
            //The intersection and the first point
            Vector2 ac = c - a;

            //Need to check 2 things: 
            //1. If the vectors are pointing in the same direction = if the dot product is positive
            //2. If the length of the vector between the intersection and the first point is smaller than the entire line
            if (Vector2.Dot(ab, ac) > 0f && ab.sqrMagnitude >= ac.sqrMagnitude)
            {
                isBetween = true;
            }

            return isBetween;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsBetween3DSpace(Vector3 a, Vector3 b, Vector3 c)
        {
            bool isBetween = false;

            a.y = b.y = c.y = 0;

            //Entire line segment
            Vector3 ab = b - a;
            //The intersection and the first point
            Vector3 ac = c - a;

            //Need to check 2 things: 
            //1. If the vectors are pointing in the same direction = if the dot product is positive
            //2. If the length of the vector between the intersection and the first point is smaller than the entire line
            if (Vector3.Dot(ab, ac) > 0f && ab.sqrMagnitude >= ac.sqrMagnitude)
            {
                isBetween = true;
            }

            return isBetween;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsBetween2(Vector2 a, Vector2 b, Vector2 c)
        {
            bool isBetween = false;

            //Entire line segment
            Vector2 bc = b - c;
            //The intersection and the first point
            Vector2 ac = c - a;

            //Need to check 2 things: 
            //1. If the vectors are pointing in the same direction = if the dot product is positive
            //2. If the length of the vector between the intersection and the first point is smaller than the entire line
            if (Vector2.Dot(bc, ac) > 0.98F)
            {
                isBetween = true;
            }

            return isBetween;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Flat(this Vector3 vector)
        {
            return new Vector3(vector.x, 0, vector.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float isLeft(Vector2 P0, Vector2 P1, Vector2 P2)
        {
            return ((P1.x - P0.x) * (P2.y - P0.y) - (P2.x - P0.x) * (P1.y - P0.y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PointInRectangle2D(Vector2 X, Vector2 Y, Vector2 Z, Vector2 W, Vector2 P)
        {
            return (isLeft(X, Y, P) > 0 && isLeft(Y, Z, P) > 0 && isLeft(Z, W, P) > 0 && isLeft(W, X, P) > 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PointInRectangle3D(Vector3 X, Vector3 Y, Vector3 Z, Vector3 W, Vector3 P)
        {
            X = X.ToVector2_2DSpace();
            Y = Y.ToVector2_2DSpace();
            Z = Z.ToVector2_2DSpace();
            W = W.ToVector2_2DSpace();
            P = P.ToVector2_2DSpace();

            return PointInRectangle2D(X, Y, Z, W, P);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PointInRectangle3D(Square square, Vector3 P)
        {
            Vector3 X = square.Line1.A1;
            Vector3 Y = square.Line2.A1;
            Vector3 Z = square.Line2.A2;
            Vector3 W = square.Line1.A2;

            return PointInRectangle3D(X, Y, Z, W, P);
        }

        public struct Line
        {
            public Vector3 A1 { get; set; }
            public Vector3 A2 { get; set; }

            public Line(Vector3 a1, Vector3 a2)
            {
                a1.y = 0;
                a2.y = 0;
                A1 = a1;
                A2 = a2;
            }
        }

        public struct Square
        {
            public Line Line1 { get; set; }
            public Line Line2 { get; set; }

            public Square(Line line1, Line line2)
            {
                Line1 = line1;
                Line2 = line2;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DebugDrawSquare(VectorExtensions.Square square, Color color, float duration = 0)
        {
            if (duration == 0)
            {
                Debug.DrawLine(square.Line1.A1, square.Line1.A2, color);
                Debug.DrawLine(square.Line2.A1, square.Line2.A2, color);
                Debug.DrawLine(square.Line1.A1, square.Line2.A1, color);
                Debug.DrawLine(square.Line1.A2, square.Line2.A2, color);
            }
            else
            {
                Debug.DrawLine(square.Line1.A1, square.Line1.A2, color, duration);
                Debug.DrawLine(square.Line2.A1, square.Line2.A2, color, duration);
                Debug.DrawLine(square.Line1.A1, square.Line2.A1, color, duration);
                Debug.DrawLine(square.Line1.A2, square.Line2.A2, color, duration);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 LineWithSquareIntersect(Line line, Square square, bool isFullCheck = false)
        {
            Vector3 intersectPoint = VectorExtensions.GetIntersectionPointCoordinates3DSpaceWithCheckIntersection(line.A1, line.A2, square.Line1.A1, square.Line1.A2);

            if (intersectPoint != Vector3.zero)
            {
                return intersectPoint;
            }

            intersectPoint = VectorExtensions.GetIntersectionPointCoordinates3DSpaceWithCheckIntersection(line.A1, line.A2, square.Line2.A1, square.Line2.A2);

            if (intersectPoint != Vector3.zero)
            {
                return intersectPoint;
            }

            if (isFullCheck)
            {
                intersectPoint = VectorExtensions.GetIntersectionPointCoordinates3DSpaceWithCheckIntersection(line.A1, line.A2, square.Line1.A1, square.Line2.A1);

                if (intersectPoint != Vector3.zero)
                {
                    return intersectPoint;
                }

                intersectPoint = VectorExtensions.GetIntersectionPointCoordinates3DSpaceWithCheckIntersection(line.A1, line.A2, square.Line1.A2, square.Line2.A2);

                if (intersectPoint != Vector3.zero)
                {
                    return intersectPoint;
                }
            }

            return Vector3.zero;
        }

        /// <summary> Method to calculate the intersection point or the closest point of approach of two lines. </summary>   
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LineLineIntersection(Vector3 origin1, Vector3 dir1, Vector3 origin2, Vector3 dir2, out Vector3 intersection)
        {
            Vector3 crossDir = Vector3.Cross(dir1, dir2);
            float denominator = crossDir.sqrMagnitude;

            // Lines are not parallel
            if (denominator > 0.0001f)
            {
                Vector3 diff = origin2 - origin1;
                float t1 = Vector3.Dot(Vector3.Cross(diff, dir2), crossDir) / denominator;
                intersection = origin1 + dir1 * t1;

                // Check if the intersection point lies on the second line
                float t2 = Vector3.Dot(Vector3.Cross(diff, dir1), crossDir) / denominator;
                Vector3 pointOnSecondLine = origin2 + dir2 * t2;

                // If the points are the same, then there is an intersection
                if ((pointOnSecondLine - intersection).sqrMagnitude < 0.000001f)
                {
                    return true;
                }
                else
                {
                    // Return the closest point of approach
                    intersection = (intersection + pointOnSecondLine) * 0.5f;
                    return false;
                }
            }
            else
            {
                // Lines are parallel, check if they are collinear
                //Vector3 diff = origin2 - origin1;
                //if (Vector3.Dot(Vector3.Cross(diff, dir1), crossDir) == 0.0f)
                //{
                //    // Lines are collinear
                //    intersection = origin1; // Can choose any point on the line
                //    return true;
                //}
                //else
                {
                    // Lines are parallel but not collinear, no intersection
                    intersection = Vector3.zero;
                    return false;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 LineWithSquareClosestPointIntersect(Line line, Square square)
        {
            Vector3 intersectPoint = VectorExtensions.GetIntersectionPointCoordinates3DSpaceWithCheckIntersection(line.A1, line.A2, square.Line1.A1, square.Line1.A2);

            if (intersectPoint != Vector3.zero)
            {
                return NearestPointOnLine(square.Line1, line.A1);
            }

            intersectPoint = VectorExtensions.GetIntersectionPointCoordinates3DSpaceWithCheckIntersection(line.A1, line.A2, square.Line2.A1, square.Line2.A2);

            if (intersectPoint != Vector3.zero)
            {
                return NearestPointOnLine(square.Line2, line.A1);
            }

            intersectPoint = VectorExtensions.GetIntersectionPointCoordinates3DSpaceWithCheckIntersection(line.A1, line.A2, square.Line1.A1, square.Line2.A1);

            if (intersectPoint != Vector3.zero)
            {
                return NearestPointOnLine(square.Line1.A1, square.Line2.A1 - square.Line1.A1, line.A1);
            }

            intersectPoint = VectorExtensions.GetIntersectionPointCoordinates3DSpaceWithCheckIntersection(line.A1, line.A2, square.Line1.A2, square.Line2.A2);

            if (intersectPoint != Vector3.zero)
            {
                return NearestPointOnLine(square.Line1.A2, square.Line2.A2 - square.Line1.A2, line.A1);
            }

            return Vector3.zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector3 SquareWithSquareIntersect(Square square1, Square square2)
        {
            Vector3 intersectPoint = VectorExtensions.GetIntersectionPointCoordinates3DSpaceWithCheckIntersection(square1.Line1.A1, square1.Line1.A2, square2.Line1.A1, square2.Line1.A2);

            if (intersectPoint != Vector3.zero)
            {
                return intersectPoint;
            }

            intersectPoint = VectorExtensions.GetIntersectionPointCoordinates3DSpaceWithCheckIntersection(square1.Line1.A1, square1.Line1.A2, square2.Line2.A1, square2.Line2.A2);

            if (intersectPoint != Vector3.zero)
            {
                return intersectPoint;
            }

            intersectPoint = VectorExtensions.GetIntersectionPointCoordinates3DSpaceWithCheckIntersection(square1.Line2.A1, square1.Line2.A2, square2.Line1.A1, square2.Line1.A2);

            if (intersectPoint != Vector3.zero)
            {
                return intersectPoint;
            }

            intersectPoint = VectorExtensions.GetIntersectionPointCoordinates3DSpaceWithCheckIntersection(square1.Line2.A1, square1.Line2.A2, square2.Line2.A1, square2.Line2.A2);

            if (intersectPoint != Vector3.zero)
            {
                return intersectPoint;
            }
            return Vector3.zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 SquareWithSquareIntersect(Square square1, Square square2, bool isFullCheck)
        {
            Vector3 intersectPoint = SquareWithSquareIntersect(square1, square2);

            if (intersectPoint != Vector3.zero)
            {
                return intersectPoint;
            }

            if (isFullCheck)
            {
                intersectPoint = VectorExtensions.GetIntersectionPointCoordinates3DSpaceWithCheckIntersection(square1.Line1.A1, square1.Line2.A1, square2.Line1.A1, square2.Line1.A2);

                if (intersectPoint != Vector3.zero)
                {
                    return intersectPoint;
                }

                intersectPoint = VectorExtensions.GetIntersectionPointCoordinates3DSpaceWithCheckIntersection(square1.Line1.A1, square1.Line2.A1, square2.Line2.A1, square2.Line2.A2);

                if (intersectPoint != Vector3.zero)
                {
                    return intersectPoint;
                }

                intersectPoint = VectorExtensions.GetIntersectionPointCoordinates3DSpaceWithCheckIntersection(square1.Line1.A2, square1.Line2.A2, square2.Line1.A1, square2.Line1.A2);

                if (intersectPoint != Vector3.zero)
                {
                    return intersectPoint;
                }

                intersectPoint = VectorExtensions.GetIntersectionPointCoordinates3DSpaceWithCheckIntersection(square1.Line1.A2, square1.Line2.A2, square2.Line2.A1, square2.Line2.A2);

                if (intersectPoint != Vector3.zero)
                {
                    return intersectPoint;
                }
            }

            return Vector3.zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToVector2(this Vector3 vector)
        {
            return new Vector2(vector.x, vector.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToVector2_2DSpace(this Vector3 vector)
        {
            return new Vector2(vector.x, vector.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToVector3_3DSpace(this Vector2 vector)
        {
            return new Vector3(vector.x, 0, vector.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEqual(this Vector3 lhs, Vector3 rhs)
        {
            return IsEqual(lhs, rhs, Mathf.Epsilon);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEqual(this Vector3 lhs, Vector3 rhs, float precision)
        {
            return Vector3.SqrMagnitude(lhs - rhs) < precision;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEqual(this Vector2 lhs, Vector2 rhs)
        {
            return IsEqual(lhs, rhs, Mathf.Epsilon);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEqual(this Vector2 lhs, Vector2 rhs, float precision)
        {
            return Vector3.SqrMagnitude(lhs - rhs) < precision;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BetweenLineAndCircle(
        Vector3 circleCenter, float circleRadius,
        Vector3 point1, Vector3 point2,
        out Vector2 intersection1, out Vector2 intersection2)
        {
            Vector2 circleCenter2 = circleCenter.ToVector2_2DSpace();
            Vector2 point12 = point1.ToVector2_2DSpace();
            Vector2 point22 = point2.ToVector2_2DSpace();

            intersection1 = Vector3.zero;
            intersection2 = Vector3.zero;

            return BetweenLineAndCircle(
        circleCenter2, circleRadius,
        point12, point22,
        out intersection1, out intersection2);
        }

        public static int BetweenLineAndCircle(
        Vector2 circleCenter, float circleRadius,
        Vector2 point1, Vector2 point2,
        out Vector2 intersection1, out Vector2 intersection2)
        {
            float t;

            var dx = point2.x - point1.x;
            var dy = point2.y - point1.y;

            var a = dx * dx + dy * dy;
            var b = 2 * (dx * (point1.x - circleCenter.x) + dy * (point1.y - circleCenter.y));
            var c = (point1.x - circleCenter.x) * (point1.x - circleCenter.x) + (point1.y - circleCenter.y) * (point1.y - circleCenter.y) - circleRadius * circleRadius;

            var determinate = b * b - 4 * a * c;
            if ((a <= 0.0000001) || (determinate < -0.0000001))
            {
                // No real solutions.
                intersection1 = Vector2.zero;
                intersection2 = Vector2.zero;
                return 0;
            }
            if (determinate < 0.0000001 && determinate > -0.0000001)
            {
                // One solution.
                t = -b / (2 * a);
                intersection1 = new Vector2(point1.x + t * dx, point1.y + t * dy);
                intersection2 = Vector2.zero;
                return 1;
            }

            // Two solutions.
            t = (float)((-b + Mathf.Sqrt(determinate)) / (2 * a));
            intersection1 = new Vector2(point1.x + t * dx, point1.y + t * dy);
            t = (float)((-b - Mathf.Sqrt(determinate)) / (2 * a));
            intersection2 = new Vector2(point1.x + t * dx, point1.y + t * dy);

            return 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FindClosestTarget<T>(IEnumerable<T> targets, Vector3 position) where T : MonoBehaviour
        {
            return targets.OrderBy(item => (item.transform.position - position).sqrMagnitude).FirstOrDefault();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> SortTargetsByDistance<T>(IEnumerable<T> targets, Vector3 position) where T : MonoBehaviour
        {
            return targets.OrderBy(item => (item.transform.position - position).sqrMagnitude);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Transform FindClosestTarget(IEnumerable<Transform> targets, Vector3 position)
        {
            return targets.OrderBy(item => (item.position - position).sqrMagnitude).FirstOrDefault();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 FindClosestTarget(IEnumerable<Vector3> targets, Vector3 position)
        {
            return targets.OrderBy(targetPosition => (targetPosition - position).sqrMagnitude).FirstOrDefault();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 FindClosestTarget(Vector3 sourcePosition, Vector3 position1, Vector3 position2)
        {
            var distance1 = Vector3.SqrMagnitude(sourcePosition - position1);
            var distance2 = Vector3.SqrMagnitude(sourcePosition - position2);

            return distance1 < distance2 ? position1 : position2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetClosestTransform<T>(Vector3 sourcePos, T transform1, T transform2) where T : MonoBehaviour
        {
            var dist1 = Vector3.SqrMagnitude(sourcePos - transform1.transform.position);
            var dist2 = Vector3.SqrMagnitude(sourcePos - transform2.transform.position);

            return dist1 < dist2 ? transform1 : transform2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GUIScreenToWorldSpace(this Vector2 mousePosition, bool raycast = false, int layerMask = ~0)
        {
            Vector3 worldPosition = Vector3.zero;

#if UNITY_EDITOR
            var ray = HandleUtility.GUIPointToWorldRay(mousePosition);

            if (!raycast)
            {
                Plane hPlane = new Plane(Vector3.up, Vector3.zero);
                // Plane.Raycast stores the distance from ray.origin to the hit point in this variable:
                float distance = 0;
                // if the ray hits the plane...

                if (hPlane.Raycast(ray, out distance))
                {
                    // get the hit point:
                    worldPosition = ray.GetPoint(distance).Flat();
                }
            }
            else
            {
                if (Physics.Raycast(ray, out var hit, Mathf.Infinity, layerMask, QueryTriggerInteraction.Ignore))
                {
                    worldPosition = hit.point;
                }
                else
                {
                    return GetCenterOfSceneView(false);
                }
            }
#endif

            return worldPosition;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RoundAngle(this Transform rotation, float roundAngle)
        {
            rotation.rotation = RoundAngle(rotation.rotation, roundAngle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion RoundAngle(Quaternion rotation, float roundAngle)
        {
            var vec = rotation.eulerAngles;
            vec.x = Mathf.Round(vec.x / roundAngle) * roundAngle;
            vec.y = Mathf.Round(vec.y / roundAngle) * roundAngle;
            vec.z = Mathf.Round(vec.z / roundAngle) * roundAngle;
            rotation.eulerAngles = vec;

            return rotation;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetParallelPointAtSourceRelativeTargetLine(Vector3 sourcePoint, Vector3 targetPoint, float sideOffset)
        {
            return sourcePoint + Vector3.Cross(Vector3.up, (targetPoint - sourcePoint).normalized) * sideOffset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetParallelPointAtSourceRelativeForward(Vector3 sourcePoint, Vector3 forward, float sideOffset)
        {
            return sourcePoint + Vector3.Cross(Vector3.up, forward) * sideOffset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetCenterOfSceneView(bool raycast = false, int layerMask = ~0)
        {
            Ray ray = default;

#if UNITY_EDITOR
            ray = SceneView.lastActiveSceneView.camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 1.0f));
#endif

            Vector3 worldPosition = Vector3.zero;

            if (!raycast)
            {
                Plane hPlane = new Plane(Vector3.up, Vector3.zero);
                // Plane.Raycast stores the distance from ray.origin to the hit point in this variable:
                float distance = 0;
                // if the ray hits the plane...

                if (hPlane.Raycast(ray, out distance))
                {
                    // get the hit point:
                    worldPosition = ray.GetPoint(distance).Flat();
                }
            }
            else
            {
                if (Physics.Raycast(ray, out var hit, Mathf.Infinity, layerMask, QueryTriggerInteraction.Ignore))
                {
                    worldPosition = hit.point;
                }
                else
                {
                    return GetCenterOfSceneView(false);
                }
            }

            return worldPosition;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AngleOffAroundAxis(Vector3 v, Vector3 forward, Vector3 axis)
        {
            Vector3 right = Vector3.Cross(axis, forward).normalized;
            forward = Vector3.Cross(right, axis).normalized;
            return Mathf.Atan2(Vector3.Dot(v, right), Vector3.Dot(v, forward)) * Mathf.Rad2Deg;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float TestAngle(Vector3 targetDirection, Vector3 forward)
        {
            var angleOnY = Mathf.Asin(Vector3.Cross(targetDirection, forward).z) * Mathf.Rad2Deg;

            return angleOnY;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 SetY(this Vector3 vector, float yValue)
        {
            return new Vector3(vector.x, yValue, vector.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetY(this Transform transform, float yValue)
        {
            transform.position = new Vector3(transform.position.x, yValue, transform.position.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 FindNearestPointOnLine(Vector3 origin, Vector3 direction, Vector3 point)
        {
            direction.Normalize();
            Vector3 lhs = point - origin;

            float dotP = Vector3.Dot(lhs, direction);
            return origin + direction * dotP;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 FindNearestPointOnLine(Vector3[] points, Vector3 targetPoint)
        {
            if (points == null || points.Length <= 1)
            {
                return default;
            }

            float distanceSq = float.MaxValue;
            Vector3 currentPoint = default;

            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector3 point = points[i];
                Vector3 nextPoint = points[i + 1];

                var dir = (nextPoint - point).normalized;

                var pointOnLine = FindNearestPointOnLine(point, dir, targetPoint);

                float distance = Vector3.SqrMagnitude(targetPoint - pointOnLine);

                if (distance < distanceSq)
                {
                    distanceSq = distance;
                    currentPoint = pointOnLine;
                }
            }

            return currentPoint;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Divide(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
        }
    }
}