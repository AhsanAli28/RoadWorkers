using Spirit604.Gameplay.Road;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public static class RoadUtils
    {
        public static int StripPath(Path tempPath, float maxStripAngle, float minStripDistance, bool recordUndo = false)
        {
            var points = tempPath.Nodes;

            int index = 1;

            Vector3 lastPoint = points[0].transform.position;

            float currentDistance = 0;

            int removedCount = 0;

            while (index < points.Count - 1)
            {
                var prevPoint = points[index - 1].transform.position;
                var currentPoint = points[index].transform.position;
                var nextPoint = points[index + 1].transform.position;

                Vector3 dirPrevious = (currentPoint - prevPoint).normalized;
                Vector3 dirNext = (nextPoint - currentPoint).normalized;

                var angle = Vector3.Angle(dirPrevious, dirNext);

                bool removed = false;

                if (angle < maxStripAngle)
                {
                    var remove = true;

                    if (minStripDistance > 0)
                    {
                        currentDistance += Vector3.Distance(currentPoint, lastPoint);
                        remove = currentDistance <= minStripDistance;
                    }

                    if (remove)
                    {
                        tempPath.RemoveNodeAt(index, recordUndo, false);
                        removed = true;
                        removedCount++;
                    }
                    else
                    {
                        lastPoint = currentPoint;
                        currentDistance = 0;
                    }
                }
                else
                {
                    lastPoint = currentPoint;
                    currentDistance = 0;
                }

                if (!removed)
                {
                    index++;
                }
            }

            return removedCount;
        }

        public static int StripPoints(List<Vector3> points, float maxStripAngle, float minStripDistance, float minDistance = 0.25f)
        {
            int index = 1;

            Vector3 lastPoint = points[0];

            float currentDistance = 0;

            int removedCount = 0;

            while (index < points.Count - 1)
            {
                var prevPoint = points[index - 1];
                var currentPoint = points[index];
                var nextPoint = points[index + 1];

                Vector3 dirPrevious = (currentPoint - prevPoint).normalized;
                Vector3 dirNext = (nextPoint - currentPoint).normalized;

                var angle = Vector3.Angle(dirPrevious, dirNext);

                bool removed = false;

                if (angle < maxStripAngle)
                {
                    var remove = true;

                    if (minStripDistance > 0)
                    {
                        currentDistance += Vector3.Distance(currentPoint, lastPoint);
                        remove = currentDistance <= minStripDistance;
                    }

                    if (!remove && minDistance > 0)
                    {
                        var distance = Vector3.Distance(prevPoint, currentPoint);

                        if (distance < minDistance)
                        {
                            remove = true;
                        }
                    }

                    if (remove)
                    {
                        points.RemoveAt(index);
                        removed = true;
                        removedCount++;
                    }
                    else
                    {
                        lastPoint = currentPoint;
                        currentDistance = 0;
                    }
                }
                else
                {
                    lastPoint = currentPoint;
                    currentDistance = 0;
                }

                if (!removed)
                {
                    index++;
                }
            }

            return removedCount;
        }

        public static float GetLineLength(List<Vector3> points)
        {
            if (points.Count < 2)
                return 0;

            float length = 0;

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 A1point = points[i];
                Vector3 A2point = points[i + 1];

                length += Vector3.Distance(A1point, A2point);
            }

            return length;
        }
    }
}
