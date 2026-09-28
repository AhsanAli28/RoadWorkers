#if UNITY_EDITOR
using Spirit604.Attributes;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using UnityEditor;
using UnityEngine;
using static Spirit604.CityEditor.Road.RoadSegmentCreator;

namespace Spirit604.CityEditor.Road
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(RoadSegmentCreator))]
    public partial class RoadSegmentCreatorEditor : Editor
    {
        private const string DocLink = "https://dotstrafficcity.readthedocs.io/en/latest/roadSegmentCreator.html";

        private RoadSegmentCreator creator;

        private SerializedProperty trafficNodeParent;
        private SerializedProperty cornerNodes;
        private SerializedProperty trafficLights;
        private SerializedProperty pedestrianLights;
        private SerializedProperty trafficLightCrossroad;

        private SerializedProperty roadSegmentType;

        private PathCreator pathCreator;
        private TrafficNodeWindowEditor trafficNodeWindowEditor;
        private PathSettingsWindowEditor pathSettingsWindow;
        private Path lastSelectedPath;

        private SerializedObject configSo;
        private GUIStyle trafficNodeGuiStyle = new GUIStyle();

        private GUIStyle gUIStyle = new GUIStyle();

        private RoadSegmentCreatorEditorSettings segmentEditorSettings;

        private ICustomCreatorEditor customCreatorEditor;

        private string[] SnapHeaders = new[] { "Node", "Surface", "Line" };
        private string[] Headers = new[] { "Common Settings", "Path Settings", "Snap Settings", "Light Settings", "Segment Settings", "Other Settings" };
        private Action[] ToolbarCallbacks;

        private RoadSegmentCreatorConfig Config => creator.roadSegmentCreatorConfig;

        private void OnEnable()
        {
            LoadPrefs();

            if (creator == null)
            {
                creator = target as RoadSegmentCreator;
                creator.OnInspectorRepaintRequested += RoadSegmentCreator_OnInspectorRepaintRequested;
            }

            ToolbarCallbacks = new Action[Headers.Length];
            ToolbarCallbacks[0] = () =>
            {
                this.ShowGeneralSettings(false);
            };
            ToolbarCallbacks[1] = () =>
            {
                this.ShowPathSettings();
            };
            ToolbarCallbacks[2] = () =>
            {
                switch (creator.GetRoadSegmentType)
                {
                    case RoadSegmentCreator.RoadSegmentType.CustomStraightRoad:
                        {
                            creator.snapToolbarIndex = GUILayout.Toolbar(creator.snapToolbarIndex, SnapHeaders);

                            GUILayout.BeginVertical("HelpBox");

                            switch (creator.snapToolbarIndex)
                            {
                                case 0:
                                    this.ShowSnapNodeSettings();
                                    break;
                                case 1:
                                    this.ShowSnapSurfaceSettings();
                                    break;
                                case 2:
                                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.angleThreshold)));
                                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.minWaypointOffset)));

                                    ConfigPropertyField("debugCast");
                                    ConfigPropertyField("snapLayerMask");
                                    ConfigPropertyField("snapSurfaceOffset");

                                    if (GUILayout.Button("Align Path"))
                                    {
                                        creator.AlignCustomPath();
                                    }
                                    break;
                            }

                            GUILayout.EndVertical();

                            break;
                        }
                    case RoadSegmentCreator.RoadSegmentType.CustomSegment:
                        {
                            this.ShowSnapNodeSettings();
                            break;
                        }
                }
            };
            ToolbarCallbacks[3] = () =>
            {
                this.ShowLightSettings();
            };
            ToolbarCallbacks[4] = () =>
            {
                this.ShowSegmentHandler();
            };
            ToolbarCallbacks[5] = () =>
            {
                this.ShowOtherSettings();
            };

            trafficNodeGuiStyle = new GUIStyle();
            trafficNodeGuiStyle.normal.textColor = Color.white;
            trafficNodeGuiStyle.fontSize = 24;

            gUIStyle.fontSize = 28;
            gUIStyle.normal.textColor = Color.white;

            trafficNodeParent = serializedObject.FindProperty("trafficNodeParent");
            cornerNodes = serializedObject.FindProperty("cornerNodes");
            trafficLights = serializedObject.FindProperty("trafficLights");
            pedestrianLights = serializedObject.FindProperty("pedestrianLights");
            trafficLightCrossroad = serializedObject.FindProperty("trafficLightCrossroad");

            roadSegmentType = serializedObject.FindProperty("roadSegmentType");

            if (Config != null)
            {
                configSo = new SerializedObject(Config);
            }

            creator.OnInspectorEnabled();
            creator.OnTrafficNodeAdd += Creator_OnTrafficNodeAdd;
            creator.OnTrafficNodeRemove += Creator_OnTrafficNodeRemove;
            creator.OnPathSelectionChangedEvent += Creator_OnPathSelectionChangedEvent;
            InitCustomCreatorInspector();

            Undo.undoRedoPerformed += Undo_undoRedoPerformed;
            ObjectChangeEvents.changesPublished += ObjectChangeEvents_ChangesPublished;
        }

        private void OnDisable()
        {
            creator = target as RoadSegmentCreator;
            SavePrefs();
            OnInspectorDisabledInternal();
            creator.OnInspectorDisabled();
            creator.OnTrafficNodeAdd -= Creator_OnTrafficNodeAdd;
            creator.OnTrafficNodeRemove -= Creator_OnTrafficNodeRemove;
            creator.OnPathSelectionChangedEvent -= Creator_OnPathSelectionChangedEvent;
            sceneTrafficNodes = null;

            Undo.undoRedoPerformed -= Undo_undoRedoPerformed;
            ObjectChangeEvents.changesPublished -= ObjectChangeEvents_ChangesPublished;
        }

        public override void OnInspectorGUI()
        {
            if (Selection.objects.Length > 1)
            {
                EditorGUILayout.HelpBox("Multi-editing not available.", MessageType.Info);
                return;
            }

            serializedObject.Update();

            DocumentationLinkerUtils.ShowButtonFirst(DocLink, -4);

            Action prefabContent = () =>
            {
                EditorGUILayout.PropertyField(trafficNodeParent);
                EditorGUILayout.PropertyField(cornerNodes);
                EditorGUILayout.PropertyField(trafficLights);
                EditorGUILayout.PropertyField(pedestrianLights);
                EditorGUILayout.PropertyField(trafficLightCrossroad);

                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.roadSegmentCreatorConfig)));

                if (EditorGUI.EndChangeCheck())
                {
                    ApplySerializedObject();
                    ConfigChanged();
                }

                EditorGUILayout.Separator();
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Prefabs", prefabContent, ref segmentEditorSettings.PrefabsFoldOut);

            if (Config == null)
            {
                EditorGUILayout.HelpBox("Assign RoadSegmentCreatorConfig config!", MessageType.Error);
                ApplySerializedObject();
                return;
            }

            if (configSo == null)
            {
                ConfigChanged();
            }

            configSo.Update();

            var viewType = Config.ViewType;

            switch (viewType)
            {
                case RoadSegmentCreator.ViewType.Toolbar:
                    ShowToolbar();
                    break;
                case RoadSegmentCreator.ViewType.Tabs:
                    ShowTabs();
                    break;
            }

            ApplySerializedObject();

            configSo.ApplyModifiedProperties();
        }

        private void ShowToolbar()
        {
            GUILayout.BeginVertical("GroupBox");

            ShowRoadType();

            GUILayout.EndVertical();

            GUILayout.BeginVertical("GroupBox");

            creator.selectedTabIndex = GUILayout.SelectionGrid(creator.selectedTabIndex, Headers, 3);

            GUILayout.BeginHorizontal();

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.BeginVertical("GroupBox");

            ToolbarCallbacks[creator.selectedTabIndex]?.Invoke();

            GUILayout.EndVertical();

            if (creator.ParkingBuilderModeSupported)
            {
                DrawParkingBuilderSettingsTab();
            }
        }

        private void ShowTabs()
        {
            Action generalSettingsContent = () =>
            {
                ShowGeneralSettings();
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("General Settings", generalSettingsContent, ref segmentEditorSettings.GeneralSettingsFoldOut);

            if (creator.roadSegmentType != RoadSegmentCreator.RoadSegmentType.CustomStraightRoad)
            {
                if (creator.IsCustom())
                {
                    if (creator.ParkingBuilderModeSupported)
                    {
                        DrawParkingBuilderSettingsTab();
                    }

                    ShowNodeSnapSettingsTab();
                }

                ShowLightSettingsTab();

                Action pathSettingsContent = () =>
                {
                    ShowPathSettings();
                };

                InspectorExtension.DrawDefaultInspectorGroupBlock("Path Settings", pathSettingsContent, ref segmentEditorSettings.PathSettingsFoldOut);

                ShowSegmentHandlerTab();
            }
            else
            {
                ShowCustomStraightTabs();
            }

            ShowOtherSettingsTab();
        }

        private void ShowCustomStraightTabs()
        {
            ShowNodeSnapSettingsTab();

            Action snapSurfaceSettingContent = () =>
            {
                ShowSnapSurfaceSettings();
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Snap Surface Settings", snapSurfaceSettingContent, ref segmentEditorSettings.SnapSurfaceSettingsFoldOut);

            Action pathSettingContent = () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.showEditButtonsPathNodes)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.showTrafficNodeHandles)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.showTrafficNodeForward)));

                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.straightRoadPathSpeedLimit)), new GUIContent("Speed Limit"));

                if (EditorGUI.EndChangeCheck())
                {
                    creator.OnStraightSpeedLimitChanged();
                }
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Path Settings", pathSettingContent, ref segmentEditorSettings.PathSettingsFoldOut);

            ShowSegmentHandlerTab();
        }

        private void ShowSnapSurfaceSettings()
        {
            EditorGUILayout.PropertyField(configSo.FindProperty("snapSurfaceOffset"));
            EditorGUILayout.PropertyField(configSo.FindProperty("snapLayerMask"));

            creator.straightRoadSelectedNodeIndex = GUILayout.Toolbar(creator.straightRoadSelectedNodeIndex, creator.customStraightRoadNodesNames);

            if (GUILayout.Button("Snap To Surface"))
            {
                creator.SnapToSurfaceCustomPath();
            }
        }

        private void ShowLightSettingsTab()
        {
            Action lightSettingsContent = () =>
            {
                ShowLightSettings();
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Light Settings", lightSettingsContent, ref segmentEditorSettings.LightSettingsFoldOut);
        }

        private void DrawLightSettings(RoadSegmentCreator.LightObjectData lightObjectData, bool showEnabledOption = false, bool sharedSettings = false)
        {
            EditorGUI.BeginChangeCheck();

            if (showEnabledOption)
            {
                lightObjectData.Enabled = EditorGUILayout.Toggle("Enabled", lightObjectData.Enabled);
            }

            GUI.enabled = lightObjectData.Enabled;

            var newSelectedLightPrefabType = (LightPrefabType)EditorGUILayout.EnumPopup("Selected Light Prefab Type", lightObjectData.SelectedLightPrefabType);

            if (lightObjectData.SelectedLightPrefabType != newSelectedLightPrefabType)
            {
                lightObjectData.SelectedLightPrefabType = newSelectedLightPrefabType;

                if (sharedSettings)
                {
                    foreach (var item in creator.lightObjectDatas)
                    {
                        item.SelectedLightPrefabType = newSelectedLightPrefabType;
                    }
                }
            }

            var newLightLocation = (RoadSegmentCreator.LightLocation)EditorGUILayout.EnumPopup("Light Location", lightObjectData.LightLocation);

            if (lightObjectData.LightLocation != newLightLocation)
            {
                lightObjectData.LightLocation = newLightLocation;

                if (sharedSettings)
                {
                    foreach (var item in creator.lightObjectDatas)
                    {
                        item.LightLocation = newLightLocation;
                    }
                }
            }

            lightObjectData.TrafficLightOffset = EditorGUILayout.Vector3Field("Traffic Light Offset", lightObjectData.TrafficLightOffset);

            if (lightObjectData.Enabled && !sharedSettings)
            {
                segmentEditorSettings.LightAngleOffsetSettingsFoldOut = EditorGUILayout.Foldout(segmentEditorSettings.LightAngleOffsetSettingsFoldOut, "Light AngleOffset settings");

                if (segmentEditorSettings.LightAngleOffsetSettingsFoldOut)
                {
                    DrawAngleOffsets(lightObjectData);
                }
            }

            GUI.enabled = true;

            if (EditorGUI.EndChangeCheck())
            {
                ApplySerializedObject();
                creator.OnLightSettingsChanged();
            }
        }

        private void DrawAngleOffsets(LightObjectData lightObjectData)
        {
            float originalValue = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 20f;

            for (int i = 0; i < lightObjectData.LocalLightCount; i++)
            {
                if (lightObjectData.AngleOffsets.Count <= i)
                {
                    continue;
                }

                EditorGUILayout.BeginHorizontal();

                int index = i + 1;
                EditorGUILayout.LabelField(index.ToString(), GUILayout.Width(10f));

                lightObjectData.AngleOffsets[i] = EditorGUILayout.Slider(lightObjectData.AngleOffsets[i], 0, 360, GUILayout.ExpandWidth(true));

                EditorGUILayout.LabelField("Flip Index", GUILayout.Width(55f));

                lightObjectData.FlipAngleOffsets[i] = EditorGUILayout.Toggle(lightObjectData.FlipAngleOffsets[i], GUILayout.MaxWidth(15f));

                EditorGUILayout.EndHorizontal();
            }

            EditorGUIUtility.labelWidth = originalValue;
        }

        private void DrawPedestrianLightSettings(RoadSegmentCreator.LightObjectData lightObjectData)
        {
            EditorGUI.BeginChangeCheck();

            lightObjectData.PedestrianLightOffset = EditorGUILayout.Vector3Field("Pedestrian Light Offset", lightObjectData.PedestrianLightOffset);
            lightObjectData.PedestrianAngleOffset = EditorGUILayout.IntSlider("Pedestrian Angle Offset", lightObjectData.PedestrianAngleOffset, 0, 360);

            if (EditorGUI.EndChangeCheck())
            {
                ApplySerializedObject();
                creator.OnLightSettingsChanged();
            }
        }

        private void ShowLightSettings()
        {
            EditorGUILayout.BeginVertical("GroupBox");

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.showLightIndexes)));

            if (creator.addTrafficLights || creator.addPedestrianNodes)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.minTrafficNodesCountForAddLight)));
            }

            EditorGUILayout.EndVertical();

            var lightObjectData = creator.GetLightSettings(creator.selectedLightNodeIndex);

            EditorGUILayout.BeginVertical("GroupBox");

            if (creator.TrafficLightPlacingSupported)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.addTrafficLights)));

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.lightHandleType)));

                if (creator.lightHandleType != HandleType.None)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.lightType)));
                }

                if (creator.lightHandleType == HandleType.Position)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.lightSnapPosition)), new GUIContent("Snap Position"));

                    if (creator.lightSnapPosition)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.lightAddHalfOffset)), new GUIContent("Add Half Offset"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.lightSnapCustomSize)), new GUIContent("Snap Value"));
                    }
                }

                if (creator.lightHandleType == HandleType.Rotation)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.lightAutoRoundRotation)), new GUIContent("Auto Round Rotation"));

                    if (creator.lightAutoRoundRotation)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.lightRoundAngle)), new GUIContent("Round Angle"));
                    }
                }

                if (creator.addTrafficLights)
                {
                    if (creator.trafficNodeHeaders?.Length > 1)
                    {
                        var headerCount = creator.trafficNodeHeaders.Length - 1;

                        if (headerCount != creator.CreatedTrafficNodes.Count)
                        {
                            creator.InitializeTrafficNodeHeaders();
                        }

                        creator.selectedLightNodeIndex = GUILayout.SelectionGrid(creator.selectedLightNodeIndex + 1, creator.trafficNodeHeaders, maxRowSelectionGridCount) - 1;
                    }

                    var commonSettings = creator.selectedLightNodeIndex == -1;

                    DrawLightSettings(lightObjectData, !commonSettings, commonSettings);
                    EditorGUILayout.Separator();
                }
            }
            else
            {
                GUI.enabled = false;
                EditorGUILayout.Toggle("Add Traffic Lights", false);
                GUI.enabled = true;
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("GroupBox");

            if (creator.addPedestrianNodes)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.addPedestrianLights)));

                if (creator.addPedestrianLights)
                {
                    DrawPedestrianLightSettings(lightObjectData);
                }
            }
            else
            {
                GUI.enabled = false;
                EditorGUILayout.Toggle("Add Pedestrian Lights [crosswalk disabled]", false);
                GUI.enabled = true;
            }

            if (EditorGUI.EndChangeCheck())
            {
                ApplySerializedObject();
                creator.OnLightSettingsChanged();
            }

            ApplySerializedObject();

            EditorGUILayout.EndVertical();
        }

        private void ShowSegmentHandlerTab()
        {
            Action segmentHandlerContent = () =>
            {
                ShowSegmentHandler();
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Segment Handler Settings", segmentHandlerContent, ref segmentEditorSettings.SegmentHandlerSettingsFoldOut);
        }

        private void ShowSegmentHandler()
        {
            if (configSo == null)
            {
                return;
            }

            var showSegmentPositionHandleProp = configSo.FindProperty("showSegmentPositionHandle");
            EditorGUILayout.PropertyField(showSegmentPositionHandleProp);

            if (showSegmentPositionHandleProp.boolValue)
            {
                EditorGUILayout.PropertyField(configSo.FindProperty("autoRecalculateExternalPaths"));

                var snapSegmentPositionProp = configSo.FindProperty("snapSegmentPosition");
                EditorGUILayout.PropertyField(snapSegmentPositionProp);

                if (snapSegmentPositionProp.boolValue)
                {
                    EditorGUILayout.PropertyField(configSo.FindProperty("addHalfOffset"));

                    EditorGUILayout.PropertyField(configSo.FindProperty("customSnapSize"));
                }
            }

            EditorGUILayout.PropertyField(configSo.FindProperty("snapSurfaceOffset"));
            EditorGUILayout.PropertyField(configSo.FindProperty("snapLayerMask"));

            if (GUILayout.Button("Snap Segment To Surface"))
            {
                creator.SnapSegment();
            }
        }

        private void ShowOtherSettingsTab()
        {
            Action otherSettingsContent = () =>
            {
                ShowOtherSettings();
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Other Settings", otherSettingsContent, ref segmentEditorSettings.OtherSettingsFoldOut);
        }

        private void ShowOtherSettings()
        {
            GUILayout.BeginVertical("GroupBox");

            if (GUILayout.Button("Merge Segment"))
            {
                MergeSegmentWindow.ShowWindow();
            }

            GUI.enabled = !creator.IsCustom(false);

            if (GUILayout.Button("Convert To Custom"))
            {
                creator.ConvertToCustom();
                InitCustomCreatorInspector();
            }

            GUI.enabled = true;

            GUILayout.EndVertical();

            GUILayout.BeginVertical("GroupBox");

            EditorGUILayout.BeginHorizontal();

            var savePathProp = configSo.FindProperty("savePrefabPath");

            if (savePathProp != null)
            {
                var width = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 120f;
                EditorGUILayout.PropertyField(savePathProp);
                EditorGUIUtility.labelWidth = width;

                if (GUILayout.Button("Open", GUILayout.Width(50f)))
                {
                    AssetDatabaseExtension.SelectProjectFolder(savePathProp.stringValue);
                }

                if (GUILayout.Button("+", GUILayout.Width(25f)))
                {
                    AssetDatabaseExtension.SelectProjectFolderFromDialogWindow("Select new prefab path", ref savePathProp, savePathProp.stringValue);
                }
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Save To Prefab"))
            {
                creator.SaveToPrefab();
            }

            GUILayout.EndVertical();

            ShowDefaultButtons();
        }

        private void ShowYAxisSettings()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.lockYAxisMove)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.showYPosition)));

            if (!creator.lockYAxisMove)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.roundYPosition)));

                if (creator.roundYPosition)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.roundYValue)));
                }
            }
        }

        private void ShowNodeSnapSettingsTab()
        {
            Action snapNodeSettingsContent = () =>
            {
                ShowSnapNodeSettings();
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Snap Node Settings", snapNodeSettingsContent, ref segmentEditorSettings.SnapNodeSettingsFoldOut);
        }

        private void ShowSnapNodeSettings()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.snapObjectType)));

            var autoSnapPositionProp = serializedObject.FindProperty(nameof(creator.autoSnapPosition));
            EditorGUILayout.PropertyField(autoSnapPositionProp, new GUIContent("Auto-snap position"));

            if (autoSnapPositionProp.boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.addHalfOffset)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.autoSnapCustomSize)));
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.autoRoundRotation)), new GUIContent("Auto-round rotation"));

            if (creator.autoRoundRotation)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.roundAngle)));
            }

            if (creator.GetRoadSegmentType == RoadSegmentCreator.RoadSegmentType.CustomSegment)
            {
                ConfigPropertyField("snapSurfaceOffset");

                if (GUILayout.Button("Snap To Surface"))
                {
                    creator.SnapNodes();
                }
            }
        }

        private void ShowDefaultButtons()
        {
            GUILayout.BeginVertical("GroupBox");

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Rotate -90°"))
            {
                creator.Rotate(-90);
            }
            if (GUILayout.Button("Rotate 90°"))
            {
                creator.Rotate(90);
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Recreate"))
            {
                creator.Create();
            }

            if (GUILayout.Button("Clear"))
            {
                creator.Clear();
            }

            GUILayout.EndVertical();
        }

        private void ConfigPropertyField(string fieldName)
        {
            if (configSo == null)
            {
                return;
            }

            EditorGUILayout.PropertyField(configSo.FindProperty(fieldName));
        }

        private void ConfigChanged()
        {
            configSo = null;

            if (Config != null)
            {
                configSo = new SerializedObject(Config);
            }

            creator.OnConfigChanged();
        }

        private void LoadPrefs()
        {
            var settingsJson = EditorPrefs.GetString(nameof(segmentEditorSettings));

            if (string.IsNullOrEmpty(settingsJson))
            {
                segmentEditorSettings = RoadSegmentCreatorEditorSettings.GetDefault();
            }
            else
            {
                segmentEditorSettings = JsonUtility.FromJson<RoadSegmentCreatorEditorSettings>(settingsJson);
            }
        }

        private void SavePrefs()
        {
            EditorPrefs.SetString(nameof(segmentEditorSettings), JsonUtility.ToJson(segmentEditorSettings));
        }

        private void UnselectPaths()
        {
            creator.IterateAllNodes(node =>
            {
                TrafficNodeEditorExtension.SwitchSelectionState(node, null, false, TrafficNodeDirectionType.RightAndLeft);
            });
        }

        private void ApplySerializedObject()
        {
            try
            {
                if (serializedObject != null && serializedObject.targetObject != null)
                    serializedObject.ApplyModifiedProperties();
            }
            catch { }
        }

        private void InitCustomCreatorInspector()
        {
            customCreatorEditor = null;

            switch (creator.GetRoadSegmentType)
            {
                case RoadSegmentCreator.RoadSegmentType.Roundabout:
                    customCreatorEditor = new RoundaboutEditorWrapper(creator.gameObject.GetOrCreateComponent<Roundabout>(), creator);
                    break;
            }
        }

        private void IterateAllSelected(Action<RoadSegmentCreator> action)
        {
            foreach (var go in Selection.gameObjects)
            {
                if (go.TryGetComponent<RoadSegmentCreator>(out var creator))
                {
                    action(creator);
                }
            }
        }

        public void OnInspectorDisabledInternal()
        {
            UnselectPaths();
        }

        private void Creator_OnTrafficNodeAdd(TrafficNode trafficNode)
        {
            trafficNodeWindowEditor?.Initialize(creator, creator.CreatedTrafficNodes.ToArray(), creator.CrossWalkOffset);
        }

        private void Creator_OnTrafficNodeRemove(TrafficNode trafficNode)
        {
            trafficNodeWindowEditor?.Repaint();
        }

        private void Creator_OnPathSelectionChangedEvent(Path newSelectedPath)
        {
            if (pathSettingsWindow != null)
            {
                pathSettingsWindow.Initialize(newSelectedPath);
            }
        }

        private void Undo_undoRedoPerformed()
        {
            IterateAllSelected(creator =>
            {
                creator.UndoClicked();
                creator.DirtyAllPaths();
            });

            InitCustomCreatorInspector();
        }

        private void RoadSegmentCreator_OnInspectorRepaintRequested()
        {
            Repaint();
        }

        private static void ObjectChangeEvents_ChangesPublished(ref ObjectChangeEventStream stream)
        {
            for (int i = 0; i < stream.length; ++i)
            {
                var type = stream.GetEventType(i);

                switch (type)
                {
                    case ObjectChangeKind.CreateGameObjectHierarchy:
                        {
                            stream.GetCreateGameObjectHierarchyEvent(i, out var createGameObjectHierarchyEvent);
                            var newGameObject = EditorUtility.InstanceIDToObject(createGameObjectHierarchyEvent.instanceId) as GameObject;

                            if (newGameObject)
                            {
                                var creator = newGameObject.GetComponentInChildren<RoadSegmentCreator>();

                                if (creator != null)
                                {
                                    creator.ProcessDuplicate();
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