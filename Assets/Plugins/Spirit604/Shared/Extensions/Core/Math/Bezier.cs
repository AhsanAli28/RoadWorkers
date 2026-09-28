using UnityEngine;

namespace Spirit604.Extensions
{
    public static class Bezier
    {
        public const int SEGMENT_COUNT = 10;

        public static Vector3 CalculateCubeBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;

            Vector3 p = uu * p0;
            p += 2 * u * t * p1;
            p += tt * p2;

            return p;
        }

        public static Vector3 CalculateQuadBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Vector3 p = uuu * p0; //first term

            p += 3 * uu * t * p1; //second term
            p += 3 * u * tt * p2; //third term
            p += ttt * p3; //fourth term

            return p;
        }

        public static Vector3[] GetCurvePoints(Vector3 p0, Vector3 p1, Vector3 p2, int segmentCount = SEGMENT_COUNT)
        {
            Vector3[] wayPoints = new Vector3[segmentCount];

            for (int i = 0; i < segmentCount; i++)
            {
                float t = (float)i / (float)segmentCount;
                wayPoints[i] = CalculateCubeBezierPoint(t, p0, p1, p2);
            }

            return wayPoints;
        }


        public static Vector3 GetCurvePoint(Vector3 p0, Vector3 p1, Vector3 p2, int index, int segmentCount = SEGMENT_COUNT)
        {
            float t = (float)index / (float)segmentCount;
            var wayPoint = CalculateCubeBezierPoint(t, p0, p1, p2);

            return wayPoint;
        }

        public static Vector3 GetCurvePoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int index, int segmentCount = SEGMENT_COUNT)
        {
            float t = (float)index / (float)segmentCount;
            var wayPoint = CalculateQuadBezierPoint(t, p0, p1, p2, p3);

            return wayPoint;
        }
    }
}