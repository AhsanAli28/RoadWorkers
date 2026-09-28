#if UNITY_EDITOR
using Spirit604.Attributes;
using Spirit604.CityEditor.Pedestrian;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    [CustomEditor(typeof(PedestrianNodeCreator))]
    public class PedestrianNodeCreatorEditor : Editor
    {
        private const string DocLink = "https://dotstrafficcity.readthedocs.io/en/latest/pedestrianNode.html#pedestrian-node-creator";

        private PedestrianNodeCreator nodeCreator;
        private AdvancedConnectionWindow advancedConnectionWindow;
        private GUIStyle guiButtonStyle = new GUIStyle();
        private Tool prevTool;

        private void OnEnable()
        {
            nodeCreator = (PedestrianNodeCreator)target;

            guiButtonStyle = new GUIStyle();
            guiButtonStyle.fontSize = 24;
            guiButtonStyle.normal.textColor = Color.white;
            guiButtonStyle.border = new RectOffset(10, 10, 10, 10);

            prevTool = Tools.current;
            Tools.current = Tool.None;

            EditorExtension.FocusSceneViewTab();
            nodeCreator.OnInspectorEnabled();
            nodeCreator.OnSelected += NodeCreator_OnSelected;
            nodeCreator.OnSelectionModeChangedEvent += NodeCreator_OnSelectionModeChangedEvent;
            AdvancedConnectionWindow.OnProcessedNodes += AdvancedConnectionWindow_OnProcessedNodes;
        }

        private void OnDisable()
        {
            nodeCreator.OnInspectorDisabled();

            if (prevTool != Tool.None)
                Tools.current = prevTool;

            DisposeConnectionWindow();
            nodeCreator.OnSelected -= NodeCreator_OnSelected;
            nodeCreator.OnSelectionModeChangedEvent -= NodeCreator_OnSelectionModeChangedEvent;
            AdvancedConnectionWindow.OnProcessedNodes -= AdvancedConnectionWindow_OnProcessedNodes;
        }

        #region Inspector

        public override void OnInspectorGUI()
        {
            PedestrianNodeCreator controller = (PedestrianNodeCreator)target;

            serializedObject.Update();

            DocumentationLinkerUtils.ShowButtonFirst(DocLink, -4);

            ShowCachedValuesSettings(controller);

            ShowCommonSettings(controller);

            ShowSceneSettings(controller);

            ShowNodeSettings(controller);

            ShowSelectionValues(controller);

            serializedObject.ApplyModifiedProperties();

            #region Inspector Buttons


            if (GUILayout.Button("Create node"))
            {
                ((PedestrianNodeCreator)target).Create();
            }

            if (GUILayout.Button("Add All Scene Pedestrian Nodes"))
            {
                controller.UpdateAllScenePedestrianNodesData();
            }

            if (GUILayout.Button("Add All Scene Custom Pedestrian Nodes"))
            {
                controller.UpdateCustomPedestrianNodesData();
            }

            if (GUILayout.Button("Save global path width"))
            {
                controller.SaveGlobalRoutesWidth();
            }

            if (GUILayout.Button("Reset All Custom Path width"))
            {
                if (EditorUtility.DisplayDialog("Warning!", "Are you sure to reset all custom path width?", "Ok", "Cancel"))
                {
                    controller.ResetAllCustomPath();
                }
            }

            if (GUILayout.Button("Clear Created Nodes Info"))
            {
                controller.ClearCreatedNodes();
            }

            if (GUILayout.Button("Clear Partial Connection"))
            {
                controller.ClearPartialConnection();
            }

            switch (controller.selectionMode)
            {
                case PedestrianNodeCreator.SelectionMode.Single:
                    {
                        if (controller.selectedNode)
                        {
                            if (controller.autoSnapPosition)
                            {
                                if (GUILayout.Button("Snap To Grid"))
                                {
                                    controller.SnapSelectedToClosestGridPoint();
                                }
                            }

                            if (GUILayout.Button("Open Advanced Connection Window"))
                            {
                                ShowConnectionWindow();
                            }
                        }

                        break;
                    }
                case PedestrianNodeCreator.SelectionMode.Multiple:
                    {
                        if (controller.autoSnapPosition && controller.selectedPedestrianNodes.Count > 0)
                        {
                            if (GUILayout.Button("Snap To Grid"))
                            {
                                controller.SnapSelectedToClosestGridPoint();
                            }
                        }

                        if (GUILayout.Button("Clear Selection"))
                        {
                            controller.ClearSelection();
                        }

                        break;
                    }
            }

            #endregion
        }

        private void ShowCachedValuesSettings(PedestrianNodeCreator controller)
        {
            System.Action cachedValuesContent = () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.createRoot)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.pedestrianNodePrefab)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.pedestrianNodeCreatorConfig)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.pedestrianNodeHotkeyConfig)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.pedestrianNodeDataViewerConfig)));
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Cached values", cachedValuesContent, ref controller.cachedValues);
        }

        private void ShowCommonSettings(PedestrianNodeCreator controller)
        {
            System.Action settingsInfoContent = () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.customParent)));

                if (controller.customParent == null)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.createPath)));
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.showHandlers)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.showHandleType)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.selectionMode)));

                if (controller.selectionMode == PedestrianNodeCreator.SelectionMode.Multiple)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.multipleHandleType)));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.unselectSelected)));
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.maxPathWidth)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.connectWithPreviousNode)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.autoSelectConnectedNode)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.allowConnectTrafficNode)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.autoSplitConnection)));

                switch (controller.autoSplitConnection)
                {
                    case PedestrianNodeCreator.AutoSplitConnectionType.Disabled:
                        break;
                    case PedestrianNodeCreator.AutoSplitConnectionType.CustomAngle:
                        {
                            EditorGUI.indentLevel++;
                            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.customSplitAngle)));
                            EditorGUI.indentLevel--;
                            break;
                        }
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.autoRejoinLine)));

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.autoAttachToSurface)));

                if (controller.autoAttachToSurface)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.surfaceMask)));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.attachType)));
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.autoSnapPosition)));

                if (controller.autoSnapPosition)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.snapValue)));
                    EditorGUI.indentLevel--;
                }
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Settings", settingsInfoContent);
        }

        private void ShowSceneSettings(PedestrianNodeCreator controller)
        {
            System.Action sceneInfoContent = () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.showPath)));

                if (controller.showPath)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.showPathType)));
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.showNodeButtons)));

                if (controller.showNodeButtons)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.nodeButtonType)));
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.showUniqueInfo)));

                if (controller.showUniqueInfo)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.showFullDescription)));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.showOnlyOnCursor)));
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.showResetCustomRouteButtons)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.showBorderRoutes)));

                if (controller.showBorderRoutes)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.showPathWidthType)));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.defaultRouteColor)));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.customRouteColor)));
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.showTrafficNodeConnection)));

                if (controller.showTrafficNodeConnection)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.trafficNodeConnectionColor)));
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.showSelectedNodeSettings)));
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Scene Settings", sceneInfoContent, ref controller.sceneSettings);
        }

        private void ShowNodeSettings(PedestrianNodeCreator controller)
        {
            if (!controller.showSelectedNodeSettings)
            {
                return;
            }

            System.Action selectedNodeSettingsContent = () =>
            {
                switch (controller.selectionMode)
                {
                    case PedestrianNodeCreator.SelectionMode.Single:
                        {
                            var selectedNode = controller.selectedNode;

                            if (selectedNode != null)
                            {
                                EditorGUI.BeginChangeCheck();

                                var pedestrianNodeType = (PedestrianNodeType)EditorGUILayout.EnumPopup("Pedestrian Node Type", selectedNode.PedestrianNodeType);

                                if (EditorGUI.EndChangeCheck())
                                {
                                    selectedNode.PedestrianNodeType = pedestrianNodeType;
                                    selectedNode.ChangeNodeType(pedestrianNodeType);
                                    controller.OnSettingsChanged(selectedNode);
                                }

                                EditorGUI.BeginChangeCheck();

                                var pedestrianNodeShapeType = (NodeShapeType)EditorGUILayout.EnumPopup("Pedestrian Node Shape Type", selectedNode.PedestrianNodeShapeType);

                                GUI.enabled = selectedNode.CustomCapacity;

                                var capacity = EditorGUILayout.IntSlider("Capacity", selectedNode.Capacity, -1, 100);

                                GUI.enabled = true;

                                var weight = EditorGUILayout.Slider("Weight", selectedNode.PriorityWeight, 0f, 1f);
                                var customAchieveDistance = EditorGUILayout.Slider("Custom Achieve Distance", selectedNode.CustomAchieveDistance, 0f, 20f);
                                var canSpawnInView = EditorGUILayout.Toggle("Can Spawn In View", selectedNode.CanSpawnInView);
                                var chanceToSpawn = EditorGUILayout.Slider("Chance To Spawn", selectedNode.ChanceToSpawn, 0f, 1f);
                                var maxPathWidth = EditorGUILayout.Slider("Max Path Width", selectedNode.MaxPathWidth, 0.1f, 20f);
                                var hasRandomOffset = EditorGUILayout.Toggle("Has Movement Random Offset", selectedNode.HasMovementRandomOffset);

                                if (EditorGUI.EndChangeCheck())
                                {
                                    selectedNode.PedestrianNodeShapeType = pedestrianNodeShapeType;
                                    selectedNode.Capacity = capacity;
                                    selectedNode.PriorityWeight = weight;
                                    selectedNode.CustomAchieveDistance = customAchieveDistance;
                                    selectedNode.CanSpawnInView = canSpawnInView;
                                    selectedNode.ChanceToSpawn = chanceToSpawn;
                                    selectedNode.MaxPathWidth = maxPathWidth;
                                    selectedNode.HasMovementRandomOffset = hasRandomOffset;

                                    EditorSaver.SetObjectDirty(selectedNode);

                                    controller.OnSettingsChanged(selectedNode);
                                }
                            }
                            else
                            {
                                EditorGUILayout.LabelField("Not Selected", EditorStyles.boldLabel);
                            }

                            break;
                        }
                    case PedestrianNodeCreator.SelectionMode.Multiple:
                        {
                            var selectedNodes = controller.selectedPedestrianNodes;

                            if (selectedNodes.Count > 0)
                            {
                                var firstSelectedNode = controller.selectedPedestrianNodes[0];

                                if (firstSelectedNode != null)
                                {
                                    EditorGUILayout.LabelField($"Selected {selectedNodes.Count} nodes", EditorStyles.boldLabel);

                                    EditorGUI.BeginChangeCheck();

                                    var pedestrianNodeType = (PedestrianNodeType)EditorGUILayout.EnumPopup("PedestrianNodeType", firstSelectedNode.PedestrianNodeType);
                                    var capacity = EditorGUILayout.IntSlider("Capacity", firstSelectedNode.Capacity, -1, 20);
                                    var weight = EditorGUILayout.Slider("Weight", firstSelectedNode.PriorityWeight, 0f, 1f);
                                    var customAchieveDistance = EditorGUILayout.Slider("Custom Achieve Distance", firstSelectedNode.CustomAchieveDistance, 0f, 20f);
                                    var canSpawnInVision = EditorGUILayout.Toggle("Can Spawn In Vision", firstSelectedNode.CanSpawnInView);
                                    var chanceToSpawn = EditorGUILayout.Slider("Chance To Spawn", firstSelectedNode.ChanceToSpawn, 0f, 1f);
                                    var maxPathWidth = EditorGUILayout.Slider("Max Path Width", firstSelectedNode.MaxPathWidth, 0.1f, 20f);

                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        for (int i = 0; i < selectedNodes.Count; i++)
                                        {
                                            var localNode = selectedNodes[i];

                                            if (localNode != null)
                                            {
                                                localNode.PedestrianNodeType = pedestrianNodeType;
                                                localNode.Capacity = capacity;
                                                localNode.PriorityWeight = weight;
                                                localNode.CustomAchieveDistance = customAchieveDistance;
                                                localNode.CanSpawnInView = canSpawnInVision;
                                                localNode.ChanceToSpawn = chanceToSpawn;
                                                localNode.MaxPathWidth = maxPathWidth;

                                                EditorSaver.SetObjectDirty(localNode);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                EditorGUILayout.LabelField("Not Selected", EditorStyles.boldLabel);
                            }

                            break;
                        }
                }
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Selected Node Settings", selectedNodeSettingsContent, ref controller.selectedNodeSettingsFoldout);
        }

        private void ShowSelectionValues(PedestrianNodeCreator controller)
        {
            System.Action selectionInfoContent = () =>
            {
                switch (controller.selectionMode)
                {
                    case PedestrianNodeCreator.SelectionMode.Single:
                        {
                            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.selectedNode)));
                            break;
                        }
                    case PedestrianNodeCreator.SelectionMode.Multiple:
                        {
                            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.selectedPedestrianNodes)));
                            break;
                        }
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.createdPedestrianNodes)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(controller.customPathPedestrianNodes)));
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Selection Values", selectionInfoContent, ref controller.selectionInfo);
        }

        #endregion

        #region Scene GUI

        private void OnSceneGUI()
        {
            PedestrianNodeCreator controller = (PedestrianNodeCreator)target;

            if (Tools.current != Tool.None)
            {
                Tools.current = Tool.None;
                EditorExtension.FocusSceneViewTab();
            }

            HandleCursor(controller);
            ProcessHandlers(controller);
            ProcessSelectedNode(controller);
            HandleKeys(controller);
            ShowNodesInfo(controller);
            ShowConnections(controller);
        }

        private void HandleCursor(PedestrianNodeCreator controller)
        {
            controller.HandleCursor(Event.current.mousePosition);
        }

        private void ProcessHandlers(PedestrianNodeCreator controller)
        {
            if (controller.showHandlers)
            {
                if (controller.showHandleType == PedestrianNodeCreator.ShowHandleType.OnlySelected)
                {
                    if (controller.selectionMode == PedestrianNodeCreator.SelectionMode.Single)
                    {
                        ShowPositionHandler(controller.selectedNode, controller);
                    }
                    else
                    {
                        if (controller.multipleHandleType == PedestrianNodeCreator.MultipleHandleType.Single)
                        {
                            for (int i = 0; i < controller.selectedPedestrianNodes?.Count; i++)
                            {
                                ShowPositionHandler(controller.selectedPedestrianNodes[i], controller);
                            }
                        }
                        else
                        {
                            if (controller.selectedPedestrianNodes.Count > 0)
                            {
                                var bounds = new Bounds(controller.selectedPedestrianNodes[0].transform.position, Vector3.zero);

                                for (int i = 1; i < controller.selectedPedestrianNodes?.Count; i++)
                                {
                                    bounds.Encapsulate(controller.selectedPedestrianNodes[i].transform.position);
                                }

                                Vector3 sourcePoint = bounds.center;

                                EditorGUI.BeginChangeCheck();

                                var newPosition = Handles.PositionHandle(sourcePoint, Quaternion.identity);

                                if (EditorGUI.EndChangeCheck())
                                {
                                    Vector3 offset = newPosition - sourcePoint;

                                    if (offset != Vector3.zero)
                                    {
                                        controller.MoveMultipleSelectedNodesPosition(offset);
                                    }
                                }
                            }
                        }
                    }
                }

                if (controller.showHandleType == PedestrianNodeCreator.ShowHandleType.All)
                {
                    for (int i = 0; i < controller.allPedestrianNodes?.Count; i++)
                    {
                        ShowPositionHandler(controller.allPedestrianNodes[i], controller);
                    }
                }

                if (controller.showHandleType == PedestrianNodeCreator.ShowHandleType.OnlyCreated)
                {
                    for (int i = 0; i < controller.createdPedestrianNodes?.Count; i++)
                    {
                        ShowPositionHandler(controller.createdPedestrianNodes[i], controller);
                    }
                }
            }

            if (controller.showResetCustomRouteButtons)
            {
                var customNodes = controller.customPathPedestrianNodes;

                for (int i = 0; i < customNodes?.Count; i++)
                {
                    var pos = customNodes[i].transform.position;

                    System.Action resetCallback = () =>
                    {
                        controller.ResetCustomPathNode(customNodes[i], true, true);
                    };

                    EditorExtension.DrawButton("Reset", pos, 100f, resetCallback);
                }
            }
        }

        private void ProcessSelectedNode(PedestrianNodeCreator controller)
        {
            switch (controller.selectionMode)
            {
                case PedestrianNodeCreator.SelectionMode.Single:
                    {
                        HandleSelected(controller.selectedNode, controller);
                        break;
                    }
                case PedestrianNodeCreator.SelectionMode.Multiple:
                    {
                        var selectedNodes = controller.selectedPedestrianNodes;

                        for (int i = 0; i < selectedNodes.Count; i++)
                        {
                            HandleSelected(selectedNodes[i], controller);
                        }

                        break;
                    }
            }
        }

        private void HandleKeys(PedestrianNodeCreator controller)
        {
            if (controller.pedestrianNodeHotkeyConfig == null)
            {
                UnityEngine.Debug.Log("Add PedestrianNodeHotkeyConfig!");
                return;
            }

            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == controller.pedestrianNodeHotkeyConfig.CreateButton)
                {
                    Event.current.Use();
                    controller.Create();
                }
                else if (Event.current.keyCode == controller.pedestrianNodeHotkeyConfig.UnselectButton)
                {
                    Event.current.Use();
                    controller.Unselect();
                }
                else if (Event.current.keyCode == controller.pedestrianNodeHotkeyConfig.SpawnOrConnectButton)
                {
                    Event.current.Use();
                    controller.Spawn();
                }
                else if (Event.current.keyCode == controller.pedestrianNodeHotkeyConfig.SelectNodeButton)
                {
                    Event.current.Use();
                    controller.TryToSelectNode(Event.current.mousePosition);
                }
                else if (Event.current.keyCode == KeyCode.F)
                {
                    Event.current.Use();
                    controller.DoFocus();
                }
            }
        }

        private void ShowNodesInfo(PedestrianNodeCreator controller)
        {
            if (!controller.showUniqueInfo)
                return;

            for (int i = 0; i < controller.allPedestrianNodes?.Count; i++)
            {
                var node = controller.allPedestrianNodes[i];
                var dict = controller.pedestrianNodeDataFilter.FilteredData;

                if (dict.ContainsKey(node))
                {
                    var data = dict[node];
                    SceneDataGuiViewPopup.DrawInfo(node.gameObject, data, controller.showFullDescription, controller.showOnlyOnCursor);
                }
            }
        }

        private void ShowConnections(PedestrianNodeCreator controller)
        {
            if (controller.showTrafficNodeConnection)
            {
                var pedestrianNodes = controller.allPedestrianNodes;

                for (int i = 0; i < pedestrianNodes.Count; i++)
                {
                    PedestrianNode pedestrianNode = (PedestrianNode)pedestrianNodes[i];

                    if (pedestrianNode.ConnectedTrafficNode != null)
                    {
                        Handles.color = controller.trafficNodeConnectionColor;
                        Handles.DrawLine(pedestrianNode.transform.position, pedestrianNode.ConnectedTrafficNode.transform.position);

                        TrafficLightHandlerEditorExtension.DrawLightObjectBounds(pedestrianNode.ConnectedTrafficNode.transform.position);
                    }
                }
            }
        }

        private void HandleSelected(PedestrianNode selectedNode, PedestrianNodeCreator controller)
        {
            if (!selectedNode)
                return;

            Handles.DrawWireDisc(selectedNode.transform.position, Vector3.up, 2f);

            if (controller.showNodeButtons)
            {
                var worldPosition = selectedNode.transform.position;
                var guiPosition = HandleUtility.WorldToGUIPoint(worldPosition) + new Vector2(30, -60);

                Rect rect = new Rect(guiPosition, new Vector2(100, 100));

                Handles.BeginGUI();
                GUILayout.BeginArea(rect);

                if (controller.PedestrianNodeCreatorConfig != null && controller.PedestrianNodeCreatorConfig.DeleteButtonTexture != null)
                {
                    if (GUILayout.Button(controller.PedestrianNodeCreatorConfig.DeleteButtonTexture, guiButtonStyle, GUILayout.Width(50)))
                    {
                        controller.HandleSelectedNodeButton(selectedNode);
                    }
                }
                else
                {
                    if (GUILayout.Button("Delete", guiButtonStyle, GUILayout.Width(100)))
                    {
                        controller.HandleSelectedNodeButton(selectedNode);
                    }
                }

                GUILayout.EndArea();
                Handles.EndGUI();
            }
        }

        private void ShowPositionHandler(PedestrianNode pedestrianNode, PedestrianNodeCreator controller)
        {
            if (!pedestrianNode)
                return;

            var oldPosition = pedestrianNode.transform.position;

            EditorGUI.BeginChangeCheck();

            var newPosition = Handles.PositionHandle(oldPosition, Quaternion.identity);

            if (EditorGUI.EndChangeCheck())
            {
                Vector3 moveOffset = newPosition - oldPosition;

                if (moveOffset != Vector3.zero)
                {
                    controller.SetNodeNewPosition(pedestrianNode, oldPosition, moveOffset);
                }
            }
        }

        private AdvancedConnectionWindow ShowConnectionWindow()
        {
            DisposeConnectionWindow();
            advancedConnectionWindow = AdvancedConnectionWindow.ShowWindow();
            advancedConnectionWindow.Initialize(nodeCreator.selectedNode);
            advancedConnectionWindow.OnClose += PedestrianNodeAdvancedConnectionWindow_OnClose;
            nodeCreator.HasConnectionWindow = true;
            return advancedConnectionWindow;
        }

        private void DisposeConnectionWindow(bool close = false)
        {
            if (advancedConnectionWindow == null)
                return;

            nodeCreator.HasConnectionWindow = false;
            advancedConnectionWindow.OnClose -= PedestrianNodeAdvancedConnectionWindow_OnClose;

            if (close)
                advancedConnectionWindow.Close();

            advancedConnectionWindow = null;
        }

        #endregion

        #region Event handlers

        private void NodeCreator_OnSelected(PedestrianNode selectedNode)
        {
            if (advancedConnectionWindow)
            {
                Undo.RegisterCompleteObjectUndo(advancedConnectionWindow, "Window settings changed");
                advancedConnectionWindow.Initialize(selectedNode);
            }
        }

        private void NodeCreator_OnSelectionModeChangedEvent()
        {
            if (advancedConnectionWindow != null)
            {
                DisposeConnectionWindow(true);
            }
        }

        private void PedestrianNodeAdvancedConnectionWindow_OnClose()
        {
            DisposeConnectionWindow();
        }

        private void AdvancedConnectionWindow_OnProcessedNodes()
        {
            nodeCreator.UpdateAllScenePedestrianNodesData();
        }

        #endregion
    }
}
#endif