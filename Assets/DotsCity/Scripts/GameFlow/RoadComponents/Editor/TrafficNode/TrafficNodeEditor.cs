#if UNITY_EDITOR
using Spirit604.Attributes;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(TrafficNode))]
    public class TrafficNodeEditor : SharedSettingsEditorBase<TrafficNodeEditor.EditorSettings>
    {
        private const string DocLink = "https://dotstrafficcity.readthedocs.io/en/latest/trafficNode.html";

        [Serializable]
        public class EditorSettings
        {
            public bool Cached;
            public bool Lanes = true;
            public bool Settings = true;
        }

        private GUIStyle guiStyle = new GUIStyle();
        private TrafficNode trafficNode;
        private Tool currentTool = Tool.None;
        private bool deleted;

        protected override string SaveKey => "TrafficNodeEditorKey";

        protected override void OnEnable()
        {
            base.OnEnable();
            trafficNode = target as TrafficNode;
            trafficNode.OnInspectorEnabled();

            guiStyle.normal.textColor = Color.white;
            EditorApplication.hierarchyWindowItemOnGUI += EditorApplication_hierarchyWindowItemOnGUI;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            trafficNode?.OnInspectorDisabled();
            Tools.current = currentTool;
            EditorApplication.hierarchyWindowItemOnGUI -= EditorApplication_hierarchyWindowItemOnGUI;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DocumentationLinkerUtils.ShowButtonAndHeader(target, DocLink);

            if (Selection.objects.Length == 1)
            {
                InspectorExtension.DrawDefaultInspectorGroupBlock("Cached Values", () =>
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("trafficLightCrossroad"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("trafficLightHandler"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("trafficNodeCrosswalk"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("pathPrefab"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("pathParent"));

                }, ref SharedSettings.Cached);

                InspectorExtension.DrawDefaultInspectorGroupBlock("Lane Data", () =>
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("lanes"));

                    var externalConnectionProp = serializedObject.FindProperty("externalConnection");

                    EditorGUILayout.PropertyField(externalConnectionProp);

                    if (externalConnectionProp.enumValueIndex == 0)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("externalLanes"));
                    }
                    else
                    {
                        var mergeNodeProp = serializedObject.FindProperty("mergeNode");

                        EditorGUI.BeginChangeCheck();

                        EditorGUILayout.PropertyField(mergeNodeProp);

                        if (EditorGUI.EndChangeCheck())
                        {
                            if (mergeNodeProp.objectReferenceValue != null)
                            {
                                trafficNode.UnselectMerge();
                            }

                            serializedObject.ApplyModifiedProperties();

                            if (mergeNodeProp.objectReferenceValue != null)
                            {
                                trafficNode.SetMerge(mergeNodeProp.objectReferenceValue as TrafficNode);
                            }
                        }

                        if (mergeNodeProp.objectReferenceValue != null)
                        {
                            var localSo = new SerializedObject(mergeNodeProp.objectReferenceValue);
                            localSo.Update();

                            GUI.enabled = false;
                            EditorGUILayout.PropertyField(localSo.FindProperty("lanes"), new GUIContent("Merged Lanes"));
                            GUI.enabled = true;
                        }
                    }

                }, ref SharedSettings.Lanes);
            }

            InspectorExtension.DrawDefaultInspectorGroupBlock("Settings", () =>
            {
                if (trafficNode.HasLeftLanes)
                {
                    var laneCountTypeProp = serializedObject.FindProperty("laneCountType");

                    EditorGUILayout.PropertyField(laneCountTypeProp);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("laneCount"));

                    if (laneCountTypeProp.enumValueIndex == 1)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("externalLaneCount"));
                    }
                }
                else
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("laneCount"));
                }

                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("laneWidth"));

                if (!trafficNode.IsOneWay)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("dividerWidth"));
                }

                if (EditorGUI.EndChangeCheck())
                {
                    trafficNode.SaveAllPaths();
                    serializedObject.ApplyModifiedProperties();
                    trafficNode.ReattachAllPaths();
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("chanceToSpawn"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("weight"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("customAchieveDistance"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("lightType"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("trafficNodeType"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("hasCrosswalk"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("isOneWay"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("isEndOfOneWay"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("lockPathAutoCreation"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("autoPathIsCreated"));

            }, ref SharedSettings.Settings);

            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Connect"))
            {
                trafficNode.ConnectSegments();
            }

            if (GUILayout.Button("Force Connect"))
            {
                trafficNode.ForceConnectSegments();
            }

            if (GUILayout.Button("Resize"))
            {
                trafficNode.Resize();
            }
        }

        private void OnSceneGUI()
        {
            var trafficNode = target as TrafficNode;

            var stage = EditorExtension.GetCurrentPrefabStage();

            bool canEdit = stage != null;

            if (Tools.current == Tool.Move)
            {
                currentTool = Tool.Move;
                Tools.current = Tool.None;
            }
            else if (Tools.current == Tool.Rotate)
            {
                currentTool = Tool.Rotate;
                Tools.current = Tool.None;
            }
            else if (Tools.current != Tool.None)
            {
                currentTool = Tools.current;
                Tools.current = Tool.None;
            }

            if (currentTool == Tool.Move)
            {
                EditorGUI.BeginChangeCheck();

                var newPosition = Handles.PositionHandle(trafficNode.transform.position, trafficNode.transform.rotation);

                if (EditorGUI.EndChangeCheck())
                {
                    trafficNode.SaveAllPaths();
                    trafficNode.transform.position = newPosition;
                    trafficNode.ReattachAllPaths();
                }
            }

            if (currentTool == Tool.Rotate)
            {
                EditorGUI.BeginChangeCheck();

                var newRotation = Handles.RotationHandle(trafficNode.transform.localRotation, trafficNode.transform.position);

                if (EditorGUI.EndChangeCheck())
                {
                    trafficNode.SaveAllPaths();
                    trafficNode.transform.localRotation = newRotation;
                    trafficNode.ReattachAllPaths();
                }
            }

            if (deleted)
            {
                deleted = false;
                trafficNode.DestroyNode();
            }

            TrafficNodeEditorExtension.ShowPathHandles(trafficNode, guiStyle, TrafficNodeDirectionType.Right, canEdit);
            TrafficNodeEditorExtension.ShowDivider(trafficNode);
        }

        protected override EditorSettings GetDefaultSettings()
        {
            return new EditorSettings();
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
    }
}
#endif