using Spirit604.CityEditor;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road.Debug;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.Gameplay.Road
{
    public partial class Path : MonoBehaviour
    {
        private const float WaypointRadius = 1f;
        private const float IntersectPointRadius = 0.5f;

        public const float HighlightedLineThick = 1f;

#if UNITY_EDITOR
        private Vector3[] points;
        private List<PathNode> spawnPathNodes = new List<PathNode>();
        private bool initNodes;

        private void OnDrawGizmos()
        {
            if (ShowIntersectedPoints)
            {
                if (intersects?.Count > 0)
                {
                    for (int i = 0; i < intersects.Count; i++)
                    {
                        if (intersects[i].IntersectedPath != null)
                        {
                            var point = intersects[i].GetIntersectPoint(this);
                            GizmosDrawer.DrawSphere(point, IntersectPointRadius, Color.gray);
                        }
                    }
                }
            }

            if (!PathDebugger.ShouldDrawEditorPath && !Selected && !Highlighted)
                return;

            var color = HighlightColor;

            if (wayPoints == null || wayPoints.Count == 0 || wayPoints[0] == null)
            {
                color = Color.red;
            }

            if (customLightHandler)
            {
                color = Color.green;
            }

            if (nodes.Count <= 1 || !(wayPoints?.Count > 0) || wayPoints[0] == null)
                return;

            int count = wayPoints.Count * 2;

            if (points == null || points.Length != count || Dirty)
            {
                Dirty = false;

                if (points == null || points.Length != count)
                    points = new Vector3[count];

                spawnPathNodes.Clear();
                spawnPathNodes.Capacity = wayPoints.Count - 2;

                for (int i = 0; i < wayPoints.Count - 1; i++)
                {
                    var arrayIndex = i * 2;
                    Vector3 A1point = wayPoints[i].transform.position;
                    Vector3 A2point = wayPoints[i + 1].transform.position;

                    points[arrayIndex] = wayPoints[i].transform.position;
                    points[arrayIndex + 1] = wayPoints[i + 1].transform.position;

                    if (wayPoints[i].SpawnNode)
                    {
                        spawnPathNodes.Add(wayPoints[i]);
                    }
                }
            }

            if (initNodes)
            {
                initNodes = false;
                spawnPathNodes.Clear();
                spawnPathNodes.Capacity = wayPoints.Count - 2;

                for (int i = 1; i < wayPoints.Count - 1; i++)
                {
                    if (wayPoints[i].SpawnNode)
                    {
                        spawnPathNodes.Add(wayPoints[i]);
                    }
                }
            }

            if (!Highlighted && !HasPathCustomColor)
            {
                GizmosDrawer.DrawLineList(points, color);

                for (var i = 0; i < spawnPathNodes.Count; i++)
                {
                    if (spawnPathNodes[i] == null || !spawnPathNodes[i].SpawnNode)
                    {
                        initNodes = true;
                        continue;
                    }

                    TrafficNode.DrawArrow(spawnPathNodes[i].transform.position, spawnPathNodes[i].transform.forward, CityEditorSettings.GetOrCreateSettings().SubNodeTrafficColor);
                }

                return;
            }

            float pathLength = 0;
            float currentDistance = 0;
            bool disabledHightlight = false;
            bool forceDrawHighlight = false;

            HightlightNormalizedLength = Mathf.Clamp(HightlightNormalizedLength, -1, 1);

            if (HightlightNormalizedLength != 1f)
            {
                pathLength = GetPathLength();
            }

            float normalizedLength = Math.Abs(HightlightNormalizedLength);

            for (int i = 0; i < wayPoints.Count - 1; i++)
            {
                Vector3 A1point = wayPoints[i].transform.position;
                Vector3 A2point = wayPoints[i + 1].transform.position;
                Vector3 offsetPoint = A2point;

                float overDistance = 0;

                if (wayPoints[i].SpawnNode)
                {
                    TrafficNode.DrawArrow(A1point, wayPoints[i].transform.forward, CityEditorSettings.GetOrCreateSettings().SubNodeTrafficColor);
                }

                if (HightlightNormalizedLength != 1f && !disabledHightlight)
                {
                    currentDistance += Vector3.Distance(A1point, A2point);

                    if (currentDistance >= normalizedLength * pathLength)
                    {
                        if (HightlightNormalizedLength >= 0)
                        {
                            overDistance = currentDistance - (1 - normalizedLength) * pathLength;
                        }
                        else
                        {
                            overDistance = currentDistance - normalizedLength * pathLength;
                        }

                        disabledHightlight = true;

                        offsetPoint = A1point + (A2point - A1point).normalized * overDistance;

                        if (HightlightNormalizedLength < 0)
                        {
                            forceDrawHighlight = true;
                        }
                    }
                }

                if (!Highlighted)
                {
                    color = HasConnection ? HighlightColor : Color.red;

                    if (wayPoints[i].HasPathCustomColor)
                    {
                        color = wayPoints[i].PathCustomColor;
                    }

                    GizmosDrawer.DrawLine(A1point, A2point, color);
                }
                else
                {
                    color = HighlightColor;

                    if (wayPoints[i].HasPathCustomColor)
                    {
                        color = wayPoints[i].PathCustomColor;
                    }

                    if (offsetPoint != A2point)
                    {
                        if (HightlightNormalizedLength >= 0)
                        {
                            DebugLine.DrawThickLine(A1point, offsetPoint, HighlightedLineThick, HighlightColor, true);
                            GizmosDrawer.DrawLine(offsetPoint, A2point, color);
                        }
                        else
                        {
                            DebugLine.DrawThickLine(offsetPoint, A2point, HighlightedLineThick, HighlightColor, true);
                            GizmosDrawer.DrawLine(A1point, offsetPoint, color);
                        }
                    }
                    else
                    {
                        if (HightlightNormalizedLength >= 0 || forceDrawHighlight)
                        {
                            DebugLine.DrawThickLine(A1point, A2point, HighlightedLineThick, color, true);
                        }
                        else
                        {
                            GizmosDrawer.DrawLine(A1point, A2point, color);
                        }
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (showInfoWaypoints)
            {
                for (int i = 0; i < wayPoints?.Count; i++)
                {
                    var color = Color.white;

                    if (wayPoints[i].HasSelectCustomColor)
                    {
                        color = wayPoints[i].SelectCustomColor;
                    }

                    GizmosDrawer.DrawWireSphere(wayPoints[i].transform.position, WaypointRadius, color);
                }
            }
        }
#endif
    }
}