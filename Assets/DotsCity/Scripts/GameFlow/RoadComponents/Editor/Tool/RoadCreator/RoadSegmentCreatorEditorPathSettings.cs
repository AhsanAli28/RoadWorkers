#if UNITY_EDITOR
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public partial class RoadSegmentCreatorEditor : Editor
    {
        private const int maxRowSelectionGridCount = 5;
        private const string AutoCrossroadTip = "AutoCrossroad";
        private const string IntersectionCreationTip = "IntersectionCreation";
        private string[] autoCrossroadPopup;

        private TrafficNode[] sceneTrafficNodes;
        private List<TrafficNode> selectedTrafficNodes = new List<TrafficNode>();

        private void ShowPathSettings()
        {
            bool showCurvedRoadSettings = true;
            bool showStraightRoadSettings = true;

            if (creator.GetRoadSegmentType == RoadSegmentCreator.RoadSegmentType.CustomSegment)
            {
                showStraightRoadSettings = false;
                showCurvedRoadSettings = false;
            }

            if (creator.IsStraightRoad())
            {
                showCurvedRoadSettings = false;
            }

            EditorGUI.BeginChangeCheck();

            if (creator.trafficNodeHeaders?.Length > 1)
            {
                var headerCount = creator.trafficNodeHeaders.Length - 1;

                if (headerCount != creator.CreatedTrafficNodes.Count)
                {
                    creator.InitializeTrafficNodeHeaders();
                }

                creator.selectedTrafficNodeIndex = GUILayout.SelectionGrid(creator.selectedTrafficNodeIndex + 1, creator.trafficNodeHeaders, maxRowSelectionGridCount) - 1;
            }

            if (EditorGUI.EndChangeCheck())
            {
                creator.InitializePathHeaders();
                SceneView.RepaintAll();
            }

            EditorGUI.BeginChangeCheck();

            if (creator.pathHeaders?.Length > 1)
            {
                creator.selectedPathIndex = GUILayout.SelectionGrid(creator.selectedPathIndex + 1, creator.pathHeaders, maxRowSelectionGridCount) - 1;
            }

            if (EditorGUI.EndChangeCheck())
            {
                creator.OnPathSelectionChanged();
                SceneView.RepaintAll();
            }

            if (showStraightRoadSettings || showCurvedRoadSettings)
            {
                InspectorExtension.DrawGroupBox("Road Settings", () =>
                {
                    if (showStraightRoadSettings)
                    {
                        if (creator.GetRoadSegmentType != RoadSegmentCreator.RoadSegmentType.CustomStraightRoad)
                        {
                            GUILayout.BeginVertical("GroupBox");

                            EditorGUILayout.LabelField("Straight Road Settings", EditorStyles.boldLabel);

                            EditorGUI.BeginChangeCheck();

                            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.wayPointStraightRoadCount)));

                            if (EditorGUI.EndChangeCheck())
                            {
                                serializedObject.ApplyModifiedProperties();
                                creator.OnWayPointStraightCountChanged();
                            }

                            EditorGUI.BeginChangeCheck();

                            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.straightRoadPathSpeedLimit)));
                            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.straightRoadPriority)));

                            if (EditorGUI.EndChangeCheck())
                            {
                                serializedObject.ApplyModifiedProperties();
                                creator.OnStraightSpeedLimitChanged();
                            }

                            GUILayout.EndVertical();
                        }
                        else
                        {
                            GUILayout.BeginVertical("GroupBox");

                            EditorGUILayout.LabelField("Path Settings", EditorStyles.boldLabel);

                            EditorGUI.BeginChangeCheck();

                            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.straightRoadPathSpeedLimit)), new GUIContent("Speed Limit"));

                            if (EditorGUI.EndChangeCheck())
                            {
                                serializedObject.ApplyModifiedProperties();
                                creator.OnStraightSpeedLimitChanged();
                            }

                            GUILayout.EndVertical();
                        }
                    }

                    if (showCurvedRoadSettings)
                    {
                        GUILayout.BeginVertical("GroupBox");

                        EditorGUILayout.LabelField("Turn Road Settings", EditorStyles.boldLabel);

                        EditorGUI.BeginChangeCheck();

                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.turnCurveType)));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.wayPointTurnCurveCount)));

                        if (EditorGUI.EndChangeCheck())
                        {
                            serializedObject.ApplyModifiedProperties();
                            creator.OnWayPointTurnCountChanged();
                        }

                        EditorGUI.BeginChangeCheck();

                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.turnRoadPathSpeedLimit)));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.turnRoadPriority)));

                        if (EditorGUI.EndChangeCheck())
                        {
                            serializedObject.ApplyModifiedProperties();
                            creator.OnTurnSpeedLimitChanged();
                        }

                        GUILayout.EndVertical();
                    }

                }, ref segmentEditorSettings.PathRoadSettingsSubFoldOut);
            }

            EditorGUI.BeginChangeCheck();

            GUILayout.BeginVertical("GroupBox");

            EditorGUILayout.LabelField("Scene Settings", EditorStyles.boldLabel);

            if (creator.IsCustom())
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.showTrafficNodeHandles)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.showTrafficNodeForward)));
            }

            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(configSo.FindProperty("pathDirection"));

            if (EditorGUI.EndChangeCheck())
            {
                configSo.ApplyModifiedProperties();
                creator.InitializePathHeaders();
                SceneView.RepaintAll();
            }

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.showPathHandles)));

            if (creator.showPathHandles)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.showEditButtonsPathNodes)));
            }

            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.showWaypoints)));

            if (creator.showWaypoints)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.showWaypointsInfo)));
            }

            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }

            if (creator.IsCustom(false))
            {
                ShowCustomAdditionalTab();
            }

            GUILayout.EndVertical();

            EditorGUI.BeginChangeCheck();

            if (creator.CustomTurnSupport())
            {
                InspectorExtension.DrawGroupBox("Turn Connection Settings", () =>
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.customNodeTurnSettings)));

                    if (!creator.customNodeTurnSettings)
                    {
                        GUILayout.BeginVertical("GroupBox");

                        DrawCustomTurnSetting(creator.customTurnDatas[0]);

                        GUILayout.EndVertical();
                    }
                    else
                    {
                        for (int i = 0; i < creator.TrafficNodeCount; i++)
                        {
                            GUILayout.BeginVertical("GroupBox");

                            EditorGUILayout.LabelField(creator.TryToGetNode(i).name, EditorStyles.boldLabel);
                            var customTurnData = creator.customTurnDatas[i];
                            DrawCustomTurnSetting(customTurnData);

                            GUILayout.EndVertical();
                        }
                    }
                }, ref segmentEditorSettings.TurnConnectionSettingsFoldOut);

                InspectorExtension.DrawGroupBox("U Turn Settings", () =>
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.customNodeUTurnSettings)));

                    if (!creator.customNodeUTurnSettings)
                    {
                        var sourceValue = creator.GetUTurnData(0);
                        var newVal = EditorGUILayout.Toggle("U Turn Enabled", sourceValue);

                        if (sourceValue != newVal)
                        {
                            for (int i = 0; i < creator.TrafficNodeCount; i++)
                            {
                                creator.SetUTurnData(i, newVal);
                            }
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"U Turn Node Enabled");

                        EditorGUILayout.BeginHorizontal();

                        for (int i = 0; i < creator.TrafficNodeCount; i++)
                        {
                            if (!creator.UTurnIsAvailable(i)) continue;

                            var index = i + 1;

                            EditorGUILayout.LabelField(index.ToString(), GUILayout.Width(10f));

                            var newVal = EditorGUILayout.Toggle(creator.GetUTurnData(i));
                            creator.SetUTurnData(i, newVal);
                        }

                        EditorGUILayout.EndHorizontal();
                    }

                    if (creator.AnyUTurn())
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.stretchFactor)));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.stretchAngle)));
                    }

                }, ref segmentEditorSettings.UTurnConnectionSettingsFoldOut);
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                creator.Create();
            }

            if (creator.selectedTrafficNodeIndex >= 0)
            {
                GUI.enabled = creator.selectedPathIndex >= 0;

                if (GUILayout.Button("Open Path Settings"))
                {
                    OpenPathSettingsWindow();
                }

                GUI.enabled = true;
            }
        }

        private void CreateIntersection(bool autoGenerateCrossing = true, bool autoMerge = true)
        {
            selectedTrafficNodes = selectedTrafficNodes.Where(a => a != null).ToList();

            if (selectedTrafficNodes.Count < 2)
                return;

            creator.customTrafficNodeSettings = true;

            if (creator.CreatedTrafficNodes.Count > selectedTrafficNodes.Count)
            {
                int count = creator.CreatedTrafficNodes.Count - selectedTrafficNodes.Count;

                while (count > 0)
                {
                    var node = creator.CreatedTrafficNodes[creator.CreatedTrafficNodes.Count - 1];
                    creator.DestroyNode(node, false, false);
                    count--;
                }
            }

            if (creator.CreatedTrafficNodes.Count < selectedTrafficNodes.Count)
            {
                int count = selectedTrafficNodes.Count - creator.CreatedTrafficNodes.Count;

                for (var i = 0; i < count; i++)
                {
                    creator.AddTrafficNode();
                }
            }

            for (int i = 0; i < selectedTrafficNodes.Count; i++)
            {
                var sourceNode = selectedTrafficNodes[i];
                var node = creator.TryToGetNode(i);

                node.CloneSettings(sourceNode, true, true);

                if (autoMerge)
                    node.SetMerge(sourceNode);
            }

            if (autoGenerateCrossing)
                creator.CreateAutoCrossroad();

            selectedTrafficNodes.Clear();
        }

        private void ShowCustomAdditionalTab()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.additionalSettings)));

            var nonDefaultBox = creator.additionalSettings != RoadSegmentCreator.AdditionalPathSettingsType.Default;

            if (nonDefaultBox)
            {
                GUILayout.BeginVertical("HelpBox");
            }

            if (creator.additionalSettings == RoadSegmentCreator.AdditionalPathSettingsType.ExtrudeLane)
            {
                EditorGUILayout.PropertyField(configSo.FindProperty("selectAfterCreation"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.turnRoadPathSpeedLimit)), new GUIContent("New Lane Speed Limit"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.turnRoadPriority)), new GUIContent("New Lane Priority"));

                Config.ExtrudeLaneCount = EditorGUILayout.IntSlider("Extrude Lane Count", Config.ExtrudeLaneCount, 1, creator.MaxExtrudeLaneCount);

                GUI.enabled = creator.SourceExtrudeNode;

                if (GUILayout.Button("Create"))
                {
                    creator.CreateLaneExtrude();
                }

                GUI.enabled = true;
            }

            if (creator.additionalSettings == RoadSegmentCreator.AdditionalPathSettingsType.AutoCrossroad)
            {
                EditorTipExtension.TryToShowInspectorTip(AutoCrossroadTip, "Auto crossroad is used for quick generation of custom crossroad with custom shape. Place Traffic nodes at the entrances/exits of the intersection and press Create button.");

                var cleanupSelfIntersectionProp = serializedObject.FindProperty("cleanupSelfIntersection");

                EditorGUILayout.PropertyField(cleanupSelfIntersectionProp);

                if (cleanupSelfIntersectionProp.boolValue)
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("inverseCleanup"));

                if (creator.ignoreConnections.Count > 0)
                {
                    EditorGUILayout.BeginVertical("GroupBox");

                    EditorGUILayout.LabelField($"Ignore {creator.ignoreConnections.Count} connections:", EditorStyles.boldLabel);

                    Vector2Int removeElement = default;

                    foreach (var item in creator.ignoreConnections)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"Traffic node connection [{item.x + 1}, {item.y + 1}] is ignored");

                        if (GUILayout.Button("X", GUILayout.Width(25f)))
                        {
                            removeElement = item;
                        }

                        EditorGUILayout.EndHorizontal();
                    }

                    if (!removeElement.Equals(default))
                    {
                        creator.RemoveIgnore(removeElement);
                    }

                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField("Ignore Source/Target Traffic Node indexes");

                if (autoCrossroadPopup == null || autoCrossroadPopup.Length != creator.TrafficNodeCount)
                {
                    autoCrossroadPopup = new string[creator.TrafficNodeCount];

                    for (int i = 0; i < autoCrossroadPopup.Length; i++)
                    {
                        autoCrossroadPopup[i] = (i + 1).ToString();
                    }
                }

                var labelWidth = EditorGUIUtility.labelWidth;

                EditorGUIUtility.labelWidth = 0;

                creator.sourceIgnoreIndex = EditorGUILayout.Popup(string.Empty, creator.sourceIgnoreIndex, autoCrossroadPopup, GUILayout.MaxWidth(50));
                creator.targetIgnoreIndex = EditorGUILayout.Popup(string.Empty, creator.targetIgnoreIndex, autoCrossroadPopup, GUILayout.MaxWidth(50));

                EditorGUIUtility.labelWidth = labelWidth;
                if (GUILayout.Button("Add", GUILayout.MaxWidth(40)))
                {
                    creator.AddAutoIgnore();
                }

                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("Clear"))
                {
                    creator.ClearAutoPaths();
                }

                if (GUILayout.Button("Create"))
                {
                    creator.CreateAutoCrossroad();
                }
            }

            if (creator.additionalSettings == RoadSegmentCreator.AdditionalPathSettingsType.IntersectionCreation)
            {
                if (sceneTrafficNodes == null)
                {
                    sceneTrafficNodes = ObjectUtils.FindObjectsOfType<TrafficNode>().Where(a => !creator.CreatedTrafficNodes.Contains(a)).ToArray();
                }

                EditorTipExtension.TryToShowInspectorTip(IntersectionCreationTip, "Intersection creation is used to create intersections between existing straight roads in the scene. Select start/end of each road & press Create button");

                var autoMergeNodesProp = configSo.FindProperty("autoMergeNodes");
                var autoGenerateCrossingProp = configSo.FindProperty("autoGenerateCrossing");
                EditorGUILayout.PropertyField(autoMergeNodesProp);
                EditorGUILayout.PropertyField(autoGenerateCrossingProp);

                EditorGUILayout.LabelField($"Selected {selectedTrafficNodes.Count} Nodes", EditorStyles.boldLabel);

                GUI.enabled = selectedTrafficNodes.Count >= 2;

                if (GUILayout.Button("Clear"))
                {
                    selectedTrafficNodes.Clear();
                    Repaint();
                }

                if (GUILayout.Button("Create"))
                {
                    CreateIntersection(autoGenerateCrossingProp.boolValue, autoMergeNodesProp.boolValue);
                }

                GUI.enabled = true;
            }

            if (nonDefaultBox)
            {
                GUILayout.EndVertical();
            }
        }
    }
}
#endif
