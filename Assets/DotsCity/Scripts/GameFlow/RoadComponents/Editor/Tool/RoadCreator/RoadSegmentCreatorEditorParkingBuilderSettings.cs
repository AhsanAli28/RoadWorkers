#if UNITY_EDITOR
using Spirit604.Attributes;
using Spirit604.Extensions;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Spirit604.CityEditor.Road.RoadSegmentCreator;

namespace Spirit604.CityEditor.Road
{
    public partial class RoadSegmentCreatorEditor : Editor
    {
        private const string ParkingDoc = "https://dotstrafficcity.readthedocs.io/en/latest/roadSegmentCreator.html#roadsegmentcreatorparkingbuilder";

        private readonly string[] parkingHeaders = new string[] { "None", "Enter", "Exit" };
        private readonly string[] settingsHeaders = new string[] { "Common", "Path", "Node", "Pedestrian" };
        private readonly string[] handleHeaders = new string[] { "None", "Position", "Rotation" };
        private readonly string[] handleTypesHeaders = new string[] { "None", "Handles", "Offsets" };
        private string[] pathOffsetHeaders;

        private const float RemoveLineButtonSize = 15f;

        private void DrawParkingBuilderSettingsTab()
        {
            InspectorExtension.DrawDefaultInspectorGroupBlock("Parking Builder", () =>
            {
                DocumentationLinkerUtils.ShowButtonFirst(ParkingDoc);

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.parkingBuilderMode)));

