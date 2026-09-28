#if UNITY_EDITOR
using Spirit604.Extensions;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public partial class RoadSegmentCreatorEditor : Editor
    {
        private void ShowGeneralSettings(bool showType = true)
        {
            if (showType)
            {
                ShowRoadType();
            }

            var customTrafficNodeSettings = false;

            if (creator.GetRoadSegmentType == RoadSegmentCreator.RoadSegmentType.CustomSegment)
            {
                const string RoadSegmentCreatorCustomKey1 = "RoadSegmentCreatorCustomKey1";

                if (EditorTipExtension.TryToShowInspectorTip(RoadSegmentCreatorCustomKey1,
                    $"Try new auto-crossroad feature in 'Path Settings/Addiotional Settings/Auto Crossroad'. For more info:{System.Environment.NewLine}"))
                {
                    var r = GUILayoutUtility.GetLastRect();

                    r.x += r.width;
                    r.x -= 45f;

                    r.width = 35f;
                    r.height = EditorGUIUtility.singleLineHeight + 2;
                    r.y += EditorGUIUtility.singleLineHeight + 2;

                    if (GUI.Button(r, "Doc"))
                    {
                        Application.OpenURL("https://dotstrafficcity.readthedocs.io/en/latest/roadSegmentCreator.html#auto-crossroad");
                    }
                }

                GUILayout.BeginVertical("HelpBox");

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.customTrafficNodeSettings)), new GUIContent("Customize Nodes"));

                customTrafficNodeSettings = creator.customTrafficNodeSettings;

                if (customTrafficNodeSettings)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.newNodeSettingsType)));

                    switch (creator.newNodeSettingsType)
                    {
                        case RoadSegmentCreator.NewNodeSettingsType.Unique:
                            {
                                EditorGUILayout.PropertyField(configSo.FindProperty("newNodeSettings"));
                                break;
                            }
                        case RoadSegmentCreator.NewNodeSettingsType.CopySelected:
                            {
                                var maxIndex = creator.CreatedTrafficNodes.Count - 1;
                                creator.copySelectedIndex = EditorGUILayout.IntSlider("Copy Node Index", creator.copySelectedIndex, 0, maxIndex);
                                break;
                            }
                    }

                    if (GUILayout.Button("Open TrafficNode Editor"))
                    {
                        OpenTrafficNodeEditor();
                    }

                    if (GUILayout.Button("Add Traffic Node"))
                    {
                        creator.AddTrafficNode();
                    }
                }

                GUILayout.EndVertical();
            }

            if (!customTrafficNodeSettings)
            {
                DrawDefaultRoadSettings();

                DrawCustomRoadSettings();

                ShowCrosswalkSettings();
            }
            else
            {
                DrawCustomRoadSettings();
            }

            if (creator.GetRoadSegmentType == RoadSegmentCreator.RoadSegmentType.CustomSegment)
            {
                if (GUILayout.Button("Open TrafficNode Path Creator"))
                {
                    OpenPathCreator();
                }
            }
        }

        private void ShowRoadType()
        {
            EditorGUI.BeginChangeCheck();

            var previousType = creator.roadSegmentType;

            EditorGUILayout.PropertyField(roadSegmentType);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                var newType = (RoadSegmentCreator.RoadSegmentType)roadSegmentType.enumValueIndex;

                creator.OnSegmentTypeChanged(previousType, newType);
                InitCustomCreatorInspector();
            }
        }

        private void DrawDefaultRoadSettings()
        {
            if (customCreatorEditor != null)
                return;

            InspectorExtension.DrawGroupBox("Road Settings", () =>
            {
                EditorGUI.BeginChangeCheck();

                if (!creator.IsCustomStraight())
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.laneCount)));
                }
                else
                {
                    if (creator.CustomLaneTypeSupport)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.laneCountType)));

                        if (creator.laneCountType == Gameplay.Road.LaneCountType.Same)
                        {
                            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.laneCount)));
                        }
                        else
                        {
                            var forwardContent = new GUIContent("Forward Direction Lanes")
                            {
                                tooltip = "Lanes from TrafficNode1 to TrafficNode2"
                            };

                            var backwardContent = new GUIContent("Backward Direction Lanes")
                            {
                                tooltip = "Lanes from TrafficNode2 to TrafficNode1"
                            };

                            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.laneCount)), forwardContent);
                            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.backwardLaneCount)), backwardContent);
                        }
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.laneCount)));
                    }
                }

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.customLaneWidth)), new GUIContent("Lane Width"));

                EditorGUILayout.EndHorizontal();

                if (!creator.IsCustom())
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.crossroadWidth)), new GUIContent("CrossRoad Width"));

                    if (creator.turnCurveType == RoadSegmentCreator.TurnCurveType.BezierQuad)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.pathCorner1Offset)));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.pathCorner2Offset)));
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.pathCorner1Offset)), new GUIContent("Path Corner Offset"));
                    }
                }
                else
                {
                    if (!creator.IsStraightRoad() || !creator.oneWay)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.dividerWidth)));
                    }
                }

                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    creator.Recalculate(true);
                }

            }, ref segmentEditorSettings.RoadSettingsSubFoldOut);
        }

        private void DrawCustomRoadSettings()
        {
            if (customCreatorEditor != null)
            {
                InspectorExtension.DrawGroupBox("Custom Settings", () =>
                {
                    customCreatorEditor.Validate();
                    customCreatorEditor.DrawInspector();
                }, ref segmentEditorSettings.CustomSettingsSubFoldOut);

                return;
            }

            InspectorExtension.DrawGroupBox("Custom Settings", () =>
            {
                EditorGUI.BeginChangeCheck();

                if (creator.IsCrossRoad())
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.directionCount)));
                }

                if (creator.SubLaneSupport())
                {
                    if (creator.FlipSubLaneNodeAvailable)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("flipSubLaneNode"));
                    }

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("subLaneCount"), new GUIContent("Sub-Lane Count"));

                    var customSubSaneWidthProp = serializedObject.FindProperty("customSubLaneWidth");

                    EditorGUILayout.PropertyField(customSubSaneWidthProp);

                    if (customSubSaneWidthProp.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("subLaneWidth"));
                        EditorGUI.indentLevel--;
                    }
                }

                if (creator.roadSegmentType != RoadSegmentCreator.RoadSegmentType.CustomStraightRoad)
                {
                    if (creator.SubLaneTrafficNode())
                    {
                        creator.subTrafficNodeDistanceFromCenter = EditorGUILayout.Slider("SubTrafficNode Distance From Center", creator.subTrafficNodeDistanceFromCenter, 0f, 100f);
                    }

                    if ((creator.IsStraightRoad() || creator.IsTurnRoad()) && !creator.IsCustom())
                    {
                        creator.trafficNodeOffset1 = EditorGUILayout.Slider("Node 1 Offset", creator.trafficNodeOffset1, -100, 100f);
                        creator.trafficNodeOffset2 = EditorGUILayout.Slider("Node 2 Offset", creator.trafficNodeOffset2, -100, 100f);
                    }
                    else
                    {
                        creator.trafficNodeOffset1 = 0;
                        creator.trafficNodeOffset2 = 0;
                    }

                    if (creator.AdditionalLocalAngleSupported)
                    {
                        creator.additionalLocalAngle1 = EditorGUILayout.Slider("Additional Local Angle 1", creator.additionalLocalAngle1, -180, 180f);
                        creator.additionalLocalAngle2 = EditorGUILayout.Slider("Additional Local Angle 2", creator.additionalLocalAngle2, -180, 180f);
                    }
                }

                if (creator.IsStraightRoad() && !creator.IsCustom())
                {
                    creator.trafficNodeHeight1 = EditorGUILayout.Slider("Node 1 Height", creator.trafficNodeHeight1, -10f, 20f);
                    creator.trafficNodeHeight2 = EditorGUILayout.Slider("Node 2 Height", creator.trafficNodeHeight2, -10f, 20f);
                }

                if (creator.CheckMissing())
                {
                    var cleanupSelfIntersectionProp = serializedObject.FindProperty("cleanupSelfIntersection");

                    EditorGUILayout.PropertyField(cleanupSelfIntersectionProp);

                    if (cleanupSelfIntersectionProp.boolValue)
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("inverseCleanup"));
                }

                var subwayCount = creator.GetSubwayCount();

                if (subwayCount > 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("Is Enter Of Oneway");

                    for (int i = 0; i < subwayCount; i++)
                    {
                        EditorGUILayout.LabelField($"{i + 1}", GUILayout.Width(10f));
                        var newValue = EditorGUILayout.Toggle(creator.GetEnterFlagOfOneWay(i));
                        creator.SetEnterFlagOfOneWay(i, newValue);
                    }

                    EditorGUILayout.EndHorizontal();
                }

                if (creator.RevertDirectionSupport())
                {
                    creator.shouldRevertDirection = EditorGUILayout.Toggle("Should Revert Direction", creator.shouldRevertDirection);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    configSo.ApplyModifiedProperties();
                    creator.Create();
                }

                if (creator.GetRoadSegmentType == RoadSegmentCreator.RoadSegmentType.CustomStraightRoad)
                {
                    EditorGUI.BeginChangeCheck();

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.oneWay)));

                    if (creator.oneWay)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.shouldRevertDirection)));
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();
                        creator.Create();
                    }

                    ShowYAxisSettings();

                    var additionalSettings = Config.AdditionalSettings;

                    var nonDefaultSettings = additionalSettings != RoadSegmentCreator.AdditionalStraightRoadSettings.Default;

                    if (nonDefaultSettings)
                    {
                        EditorGUILayout.BeginVertical("GroupBox");
                    }

                    EditorGUILayout.PropertyField(configSo.FindProperty("additionalSettings"));

                    switch (additionalSettings)
                    {
                        case RoadSegmentCreator.AdditionalStraightRoadSettings.StripNodes:
                            {
                                EditorGUILayout.PropertyField(configSo.FindProperty("minStripAngle"));
                                EditorGUILayout.PropertyField(configSo.FindProperty("minStripDistance"));

                                if (GUILayout.Button("Strip Nodes"))
                                {
                                    creator.StripNodes();
                                }

                                break;
                            }

                        case RoadSegmentCreator.AdditionalStraightRoadSettings.GenerateSpawnNodes:
                            {
                                EditorGUILayout.PropertyField(configSo.FindProperty("minSpawnNodeOffset"));

                                if (GUILayout.Button("Clear Spawn Nodes"))
                                {
                                    creator.ClearPathInnerSpawnNodes();
                                }

                                if (GUILayout.Button("Generate Spawn Nodes"))
                                {
                                    creator.GeneratePathInnerSpawnNodes();
                                }

                                break;
                            }
                    }

                    if (nonDefaultSettings)
                    {
                        EditorGUILayout.EndVertical();
                    }
                }

                if (creator.GetRoadSegmentType == RoadSegmentCreator.RoadSegmentType.CustomSegment)
                {
                    EditorGUILayout.BeginVertical("GroupBox");

                    ShowYAxisSettings();

                    EditorGUILayout.EndVertical();

                    EditorGUILayout.BeginVertical("GroupBox");

                    ShowCustomAdditionalTab();

                    EditorGUILayout.EndVertical();
                }
            }, ref segmentEditorSettings.CustomSettingsSubFoldOut);
        }

        private void DrawCustomTurnSetting(RoadSegmentCreator.CustomTurnData customTurnData)
        {
            customTurnData.LeftTurnCount = EditorGUILayout.IntSlider("Left Turn Count", customTurnData.LeftTurnCount, 1, creator.laneCount);
            customTurnData.RightTurnCount = EditorGUILayout.IntSlider("Right Turn Count", customTurnData.RightTurnCount, 1, creator.laneCount);
            customTurnData.LaneLeftTurnConnectionCount = EditorGUILayout.IntSlider("Lane Left Turn Connection Count", customTurnData.LaneLeftTurnConnectionCount, 0, creator.laneCount);
            customTurnData.LaneRightTurnConnectionCount = EditorGUILayout.IntSlider("Lane Right Turn Connection Count", customTurnData.LaneRightTurnConnectionCount, 0, creator.laneCount);
        }

        private void ShowCrosswalkSettings()
        {
            InspectorExtension.DrawGroupBox("Pedestrian Node Settings", () =>
            {
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.addPedestrianNodes)));

                bool customCrosswalk = false;

                if (creator.addPedestrianNodes)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.pedestrianRouteWidth)));

                    if (creator.IsCustomStraight())
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.addAlongLine)));

                        if (creator.addAlongLine)
                        {
                            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.lineNodeOffset)));
                            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.nodeSpacing)));
                        }
                    }

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.customCrossWalkOffset)), new GUIContent("Cross Walk Offset"));

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.crossWalkNodeShape)));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.crosswalkNodeWidth)));

                    if (creator.crossWalkNodeShape == Gameplay.Road.NodeShapeType.Rectangle)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.crosswalkNodeHeight)));
                    }

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.customCrossWalk)));

                    if (creator.IsCustom(false))
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.connectCrosswalks)));
                    }

                    if (creator.customCrossWalk)
                    {
                        customCrosswalk = true;
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.PrefixLabel("Custom Pedestrian node");

                        int createdCount = creator.CreatedTrafficNodes.Count;

                        for (int i = 0; i < createdCount; i++)
                        {
                            var index = i + 1;
                            EditorGUILayout.LabelField(index.ToString(), GUILayout.Width(10f));

                            var realIndex = creator.GetInternalIndex(i);
                            var newVal = EditorGUILayout.Toggle(creator.GetCustomPedestrianNodeEnabledState(realIndex));
                            creator.SetPedestrianEnabledState(realIndex, newVal);
                        }

                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.PrefixLabel("Has Crosswalk");

                        for (int i = 0; i < createdCount; i++)
                        {
                            GUI.enabled = creator.customPedestrianNodesData[i];

                            var index = i + 1;
                            EditorGUILayout.LabelField(index.ToString(), GUILayout.Width(10f));

                            var realIndex = creator.GetInternalIndex(i);
                            var newVal = EditorGUILayout.Toggle(creator.GetCrosswalkEnabledState(realIndex, false));
                            creator.SetCrosswalkEnabledState(realIndex, newVal);

                            GUI.enabled = true;
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                    else
                    {
                        var sourceCrosswalk = creator.GetCrosswalkEnabledState(0, false);
                        var newCrosswalkValue = EditorGUILayout.Toggle("Has CrossWalk", sourceCrosswalk);

                        if (sourceCrosswalk != newCrosswalkValue)
                        {
                            creator.SetAllCrosswalkEnabled(newCrosswalkValue);
                        }
                    }

                    if (!creator.IsCustom() && !creator.IsStraightRoad())
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.pedestrianCornerConnectionType)));

                        if (creator.pedestrianCornerConnectionType == RoadSegmentCreator.PedestrianCornerConnectionType.Corner)
                        {
                            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.cornerOffset)));
                        }

                        if (creator.PedestrianLoopConnectionSupported)
                        {
                            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.loopPedestrianConnection)));
                        }
                    }
                }

                if (!customCrosswalk)
                {
                    creator.SetAllPedestrianEnabledState(creator.addPedestrianNodes);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    creator.OnCrosswalkSettingsChanged();

                    if (creator.addPedestrianNodes)
                    {
                        creator.OnLightSettingsChanged();
                    }
                }

            }, ref segmentEditorSettings.PedestrianNodeSubFoldOut);
        }
    }
}
#endif