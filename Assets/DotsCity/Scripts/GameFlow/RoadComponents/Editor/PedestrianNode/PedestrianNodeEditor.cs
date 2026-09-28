#if UNITY_EDITOR
using Spirit604.Attributes;
using Spirit604.CityEditor.Pedestrian;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(PedestrianNode))]
    public class PedestrianNodeEditor : SharedSettingsEditorBase<PedestrianNodeEditor.EditorSettings>
    {
        private const string DocLink = "https://dotstrafficcity.readthedocs.io/en/latest/pedestrianNode.html";

        [Serializable]
        public class EditorSettings
        {
            public bool CachedFoldout;
            public bool ConnectionFoldout = true;
            public bool DirectionFoldout;
        }

        private const float FieldOffset = 2f;

        private PedestrianNode pedestrianNode;
        private static PedestrianNodeCreator creator;
        private PrefabStage prefabStage;
        private ReorderableList reorderableList1;
        private ReorderableList reorderableList2;

        protected override string SaveKey => "PedestrianNodeEditorData";

        protected override void OnEnable()
        {
            base.OnEnable();

            pedestrianNode = target as PedestrianNode;

            InitList1();
            InitList2();

            if (!creator)
            {
                creator = ObjectUtils.FindObjectOfType<PedestrianNodeCreator>();
            }

            prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            EditorExtension.FocusSceneViewTab();

            var layer = LayerMask.NameToLayer(ProjectConstants.PEDESTRIAN_NODE_LAYER_NAME);

            if (layer == -1)
            {
                UnityEngine.Debug.Log($"PedestrianNode. PedestrianNode '{ProjectConstants.PEDESTRIAN_NODE_LAYER_NAME}' layer is not defined, make sure you have added the '{ProjectConstants.PEDESTRIAN_NODE_LAYER_NAME}' layer in the Spirit604/Package Initialiazation/Layer settings");
            }
            else if (pedestrianNode.gameObject.layer != layer)
            {
                string layerName = "NaN";

                string tempLayerName = LayerMask.LayerToName(pedestrianNode.gameObject.layer);

                if (!string.IsNullOrEmpty(tempLayerName))
                {
                    layerName = tempLayerName;
                }

                UnityEngine.Debug.Log($"PedestrianNode. PedestrianNode prefab has '{layerName}' layer, but should have '{ProjectConstants.PEDESTRIAN_NODE_LAYER_NAME}' layer");
            }

            Tools.viewToolChanged += Tools_viewToolChanged;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Tools.viewToolChanged -= Tools_viewToolChanged;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DocumentationLinkerUtils.ShowButtonFirst(DocLink, -4);

            if (Selection.objects.Length == 1)
            {
                InspectorExtension.DrawDefaultInspectorGroupBlock("Cached Values", () =>
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("HotKeyConfig"));

                }, ref SharedSettings.CachedFoldout);

                InspectorExtension.DrawDefaultInspectorGroupBlock("Connection Information", () =>
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("connectedTrafficNode"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("relatedTrafficLightHandler"));
                    reorderableList1.DoLayoutList();
                    reorderableList2.DoLayoutList();

                }, ref SharedSettings.ConnectionFoldout);

                InspectorExtension.DrawDefaultInspectorGroupBlock("Directions of auto-connections", () =>
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("leftHorizontal"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("rightHorizontal"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("leftVertical"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("rightVertical"));

                }, ref SharedSettings.DirectionFoldout);
            }

            InspectorExtension.DrawDefaultInspectorGroupBlock("Settings", () =>
            {
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("pedestrianNodeType"));

                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    pedestrianNode.ChangeNodeType();
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("pedestrianNodeShapeType"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("canSpawnInView"));

                if (pedestrianNode.CustomCapacity)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("capacity"));
                }
                else
                {
                    GUI.enabled = false;
                    EditorGUILayout.IntSlider("Capacity", pedestrianNode.Capacity, -1, 100);
                    GUI.enabled = true;
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("priorityWeight"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("customAchieveDistance"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("chanceToSpawn"));

                switch (pedestrianNode.PedestrianNodeShapeType)
                {
                    case NodeShapeType.Circle:
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxPathWidth"));
                        break;
                    case NodeShapeType.Square:
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxPathWidth"), new GUIContent("Width"));
                        break;
                    case NodeShapeType.Rectangle:
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxPathWidth"), new GUIContent("Width"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("height"));
                        break;
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("hasMovementRandomOffset"));
            });

            TryToDrawTalkAreaSettings(pedestrianNode);

            serializedObject.ApplyModifiedProperties();

            if (Selection.objects.Length == 1)
            {
                if (GUILayout.Button("Connect"))
                {
                    pedestrianNode.ConnectButton();
                }

                if (GUILayout.Button("Attach To Closest Traffic Node"))
                {
                    pedestrianNode.AttachToTrafficNode();
                }
            }

            if (GUILayout.Button("Open Advanced Connection Window"))
            {
                ShowConnectionWindow();
            }

            if (creator)
            {
                if (GUILayout.Button("Open Creator"))
                {
                    Selection.activeObject = creator;
                }
            }
        }

        private void TryToDrawTalkAreaSettings(PedestrianNode pedestrianNode)
        {
            if (pedestrianNode.PedestrianNodeType != PedestrianNodeType.TalkArea)
            {
                return;
            }

            EditorGUILayout.Separator();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Area settings", EditorStyles.boldLabel);

            pedestrianNode.CurrentPedestrianNodeAreaSettings.areaShapeType = (PedestrianAreaShapeType)EditorGUILayout.EnumPopup("Area Shape Type", pedestrianNode.CurrentPedestrianNodeAreaSettings.areaShapeType);
            pedestrianNode.CurrentPedestrianNodeAreaSettings.areaSize = EditorGUILayout.Slider("Area Size", pedestrianNode.CurrentPedestrianNodeAreaSettings.areaSize, 1, 100);

            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
                EditorSaver.SetObjectDirty(pedestrianNode);
            }

            EditorGUI.BeginChangeCheck();

            pedestrianNode.CurrentPedestrianNodeAreaSettings.minSpawnCount = EditorGUILayout.IntSlider("Min Spawn Count", pedestrianNode.CurrentPedestrianNodeAreaSettings.minSpawnCount, 0, 200);

            if (pedestrianNode.CurrentPedestrianNodeAreaSettings.minSpawnCount > pedestrianNode.CurrentPedestrianNodeAreaSettings.maxSpawnCount)
            {
                pedestrianNode.CurrentPedestrianNodeAreaSettings.maxSpawnCount = pedestrianNode.CurrentPedestrianNodeAreaSettings.minSpawnCount;
            }

            pedestrianNode.CurrentPedestrianNodeAreaSettings.maxSpawnCount = EditorGUILayout.IntSlider("Max Spawn Count", pedestrianNode.CurrentPedestrianNodeAreaSettings.maxSpawnCount, 0, 200);

            pedestrianNode.CurrentPedestrianNodeAreaSettings.unlimitedTalkTime = EditorGUILayout.Toggle("Unlimited Talk Time", pedestrianNode.CurrentPedestrianNodeAreaSettings.unlimitedTalkTime);

            if (!pedestrianNode.CurrentPedestrianNodeAreaSettings.unlimitedTalkTime)
            {
                pedestrianNode.CurrentPedestrianNodeAreaSettings.minTalkTime = EditorGUILayout.Slider("Min Talk Time", pedestrianNode.CurrentPedestrianNodeAreaSettings.minTalkTime, 0, 200f);
                pedestrianNode.CurrentPedestrianNodeAreaSettings.maxTalkTime = EditorGUILayout.Slider("Max Talk Time", pedestrianNode.CurrentPedestrianNodeAreaSettings.maxTalkTime, 0, 200f);
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorSaver.SetObjectDirty(pedestrianNode);
            }

            pedestrianNode.CurrentPedestrianNodeAreaSettings.showBounds = EditorGUILayout.Toggle("Show Bounds", pedestrianNode.CurrentPedestrianNodeAreaSettings.showBounds);

            if (pedestrianNode.CurrentPedestrianNodeAreaSettings.showBounds)
            {
                EditorGUI.indentLevel++;
                pedestrianNode.CurrentPedestrianNodeAreaSettings.boundsColor = EditorGUILayout.ColorField("Bounds Color", pedestrianNode.CurrentPedestrianNodeAreaSettings.boundsColor);
                EditorGUI.indentLevel--;
            }
        }

        private void OnSceneGUI()
        {
            var pedestrianNode = target as PedestrianNode;

            HandlePedestrianAreaShape(pedestrianNode);
            HandleKeys();
        }

        private void HandlePedestrianAreaShape(PedestrianNode pedestrianNode)
        {
            if (pedestrianNode.PedestrianNodeType == PedestrianNodeType.TalkArea && pedestrianNode.CurrentPedestrianNodeAreaSettings.showBounds)
            {
                var sourceColor = UnityEditor.Handles.color;
                UnityEditor.Handles.color = pedestrianNode.CurrentPedestrianNodeAreaSettings.boundsColor;

                switch (pedestrianNode.CurrentPedestrianNodeAreaSettings.areaShapeType)
                {
                    case PedestrianAreaShapeType.Square:
                        {
                            Handles.DrawWireCube(pedestrianNode.transform.position, new Vector3(pedestrianNode.CurrentPedestrianNodeAreaSettings.areaSize, 0.1f, pedestrianNode.CurrentPedestrianNodeAreaSettings.areaSize) * 2);
                            break;
                        }
                    case PedestrianAreaShapeType.Circle:
                        {
                            Handles.DrawWireDisc(pedestrianNode.transform.position, Vector3.up, pedestrianNode.CurrentPedestrianNodeAreaSettings.areaSize);
                            break;
                        }
                }

                UnityEditor.Handles.color = sourceColor;
            }
        }

        private void HandleKeys()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                PedestrianNode pedestrianNode = (PedestrianNode)target;

                if (pedestrianNode.HotKeyConfig != null)
                {
                    if (Event.current.keyCode == pedestrianNode.HotKeyConfig.ConnectButton)
                    {
                        Event.current.Use();
                        HandleConnection(pedestrianNode);
                    }

                    HandleCreator(pedestrianNode);
                }
                else
                {
                    if (Event.current.keyCode == KeyCode.Tab)
                    {
                        Event.current.Use();
                        HandleConnection(pedestrianNode);
                    }
                }
            }
        }

        private void HandleConnection(PedestrianNode pedestrianNode)
        {
            if (prefabStage == null)
            {
                pedestrianNode.HandleConnection();
            }
            else
            {
                pedestrianNode.HandleConnection(customRaycastScene: prefabStage.scene);
            }
        }

        private void HandleCreator(PedestrianNode pedestrianNode)
        {
            if (prefabStage != null)
                return;

            if (creator && creator.AutoSelectFromNode)
            {
                if (Event.current.keyCode == pedestrianNode.HotKeyConfig.SpawnOrConnectButton)
                {
                    Event.current.Use();
                    creator.selectedNode = pedestrianNode;
                    creator.Spawn();
                    creator.SelectCreator();
                }

                if (Event.current.keyCode == pedestrianNode.HotKeyConfig.SelectNodeButton)
                {
                    Event.current.Use();

                    if (creator.TryToSelectNode(Event.current.mousePosition))
                    {
                        creator.SelectCreator();
                    }
                }
            }
        }

        protected override EditorSettings GetDefaultSettings()
        {
            return new EditorSettings();
        }

        private AdvancedConnectionWindow ShowConnectionWindow()
        {
            var window = AdvancedConnectionWindow.ShowWindow();
            window.Initialize(pedestrianNode);
            return window;
        }

        private ReorderableList InitList(string listName, string headerText, bool defaultConnection, Action regenerate)
        {
            var defaultConnectionLocal = defaultConnection;
            var prop = serializedObject.FindProperty(listName);

            var reorderableList = new ReorderableList(serializedObject, prop, true, true, true, true)
            {

            };

            reorderableList.drawHeaderCallback = (Rect rect) =>
            {
                var width = rect.width;
                var r1 = rect;

                r1.width = 25f;

                var newVal = EditorGUI.Toggle(r1, prop.isExpanded);

                if (prop.isExpanded != newVal)
                {
                    prop.isExpanded = newVal;
                    regenerate.Invoke();
                    Repaint();
                }

                r1.x += 25f;
                r1.width = width;

                EditorGUI.LabelField(r1, headerText);

                r1.x += r1.width - 45;
                r1.width = 25f;

                prop.arraySize = EditorGUI.IntField(r1, prop.arraySize);
            };

            if (!prop.isExpanded)
            {
                reorderableList.drawNoneElementCallback = (Rect rect) => { };
                reorderableList.drawFooterCallback = (Rect rect) => { };
                reorderableList.drawElementCallback = (rect, index, isActive, isFocused) => { };
                reorderableList.footerHeight = 0;
                reorderableList.elementHeight = 0;
                return reorderableList;
            }

            reorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                if (!prop.isExpanded) return;

                var r1 = rect;

                r1.height = GetLineSize();

                var arrayElement = reorderableList.serializedProperty.GetArrayElementAtIndex(index);

                var data = pedestrianNode.TryToGetConnectionData(index, defaultConnectionLocal);

                bool hasData = data != null;

                var connectedNode = arrayElement.objectReferenceValue as PedestrianNode;

                var partialConnected = connectedNode != null && (!hasData || !data.Oneway) && !pedestrianNode.HasDoubleConnection(connectedNode);

                var oldColor = GUI.color;

                if (partialConnected)
                    GUI.color = Color.yellow;

                EditorGUI.PropertyField(r1, arrayElement);

                GUI.color = oldColor;

                var buttonR = r1;

                buttonR.x += EditorGUIUtility.labelWidth - 30;
                buttonR.width = 20f;
                buttonR.height = 20f;

                var text = hasData ? "-" : "+";

                if (GUI.Button(buttonR, text))
                {
                    if (!hasData)
                    {
                        pedestrianNode.AddConnectionData(index, defaultConnectionLocal);
                    }
                    else
                    {
                        pedestrianNode.TryToRemoveConnectionData(index, defaultConnectionLocal);
                    }
                }

                if (hasData)
                {
                    r1.y += GetLineOffset();

                    EditorGUI.BeginChangeCheck();

                    data.SubNodeCount = EditorGUI.IntField(r1, "Sub Node Count", data.SubNodeCount);

                    if (pedestrianNode.CanConnectBeOneway(pedestrianNode.TryToGetConnectedNode(index, defaultConnectionLocal)))
                    {
                        r1.y += GetLineOffset();
                        data.Oneway = EditorGUI.Toggle(r1, "Oneway", data.Oneway);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorSaver.SetObjectDirty(pedestrianNode);
                    }
                }
            };

            reorderableList.onRemoveCallback = (list) =>
            {
                pedestrianNode.TryToRemoveConnectionData(list.index, defaultConnectionLocal);
                prop.DeleteArrayElementAtIndex(list.index);
            };

            reorderableList.elementHeightCallback = (index) =>
            {
                if (!prop.isExpanded) return -10;

                int fieldCount = 1;

                var data = pedestrianNode.TryToGetConnectionData(index, defaultConnectionLocal);

                if (data != null)
                {
                    fieldCount += 1;

                    if (pedestrianNode.CanConnectBeOneway(pedestrianNode.TryToGetConnectedNode(index, defaultConnectionLocal)))
                    {
                        fieldCount += 1;
                    }
                }

                return fieldCount * GetLineSize() + 2;
            };

            return reorderableList;
        }

        private void InitList1()
        {
            reorderableList1 = InitList("autoConnectedPedestrianNodes", "Auto Connected Pedestrian Nodes", false, InitList1);
        }

        private void InitList2()
        {
            reorderableList2 = InitList("defaultConnectedPedestrianNodes", "Default Connected Pedestrian Nodes", true, InitList2);
        }

        private float GetLineSize() => EditorGUIUtility.singleLineHeight;

        private float GetLineOffset() => GetLineSize() + FieldOffset;

        private void Tools_viewToolChanged()
        {
            EditorExtension.FocusSceneViewTab();
        }
    }
}
#endif