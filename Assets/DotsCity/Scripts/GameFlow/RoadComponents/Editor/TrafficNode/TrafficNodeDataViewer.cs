using Spirit604.Attributes;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public class TrafficNodeDataViewer : EditorWindowBase
    {
        #region Consts

        private const string DocLink = "https://dotstrafficcity.readthedocs.io/en/latest/trafficNodeDebug.html#traffic-node-data-viewer";
        private const float EditFilterButtonScreenSize = 20f;

        #endregion

        #region Serialized Variables

        [Tooltip("Internal filter settings")]
        [SerializeField] private SceneDataViewerConfig trafficNodeDataViewerConfig;

        [SerializeField] private string configName = "TrafficNodeDataViewerConfig";

        [SerializeField] private string configLoadPath;

        [Tooltip("On/off filtering")]
        [SerializeField] private bool filterNodeData;

        [Tooltip("Show full setting label")]
        [SerializeField] private bool fullLabels = true;

        [Tooltip("Show node traffic data only when the cursor is hovering")]
        [SerializeField] private bool showOnlyOnCursor;

        [Tooltip("TrafficNode filtering by selected parameters")]
        [SerializeField] private bool customParamFilter;

        [Tooltip("On/off select node buttons on the scene")]
        [SerializeField] private bool showSelectButtons;

        [Tooltip("On/off feature to select multiple nodes at the same time")]
        [SerializeField] private bool multipleSelection;

        [SerializeField] private bool showSelectedNodeSettings;

        [SerializeField] private List<string> selectedFilters = new List<string>();

        #endregion

        #region Variables

        private TrafficNode trafficNodePrefab;
        private TrafficNode[] trafficNodes;
        private SerializedObject so;
        private TrafficNode selectedNode;
        private List<TrafficNode> selectedNodes = new List<TrafficNode>();
        private int selectedParamIndex;
        private SceneObjectDataFilter<TrafficNode> sceneDataFilter;

        #endregion

        #region Properties

        protected override Vector2 GetDefaultWindowSize()
        {
            return new Vector2(350, 500);
        }

        #endregion

        #region Constructor


        [MenuItem(CityEditorBookmarks.CITY_WINDOW_PATH + "TrafficNode Data Viewer")]
        public static TrafficNodeDataViewer ShowWindow()
        {
            TrafficNodeDataViewer viewer = (TrafficNodeDataViewer)GetWindow(typeof(TrafficNodeDataViewer));
            viewer.titleContent = new GUIContent("TrafficNode Data Viewer");
            return viewer;
        }

        #endregion

        #region Unity lifecycle

        protected override void OnEnable()
        {
            base.OnEnable();

            trafficNodes = ObjectUtils.FindObjectsOfType<TrafficNode>();
            so = new SerializedObject(this);

            LoadData();

            LoadPrefab();

            if (sceneDataFilter == null)
            {
                sceneDataFilter = new SceneObjectDataFilter<TrafficNode>();
            }

            if (string.IsNullOrEmpty(configLoadPath))
            {
                configLoadPath = CityEditorBookmarks.CITY_EDITOR_CONFIGS_PATH;
            }

            if (trafficNodeDataViewerConfig == null)
            {
                TryToLoadConfig();
            }
            else
            {
                SetupConfig();
            }

            OnRefreshClick();
            SceneView.duringSceneGui += SceneView_duringSceneGui;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            SaveData();
            SceneView.duringSceneGui -= SceneView_duringSceneGui;
        }

        private void OnGUI()
        {
            so.Update();

            if (trafficNodeDataViewerConfig == null)
            {
                DrawAssigments();
                EditorGUILayout.PropertyField(so.FindProperty(nameof(configName)));
                EditorGUILayout.PropertyField(so.FindProperty(nameof(configLoadPath)));

                EditorGUILayout.HelpBox("Config not found!", MessageType.Error);
                so.ApplyModifiedProperties();

                if (GUILayout.Button("Load"))
                {
                    TryToLoadConfig();
                }

                return;
            }

            ShowCommonSettings();

            ShowVisualSettings();

            ShowSettings();

            so.ApplyModifiedProperties();

            if (GUILayout.Button("Refresh"))
            {
                OnRefreshClick();
            }
        }

        private void DrawAssigments()
        {
            trafficNodePrefab = EditorGUILayout.ObjectField("Traffic Node Prefab", trafficNodePrefab, typeof(TrafficNode), false) as TrafficNode;

            EditorGUILayout.PropertyField(so.FindProperty(nameof(trafficNodeDataViewerConfig)));
        }

        private void SetupConfig()
        {
            if (trafficNodeDataViewerConfig == null)
            {
                return;
            }

            sceneDataFilter.SetupConfig(trafficNodePrefab, trafficNodeDataViewerConfig);
        }

        private TrafficNode GetCurrentSelectedNode()
        {
            if (!multipleSelection)
            {
                return selectedNode;
            }
            else
            {
                for (int i = 0; i < selectedNodes.Count; i++)
                {
                    if (selectedNodes[i] != null)
                    {
                        return selectedNodes[i];
                    }
                }
            }

            return null;
        }

        private void LoadPrefab()
        {
            if (trafficNodePrefab != null)
            {
                return;
            }

            var trafficNodeObj = AssetDatabase.LoadAssetAtPath(CityEditorBookmarks.TRAFFIC_NODE_PREFAB_PATH, typeof(GameObject));

            if (trafficNodeObj != null)
            {
                trafficNodePrefab = (trafficNodeObj as GameObject).GetComponent<TrafficNode>();
            }

            if (trafficNodePrefab)
            {
                return;
            }

            foreach (var trafficNode in trafficNodes)
            {
                var tempTrafficNodePrefab = PrefabUtility.GetCorrespondingObjectFromSource(trafficNode.gameObject);
                var outer = PrefabUtility.GetOutermostPrefabInstanceRoot(trafficNode);

                if (tempTrafficNodePrefab != outer)
                {
                    continue;
                }

                if (tempTrafficNodePrefab != null)
                {
                    trafficNodePrefab = tempTrafficNodePrefab.GetComponent<TrafficNode>();
                    break;
                }
            }
        }

        private void TryToLoadConfig()
        {
            var loadPath = AssetDatabaseExtension.GetAssetSavePath(configLoadPath, configName);
            var asset = AssetDatabase.LoadAssetAtPath(loadPath, typeof(ScriptableObject));

            if (asset != null)
            {
                trafficNodeDataViewerConfig = asset as SceneDataViewerConfig;
                SetupConfig();
            }
        }

        #endregion

        #region Methods

        private void ShowCommonSettings()
        {
            Action commonSettingsCallback = () =>
            {
                DocumentationLinkerUtils.ShowButtonFirst(DocLink, 34);
                DrawAssigments();

                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(so.FindProperty(nameof(filterNodeData)));

                if (EditorGUI.EndChangeCheck())
                {
                    so.ApplyModifiedProperties();

                    if (filterNodeData)
                    {
                        OnRefreshClick();
                    }
                    else
                    {
                        RepaintScene();
                    }
                }

                if (filterNodeData)
                {
                    EditorGUILayout.PropertyField(so.FindProperty(nameof(fullLabels)));
                    EditorGUILayout.PropertyField(so.FindProperty(nameof(showOnlyOnCursor)));
                    EditorGUILayout.PropertyField(so.FindProperty(nameof(customParamFilter)));

                    if (customParamFilter)
                    {
                        EditorGUILayout.BeginHorizontal();

                        var paramNames = sceneDataFilter.AvailableParamNames;

                        if (paramNames != null)
                        {
                            selectedParamIndex = EditorGUILayout.Popup("Custom Filter", selectedParamIndex, paramNames);

                            var selectedPopupFilter = paramNames[selectedParamIndex];
                            var contains = selectedFilters.Contains(selectedPopupFilter);

                            if (!contains)
                            {
                                if (GUILayout.Button("+", GUILayout.Width(EditFilterButtonScreenSize)))
                                {
                                    selectedFilters.TryToAdd(selectedPopupFilter);
                                }
                            }
                            else
                            {
                                if (GUILayout.Button("x", GUILayout.Width(EditFilterButtonScreenSize)))
                                {
                                    selectedFilters.TryToRemove(selectedPopupFilter);
                                }
                            }
                        }
                        else
                        {
                            if (trafficNodeDataViewerConfig)
                            {
                                SetupConfig();
                            }
                        }

                        EditorGUILayout.EndHorizontal();

                        GUILayout.BeginVertical("GroupBox");

                        if (selectedFilters.Count > 0)
                        {
                            EditorGUILayout.LabelField("Selected filters:", EditorStyles.boldLabel);

                            for (int i = 0; i < selectedFilters.Count; i++)
                            {
                                EditorGUILayout.BeginHorizontal();

                                string selectedFilter = selectedFilters[i];

                                EditorGUILayout.LabelField($"[{selectedFilter}]");

                                if (GUILayout.Button("x", GUILayout.Width(EditFilterButtonScreenSize)))
                                {
                                    selectedFilters.Remove(selectedFilter);
                                    EditorGUILayout.EndHorizontal();
                                    break;
                                }

                                EditorGUILayout.EndHorizontal();
                            }
                        }
                        else
                        {
                            EditorGUILayout.LabelField("No Filter Selected", EditorStyles.boldLabel);
                        }

                        GUILayout.EndVertical();
                    }
                }
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Common Settings", commonSettingsCallback);
        }

        private void ShowVisualSettings()
        {
            Action visualSettingsCallback = () =>
            {
                EditorGUILayout.PropertyField(so.FindProperty(nameof(showSelectButtons)));

                if (showSelectButtons)
                {
                    EditorGUILayout.PropertyField(so.FindProperty(nameof(multipleSelection)));

                    if (multipleSelection)
                    {
                        GUI.enabled = selectedNodes.Count > 0;

                        if (GUILayout.Button("Clear Selection"))
                        {
                            ClearSelection();
                        }

                        GUI.enabled = true;
                    }
                }
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Visual Settings", visualSettingsCallback);
        }

        private void ShowSettings()
        {
            Action showSettingsCallback = () =>
              {

                  if (!multipleSelection)
                  {
                      if (selectedNode != null && selectedNode.TrafficLightCrossroad != null)
                      {
                          EditorGUILayout.LabelField($"Crossroad: {selectedNode.TrafficLightCrossroad.name}", EditorStyles.boldLabel);
                      }

                      selectedNode = (TrafficNode)EditorGUILayout.ObjectField("Selected Node", selectedNode, typeof(TrafficNode), true);

                      if (selectedNode != null)
                      {
                          TrafficNodeInspectorExtension.DrawInspectorSettings(selectedNode);
                      }
                      else
                      {
                          EditorGUILayout.LabelField("Not selected", EditorStyles.boldLabel);
                      }
                  }
                  else
                  {
                      EditorGUILayout.LabelField($"Selected {selectedNodes.Count} nodes", EditorStyles.boldLabel);

                      var currentNode = GetCurrentSelectedNode();

                      if (currentNode != null)
                      {
                          TrafficNodeInspectorExtension.DrawInspectorSettings(currentNode, selectedNodes);
                      }
                      else
                      {
                          EditorGUILayout.LabelField("Not selected", EditorStyles.boldLabel);
                      }
                  }
              };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Selected Node Settings", showSettingsCallback, ref showSelectedNodeSettings);
        }

        private void OnRefreshClick()
        {
            FilterAllNodes();
        }

        private void FilterAllNodes()
        {
            sceneDataFilter.ClearFilterData();

            for (int i = 0; i < trafficNodes.Length; i++)
            {
                TrafficNode trafficNode = trafficNodes[i];

                if (trafficNode != null)
                {
                    TryToFilterNode(trafficNode);
                }
            }

            RepaintScene();
        }

        private void TryToFilterNode(TrafficNode trafficNode)
        {
            if (trafficNodePrefab == null)
            {
                return;
            }

            if (!customParamFilter)
            {
                var data = trafficNodeDataViewerConfig.VariableDataDict;

                foreach (var item in data)
                {
                    var currentFilter = item.Key;
                    sceneDataFilter.TryToFilterNode(trafficNode, currentFilter);
                }
            }
            else
            {
                foreach (var selectedFilter in selectedFilters)
                {
                    sceneDataFilter.TryToFilterNode(trafficNode, selectedFilter);
                }
            }
        }

        private void RepaintScene()
        {
            SceneView.RepaintAll();
        }

        private void Select(GameObject node)
        {
            var newSelectedNode = node.GetComponent<TrafficNode>();

            if (!multipleSelection)
            {
                selectedNode = newSelectedNode;
            }
            else
            {
                selectedNodes.TryToAdd(newSelectedNode);
            }

            Repaint();
        }

        private void ClearSelection()
        {
            selectedNodes.Clear();
            Repaint();
            RepaintScene();
        }

        private void DrawTrafficNodeCube(TrafficNode trafficNode)
        {
            var pos = trafficNode.transform.position;

            var selectedColor = Color.white;

            if (!multipleSelection)
            {
                if (trafficNode == selectedNode)
                {
                    selectedColor = Color.green;
                }
            }
            else
            {
                if (selectedNodes.Contains(trafficNode))
                {
                    selectedColor = Color.green;
                }
            }

            EditorExtension.DrawSimpleHandlesCube(pos, Vector3.one, selectedColor);
        }

        #endregion

        #region Event Handlers

        private void SceneView_duringSceneGui(SceneView obj)
        {
            if (filterNodeData)
            {
                System.Action<GameObject> callback = null;

                if (showSelectButtons)
                {
                    callback = Select;
                }

                foreach (var node in sceneDataFilter.FilteredData)
                {
                    if (node.Key == null)
                    {
                        continue;
                    }

                    DrawTrafficNodeCube(node.Key);
                    SceneDataGuiViewPopup.DrawInfo(node.Key.gameObject, node.Value, fullLabels, showOnlyOnCursor, callback);
                }
            }
            else
            {
                for (int i = 0; i < trafficNodes.Length; i++)
                {
                    TrafficNode trafficNode = trafficNodes[i];

                    if (trafficNode == null)
                    {
                        continue;
                    }

                    DrawTrafficNodeCube(trafficNode);

                    if (showSelectButtons)
                    {
                        Action selectCallback = () =>
                        {
                            Select(trafficNode.gameObject);
                        };

                        EditorExtension.DrawButton("T", trafficNode.transform.position, 50f, selectCallback);
                    }
                }
            }
        }

        #endregion
    }
}
