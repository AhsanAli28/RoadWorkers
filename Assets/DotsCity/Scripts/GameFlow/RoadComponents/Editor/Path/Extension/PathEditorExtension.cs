#if UNITY_EDITOR
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public static class PathEditorExtension
    {
        private static bool shouldRecordUndo;
        private static GUIStyle infoGUIStyleCached;

        public static void DrawWaypointInfo(
            Path path,
            GUIStyle infoGUIStyle,
            bool includeAdditionalSettings = false)
        {
            for (int i = 0; i < path.WayPoints?.Count; i++)
            {
                int index = i + 1;

                DrawWaypointInfo(path.WayPoints[i], index, infoGUIStyle, includeAdditionalSettings);
            }
        }

        public static void DrawWaypointInfo(
            PathNode wayPoint,
            int index,
            GUIStyle infoGUIStyle,
            bool includeAdditionalSettings = false)
        {
            if (infoGUIStyle == null)
            {
                infoGUIStyle = GetDefaultFontStyle();
            }

            var speedLimit = wayPoint.SpeedLimit;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append($"Waypoint {index}");
            stringBuilder.Append(Environment.NewLine);
            stringBuilder.Append($"SpeedLimit: {speedLimit}");
            stringBuilder.Append(Environment.NewLine);

            if (includeAdditionalSettings)
            {
                if (wayPoint.BackwardDirection)
                {
                    stringBuilder.Append("BackwardNode");
                    stringBuilder.Append(Environment.NewLine);
                }

                if (wayPoint.CustomGroup)
                {
                    stringBuilder.Append("Group: ");
                    stringBuilder.Append(wayPoint.CustomGroupType);
                    stringBuilder.Append(Environment.NewLine);
                }
            }

            Handles.Label(wayPoint.transform.position, stringBuilder.ToString(), infoGUIStyle);
        }

        public static GUIStyle GetDefaultFontStyle()
        {
            if (infoGUIStyleCached == null)
            {
                infoGUIStyleCached = new GUIStyle();
                infoGUIStyleCached.normal.textColor = Color.white;
            }

            return infoGUIStyleCached;
        }

        public static bool DrawPathHandles(
            Path path,
            GUIStyle infoGUIStyle = null,
            bool showEditButtons = true,
            bool drawRotationHandle = false,
            bool lockYAxis = true,
            bool showYPosition = false,
            bool roundYPosition = true,
            bool recordWaypoints = true,
            float roundValue = 0.05f,
            bool allowEdgeHandle = false,
            Action<Path, int> customAddNodeCallback = null,
            Action<Path, int> customRemoveNodeCallback = null,
            Action<Path, int, Vector3> customPositionNodeCallback = null,
            Action<Path, int, Quaternion> customRotationNodeCallback = null)
        {
            if (infoGUIStyle == null)
            {
                infoGUIStyle = new GUIStyle();
                infoGUIStyle.normal.textColor = Color.white;
            }

            Event e = Event.current;

            if (e.button == 0 && e.type == EventType.MouseDown)
            {
                shouldRecordUndo = true;
            }

            float squareSize = 1f;
            float maxPickSize = squareSize;
            float minPickSize = squareSize * 0.2f;

            var sceneView = SceneView.currentDrawingSceneView;

            if (sceneView)
            {
                var sceneViewSize = sceneView.size;
                var calcPickSize = squareSize * Mathf.Clamp01(sceneViewSize / 10);
                squareSize = Mathf.Clamp(calcPickSize, minPickSize, maxPickSize);
            }

            var circleRadius = squareSize / 3f;

            bool isEdited = false;

            for (int i = 0; i < path?.Nodes?.Count; i++)
            {
                if (path.Nodes[i] == null)
                {
                    continue;
                }

                var position = path.Nodes[i].transform.position;

                if (showYPosition)
                {
                    int fontSize = infoGUIStyle.fontSize;
                    infoGUIStyle.fontSize = 30;
                    Handles.Label(position + new Vector3(0, 2.5f), position.y.ToString(), infoGUIStyle);
                    infoGUIStyle.fontSize = fontSize;
                }

                if (showEditButtons)
                {
                    if (i < path.Nodes.Count - 1)
                    {
                        var nextPathNode = path.Nodes[i + 1];
                        var addPosition = (path.Nodes[i].transform.position + nextPathNode.transform.position) / 2;

                        Action addAction = () =>
                        {
                            int index = i + 1;
                            path.InsertNode(addPosition, index, true, true);
                            isEdited = true;
                            customAddNodeCallback?.Invoke(path, index);
                        };

                        var sourceMatrix = Handles.matrix;

                        var rot = Quaternion.LookRotation((nextPathNode.transform.position - path.Nodes[i].transform.position).Flat());
                        Matrix4x4 rotationMatrix = Matrix4x4.TRS(addPosition, rot, Vector3.one);

                        Handles.matrix = rotationMatrix;
                        Handles.DrawWireCube(Vector3.zero, new Vector3(squareSize, 0.1f, squareSize));

                        Handles.matrix = sourceMatrix;
                        EditorExtension.DrawButton("+", addPosition, 35f, addAction, fontSize: 16);
                    }
                }

                bool isEdgeNode = i == 0 || i == path.Nodes.Count - 1;

                Handles.DrawWireDisc(path.Nodes[i].transform.position, Vector3.up, circleRadius);

                if (!isEdgeNode)
                {
                    if (showEditButtons)
                    {
                        Action removeAction = () =>
                        {
                            int index = i;
                            path.RemoveNodeAt(index);
                            isEdited = true;
                            customRemoveNodeCallback?.Invoke(path, index);
                        };

                        EditorExtension.DrawButton("x", position, 35f, removeAction, fontSize: 16);
                    }
                }

                bool isLastNode = i == path.Nodes.Count - 1;

                bool showHandles =
                    !isEdgeNode ||
                    (i == 0 && path.SourceTrafficNode == null && allowEdgeHandle) ||
                    ((isLastNode && path.ConnectedTrafficNode == null && allowEdgeHandle) ||
                    (isLastNode && path.PathConnectionType == PathConnectionType.PathPoint));

                if (showHandles)
                {
                    EditorGUI.BeginChangeCheck();

                    var oldPosition = path.Nodes[i].transform.position;
                    var newPosition = Handles.PositionHandle(path.Nodes[i].transform.position, path.Nodes[i].transform.rotation);

                    if (EditorGUI.EndChangeCheck())
                    {
                        if (shouldRecordUndo)
                        {
                            Undo.RecordObject(path.Nodes[i].transform, "Undo Change Node Position");
                        }

                        if (lockYAxis)
                        {
                            newPosition.y = oldPosition.y;
                        }
                        else if (roundYPosition)
                        {
                            newPosition.y = roundValue * Mathf.RoundToInt(newPosition.y / roundValue);
                        }

                        if (path.PathConnectionType == PathConnectionType.PathPoint && path.AutoAttachPath && isLastNode && path.ConnectedPath != null)
                        {
                            Vector3 intersectPoint = PathHelper.GetAttachPoint(path.ConnectedPath, newPosition);

                            newPosition = intersectPoint;
                        }

                        var offset = newPosition - path.Nodes[i].transform.position;

                        path.Nodes[i].transform.position = newPosition;

                        TryToMoveTangent(path, i, offset);

                        path.CreatePath(false, recordWaypoints);

                        if (shouldRecordUndo)
                        {
                            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
                            shouldRecordUndo = false;
                        }

                        isEdited = true;
                        customPositionNodeCallback?.Invoke(path, i, oldPosition);
                    }

                    if (drawRotationHandle)
                    {
                        EditorGUI.BeginChangeCheck();

                        var oldRotation = path.Nodes[i].transform.rotation;
                        var newRotation = Handles.Disc(oldRotation, path.Nodes[i].transform.position, Vector3.up, 3f, false, 2f);

                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(path.Nodes[i].transform, "Undo Change Node Rotation");

                            path.Nodes[i].transform.rotation = newRotation;

                            path.CreatePath(false);

                            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
                            isEdited = true;
                            customRotationNodeCallback?.Invoke(path, i, oldRotation);
                        }
                    }
                }
            }

            return isEdited;
        }

        private static void TryToMoveTangent(Path path, int index, Vector3 offset)
        {
            if (path.ClampTangent)
            {
                var currentTangentOffset = offset * -1;

                switch (path.PathCurveType)
                {
                    case PathCurveType.StraightLine:
                        break;
                    case PathCurveType.BezierCube:
                        {
                            bool tangent = index % 2 == 1;

                            if (tangent)
                            {
                                Transform nextTangent = null;
                                Transform previousTangent = null;

                                if (index - 2 >= 0)
                                {
                                    previousTangent = path.Nodes[index - 2];
                                }

                                if (index + 2 < path.Nodes.Count)
                                {
                                    nextTangent = path.Nodes[index + 2];
                                }

                                if (previousTangent != null)
                                {
                                    if (shouldRecordUndo)
                                    {
                                        Undo.RecordObject(previousTangent.transform, "Undo Change Node Position");
                                    }

                                    previousTangent.transform.position += currentTangentOffset;
                                }

                                if (nextTangent != null)
                                {
                                    if (shouldRecordUndo)
                                    {
                                        Undo.RecordObject(nextTangent.transform, "Undo Change Node Position");
                                    }

                                    nextTangent.transform.position += currentTangentOffset;
                                }
                            }
                            else
                            {
                                Transform nextTangent = null;
                                Transform previousTangent = null;

                                if (index - 1 >= 0)
                                {
                                    previousTangent = path.Nodes[index - 1];
                                }
                                if (index + 1 < path.Nodes.Count)
                                {
                                    nextTangent = path.Nodes[index + 1];
                                }

                                if (previousTangent != null)
                                {
                                    if (shouldRecordUndo)
                                    {
                                        Undo.RecordObject(previousTangent.transform, "Undo Change Node Position");
                                    }

                                    previousTangent.transform.position += offset;
                                }

                                if (nextTangent != null)
                                {
                                    if (shouldRecordUndo)
                                    {
                                        Undo.RecordObject(nextTangent.transform, "Undo Change Node Position");
                                    }

                                    nextTangent.transform.position += offset;
                                }
                            }

                            break;
                        }
                    case PathCurveType.BezierQuad:
                        {
                            bool isStartTangent = false;
                            bool isEndTangent = false;

                            int segmentNumber = Mathf.FloorToInt(index / 3);

                            if (index == 3 * segmentNumber + 1)
                            {
                                isStartTangent = true;
                            }
                            if (index == 3 * segmentNumber + 2)
                            {
                                isEndTangent = true;
                            }

                            if (isStartTangent)
                            {
                                if (index - 2 >= 0)
                                {
                                    var startTangent = path.Nodes[index - 2].transform;

                                    if (shouldRecordUndo)
                                    {
                                        Undo.RecordObject(startTangent.transform, "Undo Change Node Position");
                                    }

                                    startTangent.transform.position += currentTangentOffset;
                                }
                            }
                            if (isEndTangent)
                            {
                                if (index + 2 < path.Nodes.Count)
                                {
                                    var endTangent = path.Nodes[index + 2].transform;

                                    if (shouldRecordUndo)
                                    {
                                        Undo.RecordObject(endTangent.transform, "Undo Change Node Position");
                                    }

                                    endTangent.transform.position += currentTangentOffset;
                                }
                            }

                            if (!isStartTangent && !isEndTangent)
                            {
                                if (index - 1 >= 0)
                                {
                                    var startTangent = path.Nodes[index - 1].transform;

                                    if (shouldRecordUndo)
                                    {
                                        Undo.RecordObject(startTangent.transform, "Undo Change Node Position");
                                    }

                                    startTangent.transform.position += offset;
                                }
                                if (index + 1 < path.Nodes.Count)
                                {
                                    var endTangent = path.Nodes[index + 1].transform;

                                    if (shouldRecordUndo)
                                    {
                                        Undo.RecordObject(endTangent.transform, "Undo Change Node Position");
                                    }

                                    endTangent.transform.position += offset;
                                }
                            }

                            break;
                        }
                }
            }
        }
    }
}
#endif