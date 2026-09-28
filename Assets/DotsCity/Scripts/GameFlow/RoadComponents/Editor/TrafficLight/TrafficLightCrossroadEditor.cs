#if UNITY_EDITOR
using Spirit604.Attributes;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(TrafficLightCrossroad))]
    public class TrafficLightCrossroadEditor : Editor
    {
        private const string DocLink = "https://dotstrafficcity.readthedocs.io/en/latest/roadSegment.html#trafficlightcrossroad";

        private SerializedProperty sharedStateContainerProp;
        private SerializedProperty trafficSegmentConfigProp;
        private SerializedProperty trafficLightHandlerProp;
        private SerializedProperty trafficLightParentProp;
        private SerializedProperty pedestrianLightParentProp;
        private SerializedProperty trafficLightHandlerDataProp;
        private SerializedProperty trafficNodesProp;
        private SerializedProperty customLightSettingsProp;
        private SerializedProperty selectedPathProp;
        private GUIContent idLabel;
        private GUIStyle timeLineStyle;
        private bool foundPathMessage;
        private bool showFoundPathMessage;
        private bool showLinks;
        private int selectedIndexList;

        private float prefixLabelOffset = 100f;
        private float inspectorWidth = 200f;
        private List<ReorderableList> reorderableLists = new List<ReorderableList>();
        private string[] lightHeaders;

        private TrafficLightCrossroad trafficLightCrossroad;

        private TrafficLightCrossroad TrafficLightCrossroad
        {
            get
            {
                if (!trafficLightCrossroad)
                {
                    trafficLightCrossroad = target as TrafficLightCrossroad;
                }

                return trafficLightCrossroad;
            }
        }

        private void OnEnable()
        {
            sharedStateContainerProp = serializedObject.FindProperty("sharedStateContainer");
            trafficSegmentConfigProp = serializedObject.FindProperty("trafficSegmentConfig");
            trafficLightHandlerProp = serializedObject.FindProperty("trafficLightHandlerParent");
            trafficLightParentProp = serializedObject.FindProperty("trafficLightParent");
            pedestrianLightParentProp = serializedObject.FindProperty("pedestrianLightParent");
            trafficNodesProp = serializedObject.FindProperty("trafficNodes");
            trafficLightHandlerDataProp = serializedObject.FindProperty("trafficLightHandlerData");
            customLightSettingsProp = serializedObject.FindProperty("customArrowLights");
            selectedPathProp = serializedObject.FindProperty("selectedPath");

            idLabel = new GUIContent("Unique Id",
                $"Unique crossroad ID to get the light state through the ID in the <b>TrafficLightHybridService</b>.{System.Environment.NewLine}{System.Environment.NewLine}" +
                $"For example, if you want to get ID of traffic light handler with index 0: (UniqueID + 0), for light handler with index 1: (UniqueID + 1) & so on.");

            CreateReordableList(TrafficLightCrossroad);

            InitTimelineStyle();

            Undo.undoRedoPerformed += Undo_undoRedoPerformed;
        }

        private void OnDisable()
        {
            TrafficLightCrossroad.UnselectPath();

            Undo.undoRedoPerformed -= Undo_undoRedoPerformed;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DocumentationLinkerUtils.ShowButtonAndHeader(target, DocLink);

            EditorGUILayout.PropertyField(sharedStateContainerProp);

            if (Selection.objects.Length == 1)
            {
                ShowCachedValues();
            }

            GUILayout.BeginVertical("GroupBox");

            EditorGUILayout.PropertyField(serializedObject.FindProperty("hasLights"));

            GUILayout.EndVertical();

            if (TrafficLightCrossroad.HasLights)
            {
                if (Selection.objects.Length == 1)
                {
                    ShowLightStateInfo(serializedObject, TrafficLightCrossroad);
                    ShowArrowSettings(TrafficLightCrossroad);

                    if (GUILayout.Button("Add New Custom TrafficLightHandler"))
                    {
                        TrafficLightCrossroad.AddCustomTrafficLightHandler();
                    }
                }
                else
                {
                    GUILayout.BeginVertical("GroupBox");

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("customSettings"));

                    GUILayout.EndVertical();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void ShowCachedValues()
        {
            System.Action cachedLinksCallback = () =>
            {
                GUI.enabled = false;
                EditorGUILayout.IntField(idLabel, TrafficLightCrossroad.UniqueId);
                GUI.enabled = true;

                if (InspectorExtension.DrawClipboardButton(TrafficLightCrossroad.UniqueId.ToString()))
                {
                    UnityEngine.Debug.Log($"ID {TrafficLightCrossroad.UniqueId} copied to the clipboard.");
                }

                EditorGUILayout.PropertyField(trafficSegmentConfigProp);
                EditorGUILayout.PropertyField(trafficLightHandlerProp);
                EditorGUILayout.PropertyField(trafficLightParentProp);
                EditorGUILayout.PropertyField(pedestrianLightParentProp);
                EditorGUILayout.PropertyField(trafficNodesProp);
                EditorGUILayout.Separator();
                EditorGUILayout.PropertyField(trafficLightHandlerDataProp);
                EditorGUILayout.Separator();
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Cached links", cachedLinksCallback, ref showLinks);
        }

        private void ShowLightStateInfo(SerializedObject serializedObject, TrafficLightCrossroad trafficLightCrossroad)
        {
            inspectorWidth = EditorGUIUtility.currentViewWidth - 70f;

            GUILayout.BeginVertical("GroupBox", GUILayout.Width(inspectorWidth));

            InitTimelineStyle();

            if (!trafficLightCrossroad.CustomSettings)
            {
                var sharedStateContainer = trafficLightCrossroad.SharedStateContainer;

                if (sharedStateContainer != null)
                {
                    TrafficLightTimingDrawerUtils.DrawCrossroadSignalTimings(trafficLightCrossroad, timeLineStyle, inspectorWidth, prefixLabelOffset);
                }
                else
                {
                    EditorGUILayout.HelpBox("Add state container!", MessageType.Error, true);
                }
            }
            else
            {
                var hasNullHandler = trafficLightCrossroad.HasNullTrafficLightHandler();

                if (!hasNullHandler)
                {
                    TrafficLightTimingDrawerUtils.DrawCrossroadSignalTimings(trafficLightCrossroad, timeLineStyle, inspectorWidth, prefixLabelOffset);
                }
                else
                {
                    EditorGUILayout.HelpBox("One or more assigned TrafficLightHandler is null", MessageType.Error);
                }
            }

            EditorGUILayout.Separator();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("customSettings"));

            EditorGUILayout.Separator();

            if (trafficLightCrossroad.CustomSettings)
            {
                selectedIndexList = GUILayout.Toolbar(selectedIndexList, lightHeaders);

                if (reorderableLists != null && reorderableLists.Count > 0 && selectedIndexList >= 0 && reorderableLists.Count > selectedIndexList)
                {
                    reorderableLists[selectedIndexList].DoLayoutList();
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("showLoopTimeSettings"));

                if (trafficLightCrossroad.ShowLoopTimeSettings)
                {
                    var customTimeOffsetProp = serializedObject.FindProperty("customTimeOffset");
                    EditorGUILayout.PropertyField(customTimeOffsetProp);

                    GUI.enabled = false;
                    EditorGUILayout.IntSlider("Selected Handler Index", selectedIndexList, 0, 9);
                    GUI.enabled = true;

                    var sourceHandlerIndexProp = serializedObject.FindProperty("sourceDataHandlerIndex");
                    EditorGUILayout.PropertyField(sourceHandlerIndexProp);

                    var sourceIndex = sourceHandlerIndexProp.intValue;

                    if (GUILayout.Button("Loop Time"))
                    {
                        if (selectedIndexList != sourceIndex && trafficLightCrossroad.LoopTime(selectedIndexList, sourceIndex, customTimeOffsetProp.floatValue))
                        {
                            CreateReordableList(trafficLightCrossroad);
                        }
                    }
                }

                if (GUILayout.Button("Reset To Config Values"))
                {
                    trafficLightCrossroad.ResetToDefaultTimings();
                    CreateReordableList(trafficLightCrossroad);
                }
            }

            GUILayout.EndVertical();
        }

        private void ShowArrowSettings(TrafficLightCrossroad trafficLightCrossroad)
        {
            GUILayout.BeginVertical("GroupBox");

            EditorGUILayout.PropertyField(customLightSettingsProp);

            EditorGUI.BeginChangeCheck();

            trafficLightCrossroad.ShowCustomArrowLightSetup = EditorGUILayout.Toggle("Show Custom Arrow Light Setup", trafficLightCrossroad.ShowCustomArrowLightSetup);

            if (EditorGUI.EndChangeCheck())
            {
                trafficLightCrossroad.SetNextCustomRelatedLightIndex();
            }

            EditorGUILayout.Separator();

            if (trafficLightCrossroad.ShowCustomArrowLightSetup)
            {
                if (trafficLightCrossroad.TrafficNodes?.Count > 0)
                {
                    trafficLightCrossroad.CustomRelatedLightIndex = EditorGUILayout.IntSlider("Custom Related Light Index", trafficLightCrossroad.CustomRelatedLightIndex, 0, 10);

                    EditorGUI.BeginChangeCheck();

                    trafficLightCrossroad.CheckForInitilization();
                    trafficLightCrossroad.SourceSelectedNode = GUILayout.Toolbar(trafficLightCrossroad.SourceSelectedNode, trafficLightCrossroad.NodeHeaders);

                    if (EditorGUI.EndChangeCheck())
                    {
                        trafficLightCrossroad.SourceSelectedPathIndex = -1;
                        trafficLightCrossroad.UnselectPath();
                        trafficLightCrossroad.InitializePathHeaders();
                    }

                    if (trafficLightCrossroad.SourceSelectedNode != -1)
                    {
                        if (trafficLightCrossroad.PathHeaders == null || trafficLightCrossroad.PathHeaders.Length == 0)
                        {
                            trafficLightCrossroad.InitializePathHeaders();
                        }

                        EditorGUI.BeginChangeCheck();

                        trafficLightCrossroad.SourceSelectedPathIndex = GUILayout.Toolbar(trafficLightCrossroad.SourceSelectedPathIndex, trafficLightCrossroad.PathHeaders);

                        if (EditorGUI.EndChangeCheck())
                        {
                            foundPathMessage = trafficLightCrossroad.SelectCustomLaneLight();
                            showFoundPathMessage = true;
                        }
                    }
                }

                EditorGUILayout.PropertyField(selectedPathProp);

                if (!foundPathMessage && showFoundPathMessage)
                {
                    EditorGUILayout.HelpBox("Path not found", MessageType.Warning, true);
                }

                if (trafficLightCrossroad.SelectedPath)
                {
                    if (GUILayout.Button("Add Custom Light"))
                    {
                        trafficLightCrossroad.AddCustomLight();
                    }
                    if (GUILayout.Button("Unselect Path"))
                    {
                        trafficLightCrossroad.UnselectPath();
                    }

                    if (trafficLightCrossroad.CustomArrowLights?.Count > 0)
                    {
                        if (GUILayout.Button("Remove Selected Path"))
                        {
                            trafficLightCrossroad.RemoveSelectedPath();
                        }
                    }
                }
            }

            GUILayout.EndVertical();
        }

        private void CreateReordableList(TrafficLightCrossroad trafficLightCrossroad)
        {
            if (reorderableLists == null)
                return;

            reorderableLists.Clear();

            var headers = new List<string>();

            foreach (var item in trafficLightCrossroad.TrafficLightHandlers)
            {
                int index = (item.Key + 1);

                ReorderableList reordableList = CreateList(item.Value);

                if (reordableList != null)
                {
                    reorderableLists.Add(reordableList);
                }

                var trafficLightName = TrafficLightTimingDrawerUtils.GetLightName(item.Value);
                headers.Add(trafficLightName);
            }

            lightHeaders = headers.ToArray();
        }

        private ReorderableList CreateList(TrafficLightHandler handler)
        {
            if (handler == null)
                return null;

            var handlerSerializedObject = new SerializedObject(handler);

            return LightStateDrawer.DrawList("lightStates", handlerSerializedObject, handler.lightStates, AddItem, removeCallback: RemoveItem);
        }

        public void AddItem(object obj)
        {
            var LightStateAddData = (LightStateAddData)obj;
            LightState lightState = LightStateAddData.LightState;
            var trafficLightCrossroad = target as TrafficLightCrossroad;

            var targetIndex = selectedIndexList;

            TrafficLightHandler handler = trafficLightCrossroad.TrafficLightHandlers[targetIndex];
            handler.lightStates.Add(new LightStateInfo() { LightState = lightState });

            EditorSaver.SetObjectDirty(handler);

            reorderableLists[selectedIndexList] = CreateList(handler);
        }

        private void RemoveItem(ReorderableList list)
        {
            var trafficLightCrossroad = target as TrafficLightCrossroad;
            var targetIndex = selectedIndexList;

            int i = list.index;

            TrafficLightHandler handler = trafficLightCrossroad.TrafficLightHandlers[targetIndex];
            handler.lightStates.RemoveAt(i);

            EditorSaver.SetObjectDirty(handler);

            reorderableLists[selectedIndexList] = CreateList(handler);
        }

        private void InitTimelineStyle()
        {
            if (timeLineStyle != null)
                return;

            timeLineStyle = TrafficLightTimingDrawerUtils.GetDefaultTimelineStyle();
        }

        private void Undo_undoRedoPerformed()
        {
            var trafficLightCrossroad = target as TrafficLightCrossroad;
            CreateReordableList(trafficLightCrossroad);
        }
    }
}
#endif