#if UNITY_EDITOR
using Spirit604.Gameplay.Road;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public static class TrafficNodeEditorExtension
    {
        public static void ShowPathHandles(TrafficNode trafficNode, GUIStyle gUIStyle, TrafficNodeDirectionType nodeDirectionType, bool canEdit = true, Path selectedPath = null, bool lockYAxis = true, bool showYPosition = false, bool roundYPosition = true, float roundValue = 0.05f)
        {
            if (trafficNode.Lanes == null || trafficNode.Lanes.Count == 0)
            {
                return;
            }

            if (nodeDirectionType.HasFlag(TrafficNodeDirectionType.Right))
            {
                trafficNode.IterateAllPaths((path) =>
                {
                    if (selectedPath == null || selectedPath == path)
                    {
                        PathEditorExtension.DrawPathHandles(path, gUIStyle, canEdit, lockYAxis: lockYAxis, showYPosition: showYPosition, roundYPosition: roundYPosition, roundValue: roundValue);
                    }
                });
            }

            if (nodeDirectionType.HasFlag(TrafficNodeDirectionType.Left))
            {
                trafficNode.IterateExternalPaths((path) =>
                {
                    if (selectedPath == null || selectedPath == path)
                    {
                        PathEditorExtension.DrawPathHandles(path, gUIStyle, canEdit, lockYAxis: lockYAxis, showYPosition: showYPosition, roundYPosition: roundYPosition, roundValue: roundValue);
                    }
                });
            }
        }

        public static void SwitchSelectionState(TrafficNode trafficNode, Path selectedPath, bool state, TrafficNodeDirectionType nodeDirectionType)
        {
            trafficNode.IterateAllPaths(path =>
            {
                if (selectedPath == null || path == selectedPath)
                {
                    path.Highlighted = state && nodeDirectionType.HasFlag(TrafficNodeDirectionType.Right);
                }
                else
                {
                    path.Highlighted = false;
                }
            });

            trafficNode.IterateExternalPaths(path =>
            {
                if (selectedPath == null || path == selectedPath)
                {
                    path.Highlighted = state && nodeDirectionType.HasFlag(TrafficNodeDirectionType.Left);
                }
                else
                {
                    path.Highlighted = false;
                }
            });
        }

        public static void ShowWaypointInfo(TrafficNode trafficNode, GUIStyle gUIStyle, bool showWaypointsInfo, int pathIndex = -1)
        {
            int pathIndexCounter = 0;

            for (int laneIndex = 0; laneIndex < trafficNode.Lanes.Count; laneIndex++)
            {
                for (int i = 0; i < trafficNode.Lanes[laneIndex].paths.Count; i++)
                {
                    if (pathIndex == -1 || pathIndex == pathIndexCounter)
                    {
                        var path = trafficNode.Lanes[laneIndex].paths[i];

                        for (int j = 0; j < path.WayPoints.Count; j++)
                        {
                            Handles.DrawWireDisc(path.WayPoints[j].transform.position, Vector3.up, 0.7f);

                            if (showWaypointsInfo)
                            {
                                PathEditorExtension.DrawWaypointInfo(path.WayPoints[j], j + 1, gUIStyle);
                            }
                        }
                    }

                    pathIndexCounter++;
                }
            }
        }

        public static void ShowDivider(TrafficNode trafficNode)
        {
            if (trafficNode.DividerWidth == 0)
            {
                return;
            }

            var mat = Handles.matrix;
            var color = Handles.color;
            var size = trafficNode.GetColliderSize();
            size.x = trafficNode.DividerWidth;

            Handles.color = Color.magenta;
            Handles.matrix = Matrix4x4.TRS(trafficNode.transform.position + new Vector3(0, size.y / 2), trafficNode.transform.rotation, Vector3.one);
            Handles.DrawWireCube(Vector3.zero, size);

            Handles.matrix = mat;
            Handles.color = color;
        }
    }
}
#endif
