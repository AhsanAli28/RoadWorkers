#if UNITY_EDITOR
using Spirit604.CityEditor.Road.Debug;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    [CustomEditor(typeof(RoadSegmentPlacer))]
    public class RoadSegmentPlacerEditor : Editor
    {
        private const float RotateButtonScreenWidth = 50;

        private GUIStyle guiStyle;
        private RoadSegmentPlacer roadSegmentPlacer;

        private void OnEnable()
        {
            guiStyle = new GUIStyle();
            guiStyle.fontSize = 48;
            guiStyle.normal.textColor = Color.white;
            roadSegmentPlacer = target as RoadSegmentPlacer;
            roadSegmentPlacer.OnInspectorEnabled();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPrefabBlock(roadSegmentPlacer);

            DrawSceneSettingsBlock(roadSegmentPlacer);

            DrawAvailablePrefabs(roadSegmentPlacer);

            DrawButtons(roadSegmentPlacer);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPrefabBlock(RoadSegmentPlacer roadSegmentPlacer)
        {
            System.Action prefabCallback = () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("roadNodeParent"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("roadCreatorConfig"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("roadSegmentCreatorConfig"));

                EditorGUILayout.BeginHorizontal();

                var assetPathProp = serializedObject.FindProperty("assetPath");
                EditorGUILayout.PropertyField(assetPathProp);

                if (GUILayout.Button("+", GUILayout.Width(25f)))
                {
                    var path = AssetDatabaseExtension.SelectProjectFolderFromDialogWindow("Select prefab segment path", assetPathProp.stringValue, "");

                    if (roadSegmentPlacer.SetNewPath(path))
                    {
                        Repaint();
                    }
                }

                EditorGUILayout.EndHorizontal();
            };

            var prefabFoldout = roadSegmentPlacer.PrefabFoldout;
            InspectorExtension.DrawDefaultInspectorGroupBlock("Prefabs", prefabCallback, ref prefabFoldout);
            roadSegmentPlacer.PrefabFoldout = prefabFoldout;
        }

        private void DrawSceneSettingsBlock(RoadSegmentPlacer roadSegmentPlacer)
        {
            System.Action sceneSettingsCallback = () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("showMovementHandlers"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("showRotationHandlers"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("showDeleteButtons"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("showLightInfo"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onlyShortNames"));

                var roadSnapPositionProp = serializedObject.FindProperty("roadSnapPosition");

                EditorGUILayout.PropertyField(roadSnapPositionProp);

                if (roadSnapPositionProp.boolValue)
                {
                    var snapTypeProp = serializedObject.FindProperty("snapType");
                    EditorGUILayout.PropertyField(snapTypeProp);

                    var snapType = (RoadSegmentPlacer.SnapType)snapTypeProp.enumValueIndex;

                    switch (snapType)
                    {
                        case RoadSegmentPlacer.SnapType.Custom:
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("customSnapSize"));
                            break;
                    }

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("evenSizeSnapPosition"));
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("createdSegments"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("roadSegmentPrefabs"));
            };

            var sceneFoldout = roadSegmentPlacer.SceneFoldout;
            InspectorExtension.DrawDefaultInspectorGroupBlock("Scene Settings", sceneSettingsCallback, ref sceneFoldout);
            roadSegmentPlacer.SceneFoldout = sceneFoldout;
        }

        private void DrawButtons(RoadSegmentPlacer roadCreator)
        {
            if (roadCreator.RoadSegmentPrefabs?.Count > 0)
            {
                if (GUILayout.Button("Add Selected RoadSegment"))
                {
                    roadCreator.AddRoadSegment();
                }
            }

            if (GUILayout.Button("Add RoadSegment Creator"))
            {
                roadCreator.AddRoadSegmentCreator();
            }

            if (GUILayout.Button("Load Assets"))
            {
                roadCreator.LoadAssets();
            }

            if (GUILayout.Button("Connect roads"))
            {
                roadCreator.Connect();
            }

            if (GUILayout.Button("Add Scene Segments"))
            {
                roadCreator.AddSceneSegments();
            }
        }

        private static void DrawAvailablePrefabs(RoadSegmentPlacer roadCreator)
        {
            if (roadCreator.ShouldToLoadAssets)
            {
                roadCreator.LoadAssets();
            }

            System.Action availablePrefabsCallback = () =>
            {
                if (roadCreator.headers != null)
                {
                    roadCreator.prefabIndex = GUILayout.SelectionGrid(roadCreator.prefabIndex, roadCreator.headers, 4);
                }
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Available Prefabs", availablePrefabsCallback);
        }

        private void OnSceneGUI()
        {
            var segments = roadSegmentPlacer.CreatedSegments;

            for (int i = 0; i < segments?.Count; i++)
            {
                if (segments[i] == null)
                {
                    continue;
                }

                var roadTransform = segments[i].transform;

                if (roadSegmentPlacer.ShowMovementHandlers)
                {
                    var oldPosition = roadTransform.position;

                    EditorGUI.BeginChangeCheck();

                    var newPosition = Handles.PositionHandle(roadTransform.position, Quaternion.identity);

                    if (EditorGUI.EndChangeCheck())
                    {
                        roadSegmentPlacer.LastSelected = segments[i];
                        roadTransform.position = newPosition;

                        if (roadSegmentPlacer.RoadSnapPosition)
                        {
                            if (!newPosition.IsEqual(oldPosition))
                            {
                                roadSegmentPlacer.SnapObject(roadTransform, roadSegmentPlacer.CurrentSnapType, roadSegmentPlacer.EvenSizeSnapPosition, roadSegmentPlacer.CustomSnapSize);
                            }
                        }
                    }
                }

                var worldPosition = roadTransform.position;

                if (roadSegmentPlacer.ShowRotationHandlers)
                {
                    var position = worldPosition + SceneView.currentDrawingSceneView.rotation * new Vector3(0, -3, 0);

                    var guiPosition = HandleUtility.WorldToGUIPoint(position) + new Vector2(-RotateButtonScreenWidth, 0);
                    Rect rect = new Rect(guiPosition, new Vector2(RotateButtonScreenWidth * 2, RotateButtonScreenWidth));

                    Handles.BeginGUI();
                    GUILayout.BeginArea(rect);
                    GUILayout.BeginHorizontal();

                    if (roadSegmentPlacer.RoadCreatorConfig?.RotateButtonTextureLeft != null)
                    {
                        if (GUILayout.Button(roadSegmentPlacer.RoadCreatorConfig.RotateButtonTextureLeft, guiStyle, GUILayout.Width(RotateButtonScreenWidth)))
                        {
                            roadSegmentPlacer.RotateSegment(segments[i], new Vector3(0, -90));
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("<", guiStyle, GUILayout.Width(RotateButtonScreenWidth)))
                        {
                            roadSegmentPlacer.RotateSegment(segments[i], new Vector3(0, -90));
                        }
                    }

                    if (roadSegmentPlacer.RoadCreatorConfig?.RotateButtonTextureRight != null)
                    {
                        if (GUILayout.Button(roadSegmentPlacer.RoadCreatorConfig?.RotateButtonTextureRight, guiStyle, GUILayout.Width(RotateButtonScreenWidth)))
                        {
                            roadSegmentPlacer.RotateSegment(segments[i], new Vector3(0, 90));
                        }
                    }
                    else
                    {
                        if (GUILayout.Button(">", guiStyle, GUILayout.Width(RotateButtonScreenWidth)))
                        {
                            roadSegmentPlacer.RotateSegment(segments[i], new Vector3(0, 90));
                        }
                    }

                    GUILayout.EndHorizontal();
                    GUILayout.EndArea();
                    Handles.EndGUI();
                }

                if (roadSegmentPlacer.ShowDeleteButtons)
                {
                    Action deleteAction = () => roadSegmentPlacer.DestroySegment(segments[i]);

                    Texture deleteButtonTexture = null;

                    if (roadSegmentPlacer.RoadCreatorConfig)
                    {
                        deleteButtonTexture = roadSegmentPlacer.RoadCreatorConfig.DeleteButtonTexture;
                    }

                    var position = worldPosition + SceneView.currentDrawingSceneView.rotation * new Vector3(15, 0, 15);

                    float buttonWidth = 50f;

                    if (deleteButtonTexture)
                    {
                        EditorExtension.DrawButton(deleteButtonTexture, position, buttonWidth, deleteAction);
                    }
                    else
                    {
                        EditorExtension.DrawButton("X", position, buttonWidth, deleteAction);
                    }
                }

                if (roadSegmentPlacer.ShowLightInfo)
                {
                    var trafficLightCrossroad = segments[i].GetComponent<TrafficLightCrossroad>();

                    TrafficLightCrossroadInfoWorldGuiRectDrawer.DrawInfo(trafficLightCrossroad, worldPosition);
                }
            }

            HandleKeys();
        }

        private void HandleKeys()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                if (roadSegmentPlacer.LastSelected)
                {
                    if (Event.current.keyCode == roadSegmentPlacer.GetRotateKey())
                    {
                        Event.current.Use();
                        roadSegmentPlacer.RotateSegment(roadSegmentPlacer.LastSelected, new Vector3(0, 90));
                    }
                }

                if (Event.current.keyCode == KeyCode.F)
                {
                    Event.current.Use();
                    roadSegmentPlacer.Focus();
                }
            }
        }
    }
}
#endif