                if (creator.parkingBuilderMode)
                {
                    EditorGUI.BeginChangeCheck();

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.parkingConfigType)));

                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();
                        creator.ParkingConfigChanged();
                    }

                    var parkingConfig = creator.CurrentParkingLineSettings;

                    switch (creator.parkingConfigType)
                    {
                        case RoadSegmentCreator.ParkingConfigType.Temp:
                            break;
                        case RoadSegmentCreator.ParkingConfigType.Selected:
                            {
                                EditorGUI.BeginChangeCheck();

                                Config.SelectedParkingPresetIndex = EditorGUILayout.Popup("Available configs", Config.SelectedParkingPresetIndex, creator.parkingConfigNames);

                                if (EditorGUI.EndChangeCheck())
                                {
                                    creator.ParkingConfigChanged();
                                }

                                break;
                            }
                    }

                    if (parkingConfig == null)
                    {
                        EditorGUILayout.HelpBox("Configs not found. Make sure that at least one config has been created.", MessageType.Info);
                        return;
                    }

                    var parkingConfigSo = new SerializedObject(parkingConfig);
                    parkingConfigSo.Update();

                    var containerProp = parkingConfigSo.FindProperty("parkingLineSettings");

                    creator.parkingSettingsTabIndex = GUILayout.Toolbar(creator.parkingSettingsTabIndex, settingsHeaders);

                    EditorGUILayout.BeginVertical("GroupBox");

                    switch (creator.parkingSettingsTabIndex)
                    {
                        case 0: // Common
                            {
                                EditorGUI.BeginChangeCheck();

                                EditorGUILayout.PropertyField(containerProp.FindPropertyRelative("placeCount"));

                                if (EditorGUI.EndChangeCheck())
                                {
                                    containerProp.serializedObject.ApplyModifiedProperties();
                                    creator.PlaceCountChanged();
                                }

                                EditorGUILayout.PropertyField(containerProp.FindPropertyRelative("parkingPlaceSpacingOffset"));
                                EditorGUILayout.PropertyField(containerProp.FindPropertyRelative("lineHandleObjectType"));

                                var parkingLineHandleTypeProp = containerProp.FindPropertyRelative("parkingLineHandleType");

                                creator.parkingHandleTabIndex = GUILayout.Toolbar(creator.parkingHandleTabIndex, handleHeaders);

                                var parkingLineHandleType = (HandleType)creator.parkingHandleTabIndex;

                                if (parkingLineHandleTypeProp.enumValueIndex != creator.parkingHandleTabIndex)
                                {
                                    parkingLineHandleTypeProp.enumValueIndex = creator.parkingHandleTabIndex;
                                }

                                switch (parkingLineHandleType)
                                {
                                    case HandleType.None:
                                        break;
                                    case HandleType.Position:
                                        {
                                            EditorGUILayout.PropertyField(containerProp.FindPropertyRelative("positionSnapType"));

                                            var positionSnapType = creator.CurrentParkingLineSettings.ParkingLineSettings.PositionSnapType;

                                            switch (positionSnapType)
                                            {
                                                case ParkingPositionSnapType.Disabled:
                                                    break;
                                                case ParkingPositionSnapType.Custom:
                                                    {
                                                        EditorGUI.indentLevel++;
                                                        EditorGUILayout.PropertyField(containerProp.FindPropertyRelative("positionSnap"));
                                                        EditorGUILayout.PropertyField(containerProp.FindPropertyRelative("snapOffset"));
                                                        EditorGUI.indentLevel--;
                                                        break;
                                                    }
                                            }

                                            break;
                                        }
                                    case HandleType.Rotation:
                                        {
                                            var rotationSnapTypeProp = containerProp.FindPropertyRelative("rotationSnapType");

                                            EditorGUILayout.PropertyField(rotationSnapTypeProp);

                                            var rotationSnapType = creator.CurrentParkingLineSettings.ParkingLineSettings.RotationSnapType;

                                            switch (rotationSnapType)
                                            {
                                                case ParkingRotationSnapType.Disabled:
                                                    break;
                                                case ParkingRotationSnapType.RightCorner:
                                                    break;
                                                case ParkingRotationSnapType.Custom:
                                                    {
                                                        EditorGUI.indentLevel++;
                                                        EditorGUILayout.PropertyField(containerProp.FindPropertyRelative("rotationSnapAngle"));
                                                        EditorGUI.indentLevel--;
                                                        break;
                                                    }
                                            }

                                            break;
                                        }
                                }

                                EditorGUI.BeginChangeCheck();

                                EditorGUILayout.PropertyField(containerProp.FindPropertyRelative("lineStartPointLocal"));

                                if (EditorGUI.EndChangeCheck())
                                {
                                    parkingConfigSo.ApplyModifiedProperties();
                                    creator.UpdateTempParkingPointPosition(true);
                                }

                                EditorGUILayout.PropertyField(containerProp.FindPropertyRelative("placeSize"));
                                EditorGUILayout.PropertyField(containerProp.FindPropertyRelative("nodeDirection"));
                                EditorGUILayout.PropertyField(containerProp.FindPropertyRelative("lineDirection"));

                                if (GUILayout.Button("Reset Start Point"))
                                {
                                    creator.tempStartParkingPoint.position = VectorExtensions.GetCenterOfSceneView();
                                }

                                break;
                            }
                        case 1: // Path
                            {
                                EditorGUILayout.BeginVertical("GroupBox");

                                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.parkingConnectionSourceType)));

                                switch (creator.parkingConnectionSourceType)
                                {
                                    case RoadSegmentCreator.ParkingConnectionSourceType.Path:
                                        {
                                            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.parkingSourcePath)));
                                            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.showSelectPathButtons)));
                                            break;
                                        }
                                    case RoadSegmentCreator.ParkingConnectionSourceType.Node:
                                        {
                                            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.sourceTrafficNode)));
                                            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.targetTrafficNode)));
                                            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.connectionLaneIndex)));
                                            break;
                                        }
                                    case RoadSegmentCreator.ParkingConnectionSourceType.SingleNode:
                                        {
                                            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.sourceTrafficNode)));
                                            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.connectionLaneIndex)));
                                            break;
                                        }
                                }

                                EditorGUILayout.EndVertical();

                                EditorGUILayout.BeginVertical("GroupBox");

                                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.autoRecalculateParkingPaths)));

                                EditorGUI.BeginChangeCheck();

                                EditorGUILayout.PropertyField(containerProp.FindPropertyRelative("railType"));

                                if (EditorGUI.EndChangeCheck())
                                {
                                    containerProp.serializedObject.ApplyModifiedProperties();
                                    creator.RailChanged();
                                }

                                EditorGUI.BeginChangeCheck();

                                EditorGUILayout.PropertyField(containerProp.FindPropertyRelative("trafficGroupMask"));

                                if (EditorGUI.EndChangeCheck())
                                {
                                    containerProp.serializedObject.ApplyModifiedProperties();
                                    creator.TrafficMaskChanged();
                                }

                                EditorGUILayout.EndVertical();

                                EditorGUILayout.BeginVertical("GroupBox");

                                GUI.enabled = creator.handleTypeTabIndex == 1;

                                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.showEditPathParkingButtons)));

                                GUI.enabled = true;

                                EditorGUI.BeginChangeCheck();

                                creator.handleTypeTabIndex = GUILayout.Toolbar(creator.handleTypeTabIndex, handleTypesHeaders);

                                if (EditorGUI.EndChangeCheck())
                                {
                                    SceneView.RepaintAll();
                                }

                                EditorGUI.BeginChangeCheck();

                                creator.selectedPathToolbarOption = GUILayout.Toolbar(creator.selectedPathToolbarOption, parkingHeaders);

                                if (EditorGUI.EndChangeCheck())
                                {
                                    SceneView.RepaintAll();
                                }

                                if (creator.selectedPathToolbarOption != 0)
                                {
                                    GUI.enabled = creator.HasParkingConnectionData;

                                    int pathIndex = creator.selectedPathToolbarOption - 1;

                                    if (!creator.HasTempPath(pathIndex) || !creator.ShowPathParkingOffsetHandles)
                                    {
                                        GUILayout.BeginHorizontal();

                                        if (GUILayout.Button("Create"))
                                        {
                                            creator.CreateTempParkingPaths(parkingConfig, creator.selectedPathToolbarOption, true);
                                        }

                                        if (creator.HasTempPath(pathIndex))
                                        {
                                            if (GUILayout.Button("Settings"))
                                            {
                                                OpenPathSettings(creator.tempCustomPaths[pathIndex], true);
                                            }

                                            if (GUILayout.Button("Destroy"))
                                            {
                                                creator.DestroyTempPath(pathIndex);
                                            }
                                        }

                                        GUILayout.EndHorizontal();
                                    }

                                    if (creator.HasTempPath(pathIndex))
                                    {
                                        if (creator.ShowPathParkingOffsetHandles)
                                        {
                                            EditorGUILayout.BeginVertical("GroupBox");

                                            CheckForOffsetHeader();

                                            EditorGUI.BeginChangeCheck();

                                            creator.selectedParkingOffsetPathIndex = GUILayout.SelectionGrid(creator.selectedParkingOffsetPathIndex, pathOffsetHeaders, 4);

                                            if (EditorGUI.EndChangeCheck())
                                            {
                                                SceneView.RepaintAll();
                                            }

                                            if (GUILayout.Button("Reset Offset"))
                                            {
                                                creator.ResetOffsets(pathIndex, true);
                                            }

                                            EditorGUILayout.EndVertical();
                                        }
                                    }

                                    EditorGUILayout.EndVertical();

                                    EditorGUILayout.BeginVertical("GroupBox");

                                    EditorGUI.BeginChangeCheck();

                                    EditorGUILayout.PropertyField(containerProp.FindPropertyRelative("initialPathSpeedLimit"));

                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        containerProp.serializedObject.ApplyModifiedProperties();
                                        creator.ParkingSpeedLimitChanged(creator.selectedPathToolbarOption - 1, true);
                                    }

                                    GUI.enabled = true;

                                    switch (creator.selectedPathToolbarOption)
                                    {
                                        case 1:
                                            {
                                                EditorGUILayout.PropertyField(containerProp.FindPropertyRelative("nodeCloneCount"));
                                                break;
                                            }
                                        case 2:
                                            {
                                                EditorGUILayout.PropertyField(containerProp.FindPropertyRelative("nodeSkipLastCount"));
                                                break;
                                            }
                                    }

                                    EditorGUILayout.EndVertical();
                                }
                                else
                                {
                                    EditorGUILayout.EndVertical();
                                }

                                break;
                            }
                        case 2: // TrafficNode
                            {
                                EditorGUILayout.PropertyField(containerProp.FindPropertyRelative("placeTrafficNodeType"));
                                EditorGUILayout.PropertyField(containerProp.FindPropertyRelative("parkingTrafficNodeWeight"));
                                EditorGUILayout.PropertyField(containerProp.FindPropertyRelative("nodeCustomAchieveDistance"));
                                break;
                            }
                        case 3: // PedestrianNode
                            {
                                EditorGUILayout.PropertyField(containerProp.FindPropertyRelative("addParkingPedestrianNodes"));

                                if (parkingConfig.AddParkingPedestrianNodes)
                                {
                                    EditorGUILayout.PropertyField(containerProp.FindPropertyRelative("parkingPedestrianNodeType"));
                                    EditorGUILayout.PropertyField(containerProp.FindPropertyRelative("autoConnectNodes"));
                                    EditorGUILayout.PropertyField(containerProp.FindPropertyRelative("parkingPedestrianNodeWeight"));
                                    EditorGUILayout.PropertyField(containerProp.FindPropertyRelative("parkingNodeOffset"));
                                    EditorGUILayout.PropertyField(containerProp.FindPropertyRelative("parkingEnterNodeOffset"));
                                }

                                break;
                            }
                    }

                    EditorGUILayout.EndVertical();

                    if (creator.parkingConfigType == RoadSegmentCreator.ParkingConfigType.Temp)
                    {
                        if (creator.parkingSettingsTabIndex == 0)
                        {
                            EditorGUILayout.BeginVertical("GroupBox");

                            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.showSaveParkingConfigSettings)));

                            if (creator.showSaveParkingConfigSettings)
                            {
                                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(creator.configName)));
                                EditorGUILayout.PropertyField(configSo.FindProperty("roadParkingConfigSavePath"));

                                if (GUILayout.Button("Save Config"))
                                {
                                    creator.SaveParkingConfig();
                                }
                            }

                            EditorGUILayout.EndVertical();
                        }
                    }

                    DrawCreatedLines();

                    if (GUILayout.Button("Create Line"))
                    {
                        creator.CreateParkingLine();
                    }

                    parkingConfigSo.ApplyModifiedProperties();
                }
                else
                {
                    if (creator.lineDatas?.Count > 0)
                    {
                        DrawCreatedLines();
                    }
                }

            }, ref segmentEditorSettings.ParkingBuilderSettingsFoldOut);
        }

        private void DrawCreatedLines()
        {
            EditorGUILayout.BeginVertical("GroupBox");

            EditorGUILayout.LabelField("Created Lines", EditorStyles.boldLabel);

            for (int i = 0; i < creator.lineDatas.Count; i++)
            {
                var lineData = creator.lineDatas[i];

                if (lineData != null)
                {
                    EditorGUILayout.BeginHorizontal();

                    var index = i;

                    var indexText = "";

                    if (lineData.LineData.Count > 0 && lineData.LineData[0] != null && lineData.LineData.Last() != null)
                    {
                        var index1 = creator.CreatedTrafficNodes.IndexOf(lineData.LineData[0]) + 1;
                        var index2 = creator.CreatedTrafficNodes.IndexOf(lineData.LineData.Last()) + 1;

                        indexText = $"Index ({index1}-{index2})";
                    }

                    EditorGUILayout.LabelField($"Line Node Count {lineData.LineData.Count} {indexText}", EditorStyles.boldLabel);

                    GUI.enabled = lineData.ParkingLineSettings != null;

                    if (GUILayout.Button("edit", GUILayout.Width(50)))
                    {
                        creator.EditParkingLine(index, true);
                    }

                    GUI.enabled = true;

                    if (GUILayout.Button("x", GUILayout.Width(RemoveLineButtonSize), GUILayout.Height(RemoveLineButtonSize)))
                    {
                        creator.ClearParkingLine(index, true);
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void CheckForOffsetHeader()
        {
            var config = creator.CurrentParkingLineSettings;

            if (!config)
            {
                return;
            }

            int headerCount = 0;

            if (pathOffsetHeaders != null)
            {
                headerCount = pathOffsetHeaders.Length - 1;
            }

            var placeCount = config.PlaceCount;

            if (headerCount != placeCount)
            {
                const int indexOffset = 2;
                pathOffsetHeaders = new string[placeCount + indexOffset];
                pathOffsetHeaders[0] = "None";
                pathOffsetHeaders[1] = "All";

                for (int i = indexOffset; i < placeCount + indexOffset; i++)
                {
                    pathOffsetHeaders[i] = $"Path{i - 1}";
                }
            }
        }
    }
}

#endif