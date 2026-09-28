#if UNITY_EDITOR
using Spirit604.Attributes;
using Spirit604.CityEditor.Road;
using Spirit604.CityEditor.Road.Debug;
using Spirit604.Extensions;
using Spirit604.Gameplay.Config.Road;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Spirit604.CityEditor.Level
{
    public class GlobalTrafficLightSettingsWindow : EditorWindowBase
    {
        #region Consts

        private const string DocLink = "https://dotstrafficcity.readthedocs.io/en/latest/trafficLight.html#global-lights-settings";

        private const float PrefixLabelOffset = 150f;
        private const float GUIButtonWidth = 50f;

        private const string RemoveLightHandlerButtonText = "H-";
        private const string RemoveLightButtonText = "L-";
        private const string RemovePedestrianNodeButtonText = "P-";
        private const string RemoveTrafficNodeButtonText = "T-";

        private readonly string[] SelectLightHandlerTexts = { "H0", "H1", "H2", "H3", "H4", "H5", "H6", "H7", "H8", "H9", "H" };
        private readonly string[] SelectLightTexts = { "L0", "L1", "L2", "L3", "L4", "L5", "L6", "L7", "L8", "L9", "L" };
        private readonly string[] SelectPedestrianNodeTexts = { "P0", "P1", "P2", "P3", "P4", "P5", "P6", "P7", "P8", "P9", "P" };
        private readonly string[] SelectTrafficNodeTexts = { "T0", "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T" };

        #endregion

        #region Inspector variables

        [SerializeField] private SharedLightStateContainer globalLightStates;

        [Tooltip("Automatically creates new TrafficLightHandler if required according to new state container")]
        [SerializeField] private bool autoSyncHandlers = true;

        [Tooltip("Aplying crossroad prefab with new state container")]
        [SerializeField] private bool applyPrefab = true;

        [Tooltip("Move the SceneView camera to the selected traffic light crossroad when you select")]
        [SerializeField] private bool focusOnSelect = true;

        [Tooltip("Show enabled traffic light data on the scene")]
        [SerializeField] private bool showWorldInfo = true;

        [Tooltip("Show all traffic light data (include disabled) on the scene")]
        [SerializeField] private bool showDisabledLights;

        [Tooltip("On/off light connections on the scene")]
        [SerializeField] private bool showLightConnections;

        [Tooltip("Auto unselect TrafficLightHandler when connecting TrafficLightHandler traffic lights to any object")]
        [SerializeField] private bool autoUnselectHandler = true;

        [Tooltip("Allow index traffic light overrides in traffic light objects")]
        [SerializeField] private bool allowOverrideLightIndex;

        [Tooltip("Traffic light object will be a child of the connected crossroad")]
        [SerializeField] private bool reparentLight;

        [Tooltip("" +
            "<b>All</b> : show all connection types\r\n\r\n" +
            "<b>Traffic node</b> : show traffic node connection only\r\n\r\n" +
            "<b>Pedestrian node</b> : show pedestrian node connection only\r\n\r\n" +
            "<b>Light</b> : show light object connection only")]
        [SerializeField] private TrafficLightHandler.ShowLightConnectionType lightConnectionType = TrafficLightHandler.ShowLightConnectionType.Light;

        [Tooltip("Show connection buttons for selected Light connection type")]
        [SerializeField] private bool showConnectionButtons;

        [Tooltip("Objects with a selected light index are displayed (-1 value - all indexes are displayed)")]
        [SerializeField][Range(-1, 9)] private int lightIndex = -1;

        [SerializeField] private Color lightConnectionColor = Color.white;
        [SerializeField] private bool commonSettingsFoldout = true;
        [SerializeField] private bool connectionSettingsFoldout = true;

        #endregion

        #region Variables

        private SharedLightStateContainer newTrafficCrossRoadSettings;
        private bool showNewSettings;
        private GUIStyle lightHeaderStyle = new GUIStyle();
        private Vector2 scrollPosition;
        private GUIStyle timeLineStyle;
        private SerializedObject so;

        private TrafficLightCrossroad[] trafficLightCrossRoads;
        private List<TrafficLightHandler> trafficLightHandlers;
        private List<TrafficLightObject> trafficLights;
        private List<TrafficNode> trafficNodes;
        private List<PedestrianNode> pedestrianNodes;
        private List<GameObject> processedPrefabs = new List<GameObject>();

        private TrafficLightHandler selectedTrafficLightHandler;
        private TrafficLightObject selectedTrafficLightObject;
        private int selectedFrameIndex;
        private TrafficLightFrameBase selectedTrafficLightFrame;
        private TrafficNode selectedTrafficNode;
        private PedestrianNode selectedPedestrianNode;

        private List<string> lightHandlersCrossroadTexts = new List<string>();
        private List<Action> lightHandlersCrossroadCallbacks = new List<Action>();
        private Dictionary<int, TrafficLightCrossroad> lightBinding;

        private TrafficLightObject[] corruptedLightObjects;
        private GameObject roadParent;

        #endregion

        #region Properties

        private bool ShowConnectionButtons => showLightConnections && showConnectionButtons;
        protected override Vector2 GetDefaultWindowSize() => new Vector2(550, 600);

        #endregion

        #region Constructor

        [MenuItem(CityEditorBookmarks.CITY_WINDOW_PATH + "Global Traffic Light Settings")]
        public static GlobalTrafficLightSettingsWindow ShowWindow()
        {
            GlobalTrafficLightSettingsWindow globalTrafficLightWindow = (GlobalTrafficLightSettingsWindow)GetWindow(typeof(GlobalTrafficLightSettingsWindow));
            globalTrafficLightWindow.titleContent = new GUIContent("Global Traffic Light Settings");

            return globalTrafficLightWindow;
        }

        #endregion

        #region Unity lifecycle

        protected override void OnEnable()
        {
            base.OnEnable();

            lightHeaderStyle = new GUIStyle();
            lightHeaderStyle.fontSize = 18;
            lightHeaderStyle.fontStyle = FontStyle.Bold;
            lightHeaderStyle.normal.textColor = EditorStyles.label.normal.textColor;

            so = new SerializedObject(this);

            InitTimelineStyle();

            LoadData();

            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            InitSceneObjects(prefabStage);

            SceneView.duringSceneGui += SceneView_duringSceneGui;
            PrefabStage.prefabStageOpened += PrefabStage_prefabStageOpened;
            PrefabStage.prefabStageClosing += PrefabStage_prefabStageClosing;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SaveData();
            SceneView.duringSceneGui -= SceneView_duringSceneGui;
            PrefabStage.prefabStageOpened -= PrefabStage_prefabStageOpened;
            PrefabStage.prefabStageClosing -= PrefabStage_prefabStageClosing;
        }

        private void OnGUI()
        {
            if (trafficLightCrossRoads == null || trafficLightCrossRoads.Length == 0)
            {
                EditorGUILayout.LabelField("Crossroads not found", lightHeaderStyle);
                return;
            }

            EditorGUILayout.BeginVertical("GroupBox");

            DocumentationLinkerUtils.ShowButtonFirst(DocLink, xOffset: 70);

            EditorGUILayout.LabelField("Global Light Settings", lightHeaderStyle);
            EditorGUILayout.Separator();

            so.Update();

            EditorGUILayout.Separator();

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.PropertyField(so.FindProperty(nameof(globalLightStates)), true);

            if (GUILayout.Button("*", GUILayout.Width(25f)))
            {
                showNewSettings = !showNewSettings;
            }

            EditorGUILayout.EndHorizontal();

            if (showNewSettings)
            {
                EditorGUILayout.BeginHorizontal();

                newTrafficCrossRoadSettings = (SharedLightStateContainer)EditorGUILayout.ObjectField("New Settings", newTrafficCrossRoadSettings, typeof(SharedLightStateContainer), false);

                GUI.enabled = newTrafficCrossRoadSettings && newTrafficCrossRoadSettings != globalLightStates;

                if (GUILayout.Button("Replace", GUILayout.Width(60f)))
                {
                    ReplaceSettings();
                }

                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(so.FindProperty(nameof(autoSyncHandlers)));
                EditorGUILayout.PropertyField(so.FindProperty(nameof(applyPrefab)));
            }

            EditorGUILayout.Separator();

            Action commonSettingsContent = () =>
                {
                    EditorGUILayout.PropertyField(so.FindProperty(nameof(focusOnSelect)));

                    EditorGUI.BeginChangeCheck();

                    EditorGUILayout.PropertyField(so.FindProperty(nameof(showWorldInfo)));

                    if (showWorldInfo)
                    {
                        EditorGUILayout.PropertyField(so.FindProperty(nameof(showDisabledLights)));
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        SceneView.RepaintAll();
                    }

                    if (corruptedLightObjects?.Length > 0)
                    {
                        if (GUILayout.Button($"Reconnect {corruptedLightObjects.Length} Lights"))
                        {
                            ReconnectLights();
                        }
                    }
                };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Common Settings", commonSettingsContent, ref commonSettingsFoldout);

            Action connectionSettingsContent = () =>
                {
                    EditorGUI.BeginChangeCheck();

                    EditorGUILayout.PropertyField(so.FindProperty(nameof(showLightConnections)));

                    if (showLightConnections)
                    {
                        EditorGUILayout.PropertyField(so.FindProperty(nameof(autoUnselectHandler)));
                        EditorGUILayout.PropertyField(so.FindProperty(nameof(allowOverrideLightIndex)));
                        EditorGUILayout.PropertyField(so.FindProperty(nameof(reparentLight)));
                        EditorGUILayout.PropertyField(so.FindProperty(nameof(lightConnectionType)));
                        EditorGUILayout.PropertyField(so.FindProperty(nameof(showConnectionButtons)));
                        EditorGUILayout.PropertyField(so.FindProperty(nameof(lightIndex)));
                        EditorGUILayout.PropertyField(so.FindProperty(nameof(lightConnectionColor)));
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        so.ApplyModifiedProperties();
                        FindLightHandlers();
                        SceneView.RepaintAll();
                    }
                };

            SharedLightStateContainer trafficCrossRoadSettings = globalLightStates == null ? trafficLightCrossRoads[0].SharedStateContainer : globalLightStates;

            InspectorExtension.DrawDefaultInspectorGroupBlock("Connection Settings", connectionSettingsContent, ref connectionSettingsFoldout);

            EditorGUILayout.EndVertical();
            so.ApplyModifiedProperties();

            EditorGUILayout.BeginVertical("GroupBox");
            EditorGUILayout.LabelField("World Traffic Lights", lightHeaderStyle);
            EditorGUILayout.Separator();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            for (int nodeIndex = 0; nodeIndex < trafficLightCrossRoads?.Length; nodeIndex++)
            {
                if (!trafficLightCrossRoads[nodeIndex].HasLights)
                {
                    continue;
                }

                DrawCrossroadLightInfo(trafficLightCrossRoads[nodeIndex]);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Methods

        private void DrawCrossroadLightInfo(TrafficLightCrossroad trafficLightCrossRoad)
        {
            EditorGUILayout.BeginVertical("GroupBox");
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField(trafficLightCrossRoad.gameObject.name, lightHeaderStyle);
            EditorGUILayout.Separator();

            trafficLightCrossRoad.CustomSettings = EditorGUILayout.Toggle("CustomSettings", trafficLightCrossRoad.CustomSettings);

            float sliderWidth = position.width - 100f;

            InitTimelineStyle();

            if (!trafficLightCrossRoad.CustomSettings)
            {
                TrafficLightTimingDrawerUtils.DrawCrossroadSignalTimings(trafficLightCrossRoad, timeLineStyle, sliderWidth, PrefixLabelOffset);
            }
            else
            {
                var hasNullHandlers = trafficLightCrossRoad.HasNullTrafficLightHandler();

                if (!hasNullHandlers)
                {
                    TrafficLightTimingDrawerUtils.DrawCrossroadSignalTimings(trafficLightCrossRoad, timeLineStyle, sliderWidth, PrefixLabelOffset);
                }
                else
                {
                    EditorGUILayout.HelpBox("One or more assigned TrafficLightHandler is null", MessageType.Error);
                }
            }

            if (trafficLightCrossRoad.CustomArrowLights.Count > 0)
            {
                EditorGUILayout.Separator();

                for (int i = 0; i < trafficLightCrossRoad.CustomArrowLights.Count; i++)
                {
                    var customArrowLight = trafficLightCrossRoad.CustomArrowLights[i];

                    if (customArrowLight == null)
                    {
                        continue;
                    }

                    EditorGUILayout.LabelField("Additional Custom light " + (i + 1).ToString(), EditorStyles.boldLabel);

                    if (customArrowLight.path != null &&
                        customArrowLight.currentTrafficLightHandler != null &&
                        customArrowLight.relatedTrafficLightHandler != null)
                    {
                        customArrowLight.startTimeOffset = EditorGUILayout.Slider("StartTimeOffset", customArrowLight.startTimeOffset, -200f, 200f);
                        customArrowLight.enabledDuration = EditorGUILayout.Slider("EnabledDuration", customArrowLight.enabledDuration, 0f, 200f);
                    }
                    else
                    {
                        if (customArrowLight.path == null)
                        {
                            EditorGUILayout.LabelField("Path error");
                        }

                        if (customArrowLight.relatedTrafficLightHandler == null)
                        {
                            EditorGUILayout.LabelField("RelatedTrafficLightHandler error");
                        }

                        if (customArrowLight.currentTrafficLightHandler == null)
                        {
                            EditorGUILayout.LabelField("CurrentTrafficLightHandler error");
                        }
                    }

                    EditorGUILayout.Separator();
                }
            }

            if (GUILayout.Button("Select"))
            {
                Selection.activeObject = trafficLightCrossRoad;

                if (focusOnSelect)
                {
                    Vector3 focusPosition = trafficLightCrossRoad.transform.position;
                    SceneView.lastActiveSceneView.LookAt(focusPosition);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void FindLightHandlers()
        {
            trafficLightHandlers = null;

            if (!roadParent) return;

            if (lightIndex != -1)
            {
                trafficLightHandlers = roadParent.GetComponentsInChildren<TrafficLightHandler>().Where(item => item.RelatedLightIndex == lightIndex).ToList();
            }
            else
            {
                trafficLightHandlers = roadParent.GetComponentsInChildren<TrafficLightHandler>().ToList();
            }
        }

        private void ReplaceSettings()
        {
            int count = 0;
            processedPrefabs.Clear();
            Undo.RegisterCompleteObjectUndo(this, "Undo settings");

            for (int i = 0; i < trafficLightCrossRoads?.Length; i++)
            {
                if (trafficLightCrossRoads[i].SharedStateContainer != null && trafficLightCrossRoads[i].SharedStateContainer != globalLightStates)
                {
                    continue;
                }

                bool prefabIsAvailable = true;

                if (applyPrefab)
                {
                    var prefab = PrefabUtility.GetCorrespondingObjectFromSource(trafficLightCrossRoads[i].gameObject);

                    if (prefab)
                    {
                        if (!processedPrefabs.Contains(prefab))
                        {
                            processedPrefabs.Add(prefab);
                        }
                        else
                        {
                            prefabIsAvailable = false;
                        }
                    }
                }

                if (!prefabIsAvailable)
                {
                    continue;
                }

                count++;
                trafficLightCrossRoads[i].SetSettings(newTrafficCrossRoadSettings, autoSyncHandlers, applyPrefab: applyPrefab);
            }

            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

            showNewSettings = false;
            globalLightStates = newTrafficCrossRoadSettings;
            newTrafficCrossRoadSettings = null;

            Debug.Log($"GlobalTrafficLightSettings. {count} containers replaced.");
        }

        private void ReconnectLights()
        {
            for (int i = 0; i < corruptedLightObjects?.Length; i++)
            {
                var trafficLight = corruptedLightObjects[i];

                if (trafficLight == null) continue;

                if (lightBinding.ContainsKey(trafficLight.ConnectedId))
                {
                    if (trafficLight.TrafficLightCrossroad == null)
                    {
                        trafficLight.AssignCrossRoad(lightBinding[trafficLight.ConnectedId]);
                        Debug.Log($"GlobalTrafficLightSettings. TrafficLight '{trafficLight.name}' InstanceID {trafficLight.GetInstanceID()} reconnected.{TrafficObjectFinderMessage.GetMessage()}");
                    }
                }
                else
                {
                    Debug.Log($"GlobalTrafficLightSettings. TrafficLight '{trafficLight.name}' InstanceID {trafficLight.GetInstanceID()} crossroad ID {trafficLight.ConnectedId} not found on the scene.{TrafficObjectFinderMessage.GetMessage()}");
                }
            }

            corruptedLightObjects = null;
        }

        #endregion

        #region SceneView methods

        private void DrawLightStates()
        {
            for (int nodeIndex = 0; nodeIndex < trafficLightCrossRoads?.Length; nodeIndex++)
            {
                if (!trafficLightCrossRoads[nodeIndex])
                {
                    continue;
                }

                TrafficLightCrossroadInfoWorldGuiRectDrawer.DrawInfo(trafficLightCrossRoads[nodeIndex], trafficLightCrossRoads[nodeIndex].transform.position, showDisabledLights);
            }
        }

        private void DrawConnectionLightButtons()
        {
            ShowTrafficLightHandlerButtons();

            ShowLightButtons();

            ShowTrafficNodeButtons();

            ShowPedestrianNodeButtons();

            TryToProcessLightConnection();

            TryToProcessTrafficNodeConnection();

            TryToProcessPedestrianNodeConnection();
        }

        private void ShowTrafficLightHandlerButtons()
        {
            if (selectedTrafficLightHandler == null)
            {
                for (int i = 0; i < trafficLightCrossRoads?.Length; i++)
                {
                    if (trafficLightCrossRoads[i] == null)
                    {
                        continue;
                    }

                    var shouldShow = trafficLightCrossRoads[i].HasLights || showDisabledLights;

                    if (!shouldShow)
                    {
                        continue;
                    }

                    if (lightIndex == -1)
                    {
                        lightHandlersCrossroadTexts.Clear();
                        lightHandlersCrossroadCallbacks.Clear();

                        var crossroadPosition = trafficLightCrossRoads[i].transform.position;

                        foreach (var trafficLightHandlerData in trafficLightCrossRoads[i].TrafficLightHandlers)
                        {
                            var trafficLightHandler = trafficLightHandlerData.Value;

                            if (!trafficLightHandler)
                            {
                                continue;
                            }

                            var lightIndex = trafficLightHandlerData.Key;

                            var text = GetSelectButtonLabelText(TrafficLightHandler.ShowLightConnectionType.All, lightIndex);
                            lightHandlersCrossroadTexts.Add(text);
                            lightHandlersCrossroadCallbacks.Add(() => { selectedTrafficLightHandler = trafficLightHandler; });
                        }

                        EditorExtension.DrawButtons(lightHandlersCrossroadTexts, crossroadPosition, GUIButtonWidth, lightHandlersCrossroadCallbacks, centralizeGuiAlign: true);
                    }
                    else
                    {
                        if (trafficLightCrossRoads[i].TrafficLightHandlers.TryGetValue(lightIndex, out var trafficLightHandler))
                        {
                            var text = GetSelectButtonLabelText(TrafficLightHandler.ShowLightConnectionType.All, lightIndex);
                            Action addHandlerCallback = () => { selectedTrafficLightHandler = trafficLightHandler; };

                            EditorExtension.DrawButton(text, trafficLightHandler.transform.position, GUIButtonWidth, addHandlerCallback, centralizeGuiAlign: true);
                        }
                    }
                }
            }
            else
            {
                Action removeCallback = () => { selectedTrafficLightHandler = null; };

                EditorExtension.DrawButton(RemoveLightHandlerButtonText, selectedTrafficLightHandler.transform.position, GUIButtonWidth, removeCallback, centralizeGuiAlign: true);
            }
        }

        private void ShowLightButtons()
        {
            if (!lightConnectionType.HasFlag(TrafficLightHandler.ShowLightConnectionType.Light))
            {
                return;
            }

            if (selectedTrafficLightObject == null)
            {
                for (int i = 0; i < trafficLights?.Count; i++)
                {
                    var lightObject = trafficLights[i];

                    if (lightObject == null)
                    {
                        continue;
                    }

                    foreach (var framesData in lightObject.TrafficLightFrames)
                    {
                        var frames = framesData.Value;
                        var relatedLightIndex = framesData.Key;

                        for (int j = 0; j < frames.TrafficLightFrames?.Count; j++)
                        {
                            var frame = frames.TrafficLightFrames[j];

                            if (frame == null)
                            {
                                continue;
                            }

                            bool shouldShow = lightIndex == -1 || lightObject.FrameHasIndex(frame, lightIndex);

                            if (!shouldShow)
                                continue;

                            var position = frame.GetIndexPosition();

                            string buttonText = GetSelectButtonLabelText(TrafficLightHandler.ShowLightConnectionType.Light, relatedLightIndex);

                            Action addLightCallback = () =>
                            {
                                selectedTrafficLightObject = lightObject;
                                selectedFrameIndex = relatedLightIndex;
                                selectedTrafficLightFrame = frame;
                            };

                            EditorExtension.DrawButton(buttonText, position, GUIButtonWidth, addLightCallback, centralizeGuiAlign: true);
                        }
                    }
                }
            }
            else
            {
                Action removeCallback = () => { selectedTrafficLightObject = null; selectedFrameIndex = -1; selectedTrafficLightFrame = null; };

                EditorExtension.DrawButton(RemoveLightButtonText, selectedTrafficLightObject.transform.position, GUIButtonWidth, removeCallback, centralizeGuiAlign: true);
            }
        }

        private void ShowTrafficNodeButtons()
        {
            if (!lightConnectionType.HasFlag(TrafficLightHandler.ShowLightConnectionType.TrafficNode))
            {
                return;
            }

            if (selectedTrafficNode == null)
            {
                for (int i = 0; i < trafficNodes?.Count; i++)
                {
                    var trafficNode = trafficNodes[i];

                    if (trafficNode == null)
                    {
                        continue;
                    }

                    int trafficNodeIndex = -1;

                    if (trafficNode.TrafficLightHandler != null)
                    {
                        trafficNodeIndex = trafficNode.TrafficLightHandler.RelatedLightIndex;
                    }

                    bool shouldShow = lightIndex == -1 || lightIndex == trafficNodeIndex || trafficNodeIndex == -1;

                    if (shouldShow)
                    {
                        string buttonText = GetSelectButtonLabelText(TrafficLightHandler.ShowLightConnectionType.TrafficNode, trafficNodeIndex);

                        Action addLightCallback = () => { selectedTrafficNode = trafficNode; };
                        EditorExtension.DrawButton(buttonText, trafficNode.transform.position, GUIButtonWidth, addLightCallback, centralizeGuiAlign: true);
                    }
                }
            }
            else
            {
                Action removeCallback = () => { selectedTrafficNode = null; };

                EditorExtension.DrawButton(RemoveTrafficNodeButtonText, selectedTrafficNode.transform.position, GUIButtonWidth, removeCallback, centralizeGuiAlign: true);
            }
        }

        private void ShowPedestrianNodeButtons()
        {
            if (!lightConnectionType.HasFlag(TrafficLightHandler.ShowLightConnectionType.PedestrianNode))
            {
                return;
            }

            if (selectedPedestrianNode == null)
            {
                for (int i = 0; i < pedestrianNodes?.Count; i++)
                {
                    var pedestrianNode = pedestrianNodes[i];

                    if (pedestrianNode == null)
                    {
                        continue;
                    }

                    int trafficNodeIndex = -1;

                    if (pedestrianNode.RelatedTrafficLightHandler != null)
                    {
                        trafficNodeIndex = pedestrianNode.RelatedTrafficLightHandler.RelatedLightIndex;
                    }

                    bool shouldShow = lightIndex == -1 || lightIndex == trafficNodeIndex || trafficNodeIndex == -1;

                    if (shouldShow)
                    {
                        string buttonText = GetSelectButtonLabelText(TrafficLightHandler.ShowLightConnectionType.PedestrianNode, trafficNodeIndex);

                        Action addLightCallback = () => { selectedPedestrianNode = pedestrianNode; };
                        EditorExtension.DrawButton(buttonText, pedestrianNode.transform.position, GUIButtonWidth, addLightCallback, centralizeGuiAlign: true);
                    }
                }
            }
            else
            {
                Action removeCallback = () => { selectedPedestrianNode = null; };
                EditorExtension.DrawButton(RemovePedestrianNodeButtonText, selectedPedestrianNode.transform.position, GUIButtonWidth, removeCallback, centralizeGuiAlign: true);
            }
        }

        private void TryToProcessLightConnection()
        {
            if (selectedTrafficLightHandler != null && selectedTrafficLightObject != null)
            {
                var relatedLightIndex = selectedTrafficLightHandler.RelatedLightIndex;

                bool lightIsConnected = false;

                var hasIndex = selectedTrafficLightObject.FrameHasIndex(selectedTrafficLightFrame, relatedLightIndex);

                if (hasIndex)
                {
                    AddFrame(selectedTrafficLightFrame);
                    lightIsConnected = true;
                }
                else if (allowOverrideLightIndex)
                {
                    lightIsConnected = selectedTrafficLightObject.ChangeFrameIndex(selectedTrafficLightFrame, selectedFrameIndex, relatedLightIndex);

                    if (lightIsConnected)
                    {
                        AddFrame(selectedTrafficLightFrame);
                    }
                }

                if (!lightIsConnected)
                {
                    Debug.Log($"Frame RelatedLightIndex {relatedLightIndex} not found! TrafficLightObject: '{selectedTrafficLightObject.name}' TrafficLightHandler: '{selectedTrafficLightHandler.name}'");
                }

                if (autoUnselectHandler)
                {
                    selectedTrafficLightHandler = null;
                }

                selectedFrameIndex = -1;
                selectedTrafficLightObject = null;
                selectedTrafficLightFrame = null;
            }
        }

        private void TryToProcessTrafficNodeConnection()
        {
            if (selectedTrafficLightHandler != null && selectedTrafficNode != null)
            {
                selectedTrafficNode.TrafficLightCrossroad?.RemoveNode(selectedTrafficNode);

                selectedTrafficLightHandler.AddNode(selectedTrafficNode);

                if (autoUnselectHandler)
                {
                    selectedTrafficLightHandler = null;
                }

                selectedTrafficNode = null;
            }
        }

        private void TryToProcessPedestrianNodeConnection()
        {
            if (selectedTrafficLightHandler != null && selectedPedestrianNode != null)
            {
                selectedPedestrianNode.RelatedTrafficLightHandler = selectedTrafficLightHandler;
                EditorSaver.SetObjectDirty(selectedPedestrianNode);

                if (autoUnselectHandler)
                {
                    selectedTrafficLightHandler = null;
                }

                selectedPedestrianNode = null;
            }
        }

        private void AddFrame(TrafficLightFrameBase frame)
        {
            selectedTrafficLightHandler.AddCustomTrafficLight(frame, reparentLight);
        }

        private string GetSelectButtonLabelText(TrafficLightHandler.ShowLightConnectionType connectionType, int index)
        {
            switch (connectionType)
            {
                case TrafficLightHandler.ShowLightConnectionType.All: // TrafficLightHandler
                    {
                        if (index >= 0 && SelectLightHandlerTexts.Length > index)
                        {
                            return SelectLightHandlerTexts[index];
                        }
                        else
                        {
                            return SelectLightHandlerTexts[SelectLightHandlerTexts.Length - 1];
                        }
                    }
                case TrafficLightHandler.ShowLightConnectionType.Light:
                    {
                        if (index >= 0 && SelectLightTexts.Length > index)
                        {
                            return SelectLightTexts[index];
                        }
                        else
                        {
                            return SelectLightTexts[SelectLightTexts.Length - 1];
                        }
                    }
                case TrafficLightHandler.ShowLightConnectionType.TrafficNode:
                    {
                        if (index >= 0 && SelectTrafficNodeTexts.Length > index)
                        {
                            return SelectTrafficNodeTexts[index];
                        }
                        else
                        {
                            return SelectTrafficNodeTexts[SelectTrafficNodeTexts.Length - 1];
                        }
                    }
                case TrafficLightHandler.ShowLightConnectionType.PedestrianNode:
                    {
                        if (index >= 0 && SelectPedestrianNodeTexts.Length > index)
                        {
                            return SelectPedestrianNodeTexts[index];
                        }
                        else
                        {
                            return SelectPedestrianNodeTexts[SelectPedestrianNodeTexts.Length - 1];
                        }
                    }
            }

            return default;
        }

        private void DrawLightHandlers()
        {
            for (int i = 0; i < trafficLightHandlers?.Count; i++)
            {
                TrafficLightHandler lightHandler = trafficLightHandlers[i];

                if (lightHandler == null)
                {
                    continue;
                }

                TrafficLightHandlerEditorExtension.DrawLightConnections(lightHandler, lightConnectionType, lightConnectionColor, showConnectionButtons);
            }

            if (lightConnectionType.HasFlag(TrafficLightHandler.ShowLightConnectionType.Light))
            {
                for (int i = 0; i < trafficLights?.Count; i++)
                {
                    TrafficLightObject trafficLight = trafficLights[i];

                    bool shouldShow = trafficLight && trafficLight.gameObject.activeInHierarchy && (lightIndex == -1 || trafficLight.HasLightIndex(lightIndex));

                    if (shouldShow)
                    {
                        TrafficLightHandlerEditorExtension.DrawLightObjectBounds(trafficLight.transform.position);
                    }
                }
            }

            if (lightConnectionType.HasFlag(TrafficLightHandler.ShowLightConnectionType.PedestrianNode))
            {
                for (int i = 0; i < pedestrianNodes?.Count; i++)
                {
                    var pedestrianNode = pedestrianNodes[i];

                    bool shouldShow = pedestrianNode && pedestrianNode.gameObject.activeInHierarchy && (lightIndex == -1 || (pedestrianNode.RelatedTrafficLightHandler != null && pedestrianNode.RelatedTrafficLightHandler.RelatedLightIndex == lightIndex));

                    if (shouldShow)
                    {
                        TrafficLightHandlerEditorExtension.DrawPedestrianNodeConnection(pedestrianNode);
                    }
                }
            }
        }

        private void InitTimelineStyle()
        {
            if (timeLineStyle != null)
            {
                return;
            }

            timeLineStyle = TrafficLightTimingDrawerUtils.GetDefaultTimelineStyle();
        }

        private void InitSceneObjects(PrefabStage prefabStage)
        {
            roadParent = null;

            if (prefabStage != null)
            {
                roadParent = EditorExtension.GetPrefabStageRoot(prefabStage);

                trafficLights = roadParent.GetComponentsInChildren<TrafficLightObject>().ToList();
                trafficNodes = roadParent.GetComponentsInChildren<TrafficNode>().ToList();
                pedestrianNodes = roadParent.GetComponentsInChildren<PedestrianNode>().ToList();
            }
            else
            {
                trafficLights = ObjectUtils.FindObjectsOfType<TrafficLightObject>().ToList();
                trafficNodes = ObjectUtils.FindObjectsOfType<TrafficNode>().ToList();
                pedestrianNodes = ObjectUtils.FindObjectsOfType<PedestrianNode>().ToList();
            }

            if (roadParent == null)
            {
                var roadNodeParents = ObjectUtils.FindObjectsOfType<RoadParent>();

                if (roadNodeParents?.Length > 0)
                {
                    roadParent = roadNodeParents[0].gameObject;
                }
            }

            FindLightHandlers();

            if (roadParent)
            {
                trafficLightCrossRoads = roadParent.GetComponentsInChildren<TrafficLightCrossroad>();
                lightBinding = new Dictionary<int, TrafficLightCrossroad>();

                foreach (var crossroad in trafficLightCrossRoads)
                {
                    if (!lightBinding.ContainsKey(crossroad.UniqueId))
                        lightBinding.Add(crossroad.UniqueId, crossroad);
                }

                corruptedLightObjects = null;

                if (prefabStage == null)
                {
                    corruptedLightObjects = ObjectUtils.FindObjectsOfType<TrafficLightObject>().Where(
                        a => a.gameObject.scene.isSubScene &&
                        a.ConnectedId != 0 &&
                        a.TrafficLightCrossroad == null &&
                        lightBinding.ContainsKey(a.ConnectedId)).ToArray();
                }

                if (globalLightStates == null && trafficLightCrossRoads.Length > 0)
                    globalLightStates = trafficLightCrossRoads.Where(item => item.SharedStateContainer != null).FirstOrDefault().SharedStateContainer;
            }

            Repaint();
        }

        #endregion

        #region Event handlers

        private void SceneView_duringSceneGui(SceneView obj)
        {
            if (showWorldInfo)
            {
                DrawLightStates();
            }

            if (showLightConnections)
            {
                DrawLightHandlers();
            }

            if (ShowConnectionButtons)
            {
                DrawConnectionLightButtons();
            }
        }

        private void PrefabStage_prefabStageOpened(PrefabStage obj)
        {
            InitSceneObjects(obj);
        }

        private void PrefabStage_prefabStageClosing(PrefabStage obj)
        {
            InitSceneObjects(null);
        }

        #endregion
    }
}
#endif