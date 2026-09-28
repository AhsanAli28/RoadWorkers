#if UNITY_EDITOR
using Spirit604.Attributes;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Path))]
    public class PathEditor : Editor
    {
        #region Consts

        private const string DocLink = "https://dotstrafficcity.readthedocs.io/en/latest/path.htm";
        private const float DottedLineSize = 5f;
        private const float AddButtonScreenWidth = 50f;

        #endregion

        #region Variables

        private GUIStyle infoGUIStyle = new GUIStyle();
        private GUIStyle indexGUIStyle = new GUIStyle();
        private GUIStyle buttonGuiStyle;
        private bool roundYPosition = true;
        private bool showYPosition = false;
        private float roundValue = 0.05f;
        private Path path;
        private Path[] paths;

        private bool cachedFoldout;
        private bool settingsFoldout = true;
        private bool visualSettingsFoldout = true;
        private bool deleted;
        private static List<TrafficNode> worldTrafficNodes;
        private PathAttachWindow pathAttachWindow;
        private bool speedLimitUpdated;

        #endregion

        #region Unity methods

        private void OnEnable()
        {
            path = target as Path;
            paths = ObjectUtils.FindObjectsOfType<Path>().Where(item => item != path).ToArray();

            infoGUIStyle.normal.textColor = Color.white;
            indexGUIStyle.normal.textColor = Color.white;
            indexGUIStyle.fontSize = 16;
            indexGUIStyle.fontStyle = FontStyle.Bold;

            buttonGuiStyle = new GUIStyle("button");
            buttonGuiStyle.fontSize = 24;
            buttonGuiStyle.normal.textColor = Color.black;

            path.Selected = true;

            path.SwitchConnectedPathHighlightState(true);

            LoadSettings();

            Undo.undoRedoPerformed += Undo_undoRedoPerformed;
            EditorApplication.hierarchyWindowItemOnGUI += EditorApplication_hierarchyWindowItemOnGUI;
        }

        private void OnDisable()
        {
            path.Selected = false;

            SaveSettings();

            path.SwitchConnectedPathHighlightState(false);

            Undo.undoRedoPerformed -= Undo_undoRedoPerformed;
            EditorApplication.hierarchyWindowItemOnGUI -= EditorApplication_hierarchyWindowItemOnGUI;
        }

        public override void OnInspectorGUI()
        {
            var path = target as Path;

            serializedObject.Update();

            DocumentationLinkerUtils.ShowButtonAndHeader(target, DocLink, -2);

            if (Selection.objects.Length == 1)
            {
                Action cachedValuesCallback = () =>
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("nodesParent"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("wayPointsParent"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("sourceTrafficNode"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("pathConnectionType"));

                    switch (path.PathConnectionType)
                    {
                        case PathConnectionType.TrafficNode:
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("connectedTrafficNode"));
                            break;
                        case PathConnectionType.PathPoint:
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("connectedPath"));
                            break;
                    }

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("customLightHandler"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("nodes"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("wayPoints"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("intersects"));
                };

                InspectorExtension.DrawDefaultInspectorGroupBlock("Cached values", cachedValuesCallback, ref cachedFoldout);
            }

            Action settingsValuesCallback = () =>
            {
                if (Selection.objects.Length == 1)
                {
                    GUI.enabled = false;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("pathLength"));
                    GUI.enabled = true;
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("pathCurveType"), new GUIContent("Curve Type"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("pathRoadType"), new GUIContent("Road Type"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("trafficGroupMask"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("priority"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("wayPointsCountPerCurve"));

                if (speedLimitUpdated)
                {
                    var r = GUILayoutUtility.GetLastRect();

                    r.x = 120;
                    r.width = 40;
                    r.y += r.height + 2;

                    if (GUI.Button(r, "Set"))
                    {
                        speedLimitUpdated = false;
                        path.ResetSpeedLimit();
                    }
                }

                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("pathSpeedLimit"), new GUIContent("Speed Limit"));

                if (EditorGUI.EndChangeCheck())
                {
                    speedLimitUpdated = true;
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("connectedLaneIndex"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("HightlightNormalizedLength"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("rail"));

                if (path.CanUseReverseConnection)
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("reversedConnectionSide"));
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Settings", settingsValuesCallback, ref settingsFoldout);

            Action visualSettingsValuesCallback = () =>
            {
                EditorGUILayout.LabelField("Common Settings", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("showInfoWaypoints"));

                if (path.ShowInfoWaypoints)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("showAdditionalInfo"));
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("lockYAxis"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ShowIntersectedPoints"));

                var showHandlesProp = serializedObject.FindProperty("ShowHandles");

                EditorGUILayout.PropertyField(showHandlesProp);

                if (showHandlesProp.boolValue)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("ShowEditButtons"));
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("HighlightColor"));

                showYPosition = EditorGUILayout.Toggle("Show Y Position", showYPosition);

                if (!path.LockYAxis)
                {
                    roundYPosition = EditorGUILayout.Toggle("Round Y Position", roundYPosition);

                    if (roundYPosition)
                    {
                        roundValue = EditorGUILayout.Slider("Round Y Value", roundValue, 0f, 5f);
                    }
                }

                if (Selection.objects.Length == 1)
                {
                    EditorGUILayout.Separator();

                    if (path.PathCurveType != PathCurveType.StraightLine)
                    {
                        EditorGUILayout.LabelField("Curve Settings", EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("drawTangent"));

                        var buttonRect = GUILayoutUtility.GetLastRect();

                        const float convertButtonRelativeWidthRate = 0.5f;

                        var width = buttonRect.width * convertButtonRelativeWidthRate;

                        buttonRect.x += (buttonRect.width - width);
                        buttonRect.y += EditorGUIUtility.singleLineHeight / 2;
                        buttonRect.width = width;
                        buttonRect.height *= 1.2f;

                        if (GUI.Button(buttonRect, "Convert To Straight Line"))
                        {
                            path.ConvertToStraightLine();
                        }

                        EditorGUILayout.PropertyField(serializedObject.FindProperty("clampTangent"));
                    }
                    else
                    {
                        GUI.enabled = false;
                        EditorGUILayout.LabelField("Curve Settings", EditorStyles.boldLabel);
                        GUI.enabled = true;
                    }

                    if (path.PathConnectionType == PathConnectionType.PathPoint)
                    {
                        EditorGUILayout.Separator();
                        EditorGUILayout.LabelField("Path Point Settings", EditorStyles.boldLabel);

                        EditorGUILayout.PropertyField(serializedObject.FindProperty("autoAttachPath"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("showAttachPathButtons"));

                        EditorGUI.BeginChangeCheck();

                        if (EditorGUI.EndChangeCheck())
                        {
                            serializedObject.ApplyModifiedProperties();
                            path.SwitchConnectedPathHighlightState(true);
                            SceneView.RepaintAll();
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("highlightConnectedPath"));
                        }
                    }
                }
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Visual Settings", visualSettingsValuesCallback, ref visualSettingsFoldout);

            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Open Path Settings"))
            {
                var pathEditor = PathSettingsWindowEditor.ShowWindow();
                pathEditor.Initialize(path);
            }

            //if (GUILayout.Button("Open Attach Window"))
            //{
            //    path.OpenAttachWindow();
            //}

            if (GUILayout.Button("Create Path"))
            {
                RecreatePath(path);
            }

            if (Selection.objects.Length == 1)
            {
                if (path.HasCustomLight)
                {
                    if (GUILayout.Button("Remove Custom Light"))
                    {
                        path.TryToRemoveCustomLight(true);
                    }
                }
                else
                {
                    if (GUILayout.Button("Add Custom Light"))
                    {
                        path.TryToAddCustomLight();
                    }
                }
            }

            if (GUILayout.Button("Reset Speed Limit"))
            {
                path.ResetSpeedLimit();
            }

            if (Selection.objects.Length == 1)
            {
                if (path.PathCurveType != PathCurveType.StraightLine)
                {
                    if (GUILayout.Button("Reset Tangent Positions"))
                    {
                        ResetTangents(path);
                    }
                }
            }
        }

        private void OnSceneGUI()
        {
            Path path = target as Path;
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            if (path.ShowHandles)
            {
                PathEditorExtension.DrawPathHandles(path, infoGUIStyle, showEditButtons: path.ShowEditButtons, lockYAxis: path.LockYAxis, showYPosition: showYPosition, roundYPosition: roundYPosition, recordWaypoints: false, roundValue: roundValue, allowEdgeHandle: true);
            }

            if (path.ShowInfoWaypoints)
            {
                bool showAdditionalSettings = path.ShowAdditionalInfo;

                DrawInfoWaypoints(path, showAdditionalSettings);
            }

            HandleKeys(path);
            HandleAttachNodes(path);
            TryToDrawTangents(path);

            if (path.PathConnectionType == PathConnectionType.PathPoint && path.ShowAttachButtons)
            {
                ShowAttachButtons();
            }
        }

        #endregion

        #region Private

        private void TryToDrawTangents(Path path)
        {
            if (!path.DrawTangent)
            {
                return;
            }

            var nodes = path.Nodes;

            switch (path.PathCurveType)
            {
                case PathCurveType.BezierCube:
                    {
                        int index = 1;

                        while (index < nodes.Count)
                        {
                            int previousIndex = index - 1;
                            int nextIndex = index + 1;

                            if (previousIndex >= 0)
                            {
                                Handles.DrawDottedLine(nodes[previousIndex].transform.position, nodes[index].transform.position, DottedLineSize);
                            }
                            if (nextIndex < nodes.Count)
                            {
                                Handles.DrawDottedLine(nodes[index].transform.position, nodes[nextIndex].transform.position, DottedLineSize);
                            }

                            index += 2;
                        }

                        break;
                    }
                case PathCurveType.BezierQuad:
                    {
                        int index = 1;

                        while (index < nodes.Count)
                        {
                            int previousIndex = index - 1;
                            int nextTangentIndex = index + 1;
                            int nextNode = index + 2;

                            if (previousIndex >= 0)
                            {
                                Handles.DrawDottedLine(nodes[previousIndex].transform.position, nodes[index].transform.position, DottedLineSize);
                            }
                            if (nextTangentIndex < nodes.Count && nextNode < nodes.Count)
                            {
                                Handles.DrawDottedLine(nodes[nextTangentIndex].transform.position, nodes[nextNode].transform.position, DottedLineSize);
                            }

                            index += 3;
                        }

                        break;
                    }
            }
        }

        private void RecreatePath(Path path)
        {
            path.RecreateAndSaveUndo();
        }

        private void HandleAttachNodes(Path path)
        {
            if (pathAttachWindow != null)
            {
                TrafficNode sourceNode = null;
                TrafficNode targetNode = null;

                if (pathAttachWindow.sourceTrafficNodeGo != null)
                {
                    sourceNode = pathAttachWindow.sourceTrafficNodeGo as TrafficNode;
                    TrafficNodesGUIHelper.DrawSelectedLanePoint(sourceNode, pathAttachWindow.sourceLaneIndex, pathAttachWindow.isRightSide, true);
                }

                if (pathAttachWindow.targetTrafficNodeGo != null)
                {
                    targetNode = pathAttachWindow.targetTrafficNodeGo as TrafficNode;
                    TrafficNodesGUIHelper.DrawSelectedLanePoint(targetNode, pathAttachWindow.TargetLaneIndex, pathAttachWindow.isRightSide, false);
                }

                TrafficNodesGUIHelper.DrawNodeButtons(path.WorldTrafficNodes, sourceNode, targetNode, AddAttachableNode, RemoveAttachableNode);
            }
        }

        private void AddAttachableNode(TrafficNode node)
        {
            pathAttachWindow.AddNode(node);
            pathAttachWindow.Focus();
        }

        private void RemoveAttachableNode(TrafficNode node)
        {
            pathAttachWindow.RemoveNode(node);
            pathAttachWindow.Focus();
        }

        private void HandleKeys(Path path)
        {
            if (Event.current.shift && Event.current.type == EventType.MouseDown)
            {
                Event.current.Use();
                Vector3 spawnPosition = Event.current.mousePosition.GUIScreenToWorldSpace();
                path.AddNode(spawnPosition);
            }

            if (Event.current.control && Event.current.type == EventType.MouseDown)
            {
                Event.current.Use();
                Vector3 worldClickPosition = Event.current.mousePosition.GUIScreenToWorldSpace();
                path.InsertNodeOnLineAtCustomPosition(worldClickPosition);
            }

            if (deleted)
            {
                deleted = false;
                path.DestroyPath(true);
            }
        }

        private void DrawInfoWaypoints(Path path, bool includeAdditionalSettings = false)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            PathEditorExtension.DrawWaypointInfo(path, infoGUIStyle, includeAdditionalSettings);
        }

        private void ShowAttachButtons()
        {
            for (int i = 0; i < paths.Length; i++)
            {
                Path targetPath = paths[i];
                var position = targetPath.GetMiddlePosition();

                bool connected = path.ConnectedPath == targetPath;

                if (!connected)
                {
                    Action addCallback = () =>
                    {
                        path.SwitchConnectedPathHighlightState(false);
                        path.ConnectedPath = targetPath;
                        path.FindConnectedLaneForPathPoint();
                        path.SwitchConnectedPathHighlightState(true);
                        EditorSaver.SetObjectDirty(path);
                    };

                    EditorExtension.DrawButton("+", position, AddButtonScreenWidth, addCallback);
                }
                else
                {
                    Action removeCallback = () =>
                    {
                        path.SwitchConnectedPathHighlightState(false);
                        path.ConnectedPath = null;
                        EditorSaver.SetObjectDirty(path);
                    };

                    EditorExtension.DrawButton("-", position, AddButtonScreenWidth, removeCallback);
                }
            }
        }
        private void ResetTangents(Path path)
        {
            var nodes = path.Nodes;

            switch (path.PathCurveType)
            {
                case PathCurveType.StraightLine:
                    break;
                case PathCurveType.BezierCube:
                    {
                        var index = 3;

                        while (index < nodes.Count)
                        {
                            var previousTangent = nodes[index - 2];
                            var currentTangent = nodes[index];
                            var node = nodes[index - 1];

                            Vector3 offset = node.position - previousTangent.position;

                            Undo.RecordObject(currentTangent.transform, "Undo Change Node Position");
                            currentTangent.transform.position = node.transform.position + offset;
                            index += 2;
                        }

                        break;
                    }
                case PathCurveType.BezierQuad:
                    {
                        int index = 1;

                        while (index < nodes.Count)
                        {
                            int previousTangentIndex = index - 2;

                            if (previousTangentIndex >= 0)
                            {
                                var previousEndTangent = nodes[previousTangentIndex].transform;
                                var node = nodes[index - 1].transform;
                                var currentStartTangent = nodes[index].transform;

                                float distance1 = Vector3.Distance(previousEndTangent.position, node.position);
                                float distance2 = Vector3.Distance(node.position, currentStartTangent.position);

                                if (distance1 < distance2)
                                {
                                    Vector3 offset = node.position - previousEndTangent.position;

                                    Undo.RecordObject(currentStartTangent.transform, "Undo Change Node Position");
                                    currentStartTangent.transform.position = node.position + offset;
                                }
                                else
                                {
                                    Vector3 offset = node.position - currentStartTangent.transform.position;

                                    Undo.RecordObject(previousEndTangent.transform, "Undo Change Node Position");
                                    previousEndTangent.transform.position = node.position + offset;
                                }
                            }

                            index += 3;
                        }


                        break;
                    }
            }

            path.CreatePath(false);
        }

        private void LoadSettings()
        {
            var pathSharedEditorSettings = PathInspectorExtension.GetPrefsSettings();

            cachedFoldout = pathSharedEditorSettings.ShowCached;
            settingsFoldout = pathSharedEditorSettings.ShowSettings;
            visualSettingsFoldout = pathSharedEditorSettings.ShowVisual;
            path.ShowInfoWaypoints = pathSharedEditorSettings.ShowWaypoints;
            path.ShowAdditionalInfo = pathSharedEditorSettings.ShowAdditionalInfo;
            path.ShowHandles = pathSharedEditorSettings.ShowHandles;
            path.ShowEditButtons = pathSharedEditorSettings.ShowEditButtons;
        }

        private void SaveSettings()
        {
            PathSharedEditorSettings pathSharedEditorSettings = default;

            pathSharedEditorSettings.ShowCached = cachedFoldout;
            pathSharedEditorSettings.ShowSettings = settingsFoldout;
            pathSharedEditorSettings.ShowVisual = visualSettingsFoldout;
            pathSharedEditorSettings.ShowWaypoints = path.ShowInfoWaypoints;
            pathSharedEditorSettings.ShowAdditionalInfo = path.ShowAdditionalInfo;
            pathSharedEditorSettings.ShowHandles = path.ShowHandles;
            pathSharedEditorSettings.ShowEditButtons = path.ShowEditButtons;

            var json = JsonUtility.ToJson(pathSharedEditorSettings);
            EditorPrefs.SetString(PathInspectorExtension.PathEditorSettingsKey, json);
        }

        #endregion

        #region Buttons

        public void OpenAttachWindow()
        {
            worldTrafficNodes = ObjectUtils.FindObjectsOfType<TrafficNode>().ToList();
            pathAttachWindow = PathAttachWindow.ShowWindow();
            pathAttachWindow.SelectedPath = path;
        }

        #endregion

        #region Event handlers

        private void Undo_undoRedoPerformed()
        {
            path?.CreatePath(true);
        }

        private void EditorApplication_hierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            var e = Event.current;

            if (e.keyCode == KeyCode.Delete)
            {
                deleted = true;
                e.type = EventType.Used;
            }
        }

        #endregion
    }
}
#endif