using Spirit604.CityEditor.Road;
using Spirit604.Collections.Dictionary;
using Spirit604.Extensions;
using Spirit604.Gameplay.Config.Road;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Spirit604.Gameplay.Road
{
    [ExecuteInEditMode]
    public class TrafficLightCrossroad : MonoBehaviour, IBakeRoad
    {
        #region Helper types

        [Serializable]
        public class TrafficHandlerDataDictionary : AbstractSerializableDictionary<int, TrafficLightHandler> { }

        #endregion

        #region Serialized Variables

        [FormerlySerializedAs("trafficCrossRoadSettings")]
        [SerializeField] private SharedLightStateContainer sharedStateContainer;

        [SerializeField] private TrafficSegmentConfig trafficSegmentConfig;
        [SerializeField] private Transform trafficLightHandlerParent;
        [SerializeField] private Transform trafficLightParent;
        [SerializeField] private Transform pedestrianLightParent;
        [SerializeField] private bool hasLights = true;
        [SerializeField] private bool customSettings;
        [SerializeField] private List<TrafficNode> trafficNodes = new List<TrafficNode>();
        [SerializeField] private TrafficHandlerDataDictionary trafficLightHandlerData = new TrafficHandlerDataDictionary();
        [SerializeField] private List<CustomArrowLightSettings> customArrowLights = new List<CustomArrowLightSettings>();
        [SerializeField] private bool showCustomArrowLightSetup;
        [SerializeField] private Path selectedPath;
        [SerializeField] private int sourceSelectedNode = -1;
        [SerializeField] private int sourceSelectedPathIndex = -1;
        [SerializeField] private int customRelatedLightIndex = 2;
        [SerializeField] private string[] nodeHeaders;
        [SerializeField] private string[] pathHeaders;
        [SerializeField] private bool showLoopTimeSettings;
        [SerializeField][Range(-100, 100)] private float customTimeOffset;
        [SerializeField][Range(0, 9)] private int sourceDataHandlerIndex;

        [Tooltip("Unique crossroad ID to get the light state through the ID in the <b>TrafficLightHybridService</b>.\r\n\r\n" +
            "For example, if you want to get ID of traffic light handler with index 0: (UniqueID + 0), for light handler with index 1: (UniqueID + 1) & so on.")]
        [SerializeField] private int uniqueId;

#if UNITY_EDITOR
        private GameObject contentLink = null;
        private string prefabPath;
        private TrafficLightCrossroad prefab = null;
#endif

        #endregion

        #region Properties     

        public SharedLightStateContainer SharedStateContainer { get => sharedStateContainer; set => sharedStateContainer = value; }
        public Transform TrafficLightParent { get => trafficLightParent; }
        public bool HasLights { get => hasLights && enabled; set => hasLights = value; }
        public bool CustomSettings { get => customSettings; set => customSettings = value; }
        public bool ShowLoopTimeSettings { get => showLoopTimeSettings; }
        public List<TrafficNode> TrafficNodes { get => trafficNodes; set => trafficNodes = value; }
        public TrafficHandlerDataDictionary TrafficLightHandlers { get => trafficLightHandlerData; }
        public List<CustomArrowLightSettings> CustomArrowLights { get => customArrowLights; set => customArrowLights = value; }
        public bool ShowCustomArrowLightSetup { get => showCustomArrowLightSetup; set => showCustomArrowLightSetup = value; }
        public Path SelectedPath { get => selectedPath; set => selectedPath = value; }
        public int SourceSelectedNode { get => sourceSelectedNode; set => sourceSelectedNode = value; }
        public int SourceSelectedPathIndex { get => sourceSelectedPathIndex; set => sourceSelectedPathIndex = value; }
        public int CustomRelatedLightIndex { get => customRelatedLightIndex; set => customRelatedLightIndex = value; }
        public string[] NodeHeaders { get => nodeHeaders; set => nodeHeaders = value; }
        public string[] PathHeaders { get => pathHeaders; set => pathHeaders = value; }
        public int HandlerActiveCount => trafficLightHandlerData.Where(a => a.Value != null && a.Value.gameObject.activeSelf).Count();

        public int UniqueId
        {
            get
            {
                this.TryToGenerateID();
                return uniqueId;
            }
        }

#if RUNTIME_ROAD

        public int CrossroadIndex { get; set; }

#endif

        #endregion

        #region Unity lifecycle

#if UNITY_EDITOR
        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                var stage = PrefabStageUtility.GetCurrentPrefabStage();

                if (stage != null)
                {
                    return;
                }

                var roadParent = ObjectUtils.FindObjectOfType<RoadParent>();

                if (roadParent != null)
                {
                    roadParent.AddCrossRoad(this);
                }
            }
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
            {
                var stage = PrefabStageUtility.GetCurrentPrefabStage();

                if (stage != null)
                {
                    return;
                }

                var roadParent = ObjectUtils.FindObjectOfType<RoadParent>();

                if (roadParent != null)
                {
                    roadParent.RemoveCrossRoad(this);
                }
            }
        }
#endif

        #endregion

        #region IBakeRoad interface

        public void Bake()
        {
            var nodes = this.gameObject.GetComponentsInChildren<TrafficNode>();

            if (this.trafficNodes.Count != nodes.Length)
            {
                UnityEngine.Debug.Log($"Crossroad: '{name}' InstanceID: {GetInstanceID()} has no assigned TrafficNodes{TrafficObjectFinderMessage.GetMessage()}");
            }

            var handlers = this.gameObject.GetComponentsInChildren<TrafficLightHandler>();

            if (this.trafficLightHandlerData.Keys.Count != handlers.Length)
            {
                UnityEngine.Debug.Log($"Crossroad: '{name}' InstanceID: {GetInstanceID()} has no assigned TrafficLightHandlers{TrafficObjectFinderMessage.GetMessage()}");
            }

            if (!customSettings)
            {
                if (sharedStateContainer)
                {
                    int lightCount = sharedStateContainer.LightCount;

                    var arrows = GetCustomArrows();

                    if (arrows != null)
                    {
                        lightCount += arrows.Length;
                    }

                    if (lightCount != this.trafficLightHandlerData.Keys.Count)
                    {
                        UnityEngine.Debug.Log($"Crossroad: '{name}' InstanceID: {GetInstanceID()} Light state count and traffic light handler count not matched{TrafficObjectFinderMessage.GetMessage()}");
                    }
                }
                else
                {
                    UnityEngine.Debug.Log($"Crossroad: '{name}' InstanceID: {GetInstanceID()} shared container is null{TrafficObjectFinderMessage.GetMessage()}");
                }
            }

            this.TryToGenerateID();
        }

        #endregion

        #region Methods

        public CustomArrowLightSettings GetCustomLightSettings(Path path)
        {
            return customArrowLights.Where(item => item.path == path).FirstOrDefault();
        }

        public CustomArrowLightSettings GetCustomLightSettings(TrafficLightHandler trafficLightHandler)
        {
            for (int i = 0; i < customArrowLights.Count; i++)
            {
                if (customArrowLights[i].currentTrafficLightHandler == trafficLightHandler)
                {
                    return customArrowLights[i];
                }
            }

            return null;
        }

        public TrafficLightHandler GetTrafficLightHandlerContainsNode(TrafficNode trafficNode)
        {
            foreach (var item in trafficLightHandlerData)
            {
                if (item.Value != null)
                {
                    for (int i = 0; i < item.Value.TrafficNodes?.Length; i++)
                    {
                        if (item.Value.TrafficNodes[i] == trafficNode)
                        {
                            return item.Value;
                        }
                    }
                }
            }

            return null;
        }

        public bool AddNode(TrafficNode trafficNode)
        {
            if (trafficNodes.TryToAdd(trafficNode))
            {
                EditorSaver.SetObjectDirty(this);
                return true;
            }

            return false;
        }

        public void RemoveNode(TrafficNode trafficNode, bool includeRemoveFromAllNodes = false, bool recordUndo = false)
        {
            if (!trafficNode)
            {
                return;
            }

#if UNITY_EDITOR
            if (recordUndo)
            {
                Undo.RegisterCompleteObjectUndo(this, "Node removed");
            }
#endif

            var lightHandler = GetTrafficLightHandlerContainsNode(trafficNode);

            if (lightHandler != null)
            {
                if (recordUndo)
                {
#if UNITY_EDITOR

                    Undo.RegisterCompleteObjectUndo(lightHandler, "TrafficNode Destroy");
#endif
                }

                lightHandler.TryToRemoveNode(trafficNode);
            }

            if (includeRemoveFromAllNodes)
            {
                trafficNodes?.TryToRemove(trafficNode);
            }

            EditorSaver.SetObjectDirty(this);
        }

        public bool AddLight(TrafficNode trafficNode, int lightIndex)
        {
            var newHandler = GetTrafficLightHandler(lightIndex);

            if (newHandler == null)
            {
                UnityEngine.Debug.Log($"TrafficLightCrossroad. Failed to add light to traffic node '{trafficNode.name}'. Light handler with {lightIndex} doesn't exist");
                return false;
            }

            var oldHandler = GetTrafficLightHandlerContainsNode(trafficNode);

            if (oldHandler != newHandler)
            {
                if (oldHandler != null)
                {
                    oldHandler.TryToRemoveNode(trafficNode);
                }

                newHandler.AddNode(trafficNode);
                return true;
            }

            return false;
        }

        public bool RemoveLight(TrafficNode trafficNode)
        {
            var oldHandler = GetTrafficLightHandlerContainsNode(trafficNode);

            if (oldHandler != null)
                return oldHandler.TryToRemoveNode(trafficNode);

            return false;
        }

        public bool AddLight(PedestrianNode node, int lightIndex)
        {
            var newHandler = GetTrafficLightHandler(lightIndex);

            if (newHandler == null)
            {
                UnityEngine.Debug.Log($"TrafficLightCrossroad. Failed to add light to pedestrian node '{node.name}'. Light handler with {lightIndex} doesn't exist");
                return false;
            }

            var oldHandler = node.RelatedTrafficLightHandler;

            if (newHandler != oldHandler)
            {
                node.RelatedTrafficLightHandler = newHandler;
                EditorSaver.SetObjectDirty(node);
                return true;
            }

            return false;
        }

        public bool RemoveLight(PedestrianNode node)
        {
            var oldHandler = node.RelatedTrafficLightHandler;

            if (oldHandler != null)
            {
                node.RelatedTrafficLightHandler = null;
                EditorSaver.SetObjectDirty(node);
                return true;
            }

            return false;
        }

        public TrafficLightHandler GetTrafficLightHandler(int relatedIndex)
        {
            TrafficLightHandler trafficLightHandler = null;

            trafficLightHandlerData.TryGetValue(relatedIndex, out trafficLightHandler);

            return trafficLightHandler;
        }

        public void ClearHandlers()
        {
            foreach (var trafficLightHandlerData in trafficLightHandlerData)
            {
                trafficLightHandlerData.Value.Clear();
            }
        }

        public void ClearLights(bool includeCustom = true)
        {
            foreach (var item in trafficLightHandlerData)
            {
                item.Value.ChildLights.Clear();

                if (includeCustom)
                {
                    item.Value.CustomLights.Clear();
                }
            }
        }

        private bool HasCustomLight(Path path)
        {
            for (int i = 0; i < customArrowLights?.Count; i++)
            {
                if (customArrowLights[i].path == path)
                {
                    return true;
                }
            }

            return false;
        }

        public bool SelectCustomLaneLight()
        {
            UnselectPath();

            Path path = null;

            if (trafficNodes.Count > sourceSelectedNode)
            {
                path = trafficNodes[sourceSelectedNode].GetPathByAbsoluteIndex(sourceSelectedPathIndex);
            }

            if (path)
            {
                var roadSegmentCreator = GetComponent<RoadSegmentCreator>();

                if (roadSegmentCreator)
                {
                    if (roadSegmentCreator.selectedTrafficNodeIndex != sourceSelectedNode)
                    {
                        roadSegmentCreator.selectedTrafficNodeIndex = sourceSelectedNode;
                        roadSegmentCreator.InitializePathHeaders();
                    }

                    roadSegmentCreator.selectedPathIndex = sourceSelectedPathIndex;
                }

                selectedPath = path;

#if UNITY_EDITOR
                selectedPath.Highlighted = true;
                SceneView.RepaintAll();
#endif
            }

            return path;
        }

        public void UnselectPath()
        {
            if (selectedPath != null)
            {
#if UNITY_EDITOR
                selectedPath.Highlighted = false;
#endif

                selectedPath = null;
            }

#if UNITY_EDITOR
            SceneView.RepaintAll();
#endif
        }

        public void AddChildLight(TrafficLightObject trafficLightObject)
        {
            var frameData = trafficLightObject.TrafficLightFrames;

            foreach (var item in frameData)
            {
                var handler = GetTrafficLightHandler(item.Key);

                if (handler != null)
                {
                    foreach (var frame in item.Value.TrafficLightFrames)
                    {
                        handler.AddChildTrafficLight(frame);
                    }
                }
            }

            trafficLightObject.AssignCrossRoad(this);
        }

        public void AddCustomLight()
        {
            AddCustomLight(selectedPath, false);
        }

        public TrafficLightHandler AddCustomLight(Path path, bool generateCustomIndex = true)
        {
            if (HasCustomLight(path))
            {
                var settings = GetCustomLightSettings(path);

                return settings.currentTrafficLightHandler;
            }

            if (generateCustomIndex)
            {
                SetNextCustomRelatedLightIndex();
            }

            var relatedHandler = GetTrafficLightHandlerContainsNode(path.SourceTrafficNode);

            if (relatedHandler == null)
            {
                relatedHandler = GetTrafficLightHandler(0);
            }

            if (relatedHandler == null)
            {
                UnityEngine.Debug.Log("TrafficLightCrossroad. Doesn't have a traffic light handler to attach");
                return null;
            }

            var trafficLightHandler = CreateCustomTrafficLightHandler(customRelatedLightIndex, true);

            if (trafficLightHandler == null)
            {
                return null;
            }

            var customSettings = new CustomArrowLightSettings(path);
            customSettings.currentTrafficLightHandler = trafficLightHandler;

            customSettings.relatedTrafficLightHandler = relatedHandler;

            float duration = relatedHandler.GetStateDuration(LightState.Green);

            customSettings.enabledDuration = duration;

            customArrowLights.Add(customSettings);
            EditorSaver.SetObjectDirty(this);

            return trafficLightHandler;
        }

        public void RemoveSelectedPath()
        {
            RemovePath(selectedPath, true);
        }

        public void RemovePath(Path path, bool destroyHandler = false)
        {
            if (!path)
                return;

            for (int i = 0; i < customArrowLights?.Count; i++)
            {
                if (customArrowLights[i].path == path)
                {
                    if (destroyHandler)
                    {
                        var handler = customArrowLights[i].currentTrafficLightHandler;

                        if (handler != null && !handler.HasAttachedObjects)
                        {
                            TryToRemoveTrafficLightHandler(handler, destroyHandler);
                        }
                    }

                    customArrowLights.TryToRemove(customArrowLights[i]);
                    break;
                }
            }
        }

        public TrafficLightHandler CreateCustomTrafficLightHandler(int lightIndex, bool autoAddToCrossroad = true, bool applyPrefab = false)
        {
            if (trafficSegmentConfig == null || trafficSegmentConfig.TrafficLightHandlerPrefab == null)
            {
                UnityEngine.Debug.Log("'TrafficSegmentConfig' or 'TrafficLightHandler' prefab not found!");
                return null;
            }

            TrafficLightHandler trafficLightHandler = null;
            TrafficLightCrossroad currentTrafficLightCrossroad = null;
            Transform currentTrafficLightParent = null;
            Transform currentPedestrianLightParent = null;

#if UNITY_EDITOR
            Transform currentTrafficLightHandlerParent = null;

            if (applyPrefab && prefab)
            {
                currentTrafficLightCrossroad = prefab;
                currentTrafficLightHandlerParent = prefab.trafficLightHandlerParent;
                currentTrafficLightParent = prefab.trafficLightParent;
                currentPedestrianLightParent = prefab.pedestrianLightParent;
            }
            else
            {
                currentTrafficLightCrossroad = this;
                currentTrafficLightHandlerParent = trafficLightHandlerParent;
                currentTrafficLightParent = trafficLightParent;
                currentPedestrianLightParent = pedestrianLightParent;
            }

            var handler = currentTrafficLightCrossroad.GetTrafficLightHandler(lightIndex);

            if (handler)
            {
                UnityEngine.Debug.Log($"Crossroad '{currentTrafficLightCrossroad.name}'. Trying to add exist TrafficLightHandler[{lightIndex}]");
                return handler;
            }

            trafficLightHandler = (PrefabUtility.InstantiatePrefab(trafficSegmentConfig.TrafficLightHandlerPrefab.gameObject, currentTrafficLightHandlerParent) as GameObject).GetComponent<TrafficLightHandler>();
            trafficLightHandler.name = $"{trafficLightHandler.name}{lightIndex + 1}";
#endif

            trafficLightHandler.transform.localRotation = Quaternion.identity;
            trafficLightHandler.transform.localPosition = Vector3.zero;

            trafficLightHandler.TrafficLightCrossroad = currentTrafficLightCrossroad;
            trafficLightHandler.RelatedLightIndex = lightIndex;
            trafficLightHandler.TrafficLightsParents = currentTrafficLightParent;
            trafficLightHandler.PedestrianLightsParents = currentPedestrianLightParent;

            EditorSaver.SetObjectDirty(trafficLightHandler);

            if (autoAddToCrossroad)
            {
#if UNITY_EDITOR
                if (!applyPrefab || !prefab)
                {
                    trafficLightHandlerData.Add(lightIndex, trafficLightHandler);
                    EditorSaver.SetObjectDirty(this);
                }
                else
                {
                    var so = new SerializedObject(this);
                    var prop = so.FindProperty(nameof(trafficLightHandlerData));

                    PrefabUtility.RevertPropertyOverride(prop, InteractionMode.AutomatedAction);

                    if (prefab.trafficLightHandlerData.Count == 0 || trafficLightHandlerData.Keys.Count != prefab.trafficLightHandlerData.Count)
                    {
                        List<int> keys = new List<int>();
                        List<TrafficLightHandler> values = new List<TrafficLightHandler>();
                        var handlerDictCopy = new TrafficHandlerDataDictionary();

                        var localHandlers = prefab.GetComponentsInChildren<TrafficLightHandler>().Where(a => a.RelatedLightIndex != lightIndex).OrderBy(a => a.RelatedLightIndex);

                        foreach (var localHandler in localHandlers)
                        {
                            keys.Add(localHandler.RelatedLightIndex);
                            values.Add(localHandler);
                        }

                        if (keys.Count > 0)
                        {
                            handlerDictCopy.SetDictionary(keys.ToArray(), values.ToArray());
                        }

                        handlerDictCopy.Add(lightIndex, trafficLightHandler);

                        prefab.trafficLightHandlerData = handlerDictCopy;
                    }
                    else
                    {
                        prefab.trafficLightHandlerData.Add(lightIndex, trafficLightHandler);
                    }
                }
#endif
            }

            return trafficLightHandler;
        }

        public void SetNextCustomRelatedLightIndex()
        {
            int maxIndex = -1;

            if (trafficLightHandlerData.Keys.Count > 0)
            {
                maxIndex = trafficLightHandlerData.Keys.Max();
            }

            customRelatedLightIndex = maxIndex + 1;

            EditorSaver.SetObjectDirty(this);
        }

        public void AddCustomTrafficLightHandler()
        {
            SetNextCustomRelatedLightIndex();
            CreateCustomTrafficLightHandler(customRelatedLightIndex, true);
        }

        public void TryToRemoveTrafficLightHandler(TrafficLightHandler trafficLightHandler, bool destroy = false)
        {
            var index = trafficLightHandler.RelatedLightIndex;

            bool makeDirty = false;

            if (trafficLightHandlerData.ContainsKey(index) && trafficLightHandlerData[index] == trafficLightHandler)
            {
                trafficLightHandlerData.Remove(index);
                makeDirty = true;
            }

            if (destroy)
            {
                makeDirty = true;
                GameObject.DestroyImmediate(trafficLightHandler.gameObject);
            }

            if (makeDirty)
            {
                EditorSaver.SetObjectDirty(this);
            }
        }

        public void Initialize(List<TrafficNode> trafficNodes)
        {
            this.trafficNodes = trafficNodes;

            nodeHeaders = trafficNodes.Select(item => item.name).ToArray();

            EditorSaver.SetObjectDirty(this);
        }

        public void CheckForInitilization()
        {
            if (trafficNodes != null && trafficNodes.Count > 0 && (nodeHeaders == null || trafficNodes.Count != nodeHeaders.Length))
            {
                Initialize(trafficNodes);
            }
        }

        public void InitializePathHeaders()
        {
            var tempHeader = new List<string>();

            if (sourceSelectedNode == -1)
            {
                return;
            }

            var lanes = trafficNodes[sourceSelectedNode].Lanes;

            for (int i = 0; i < lanes?.Count; i++)
            {
                var laneData = lanes[i];

                for (int j = 0; j < laneData.paths?.Count; j++)
                {
                    var path = laneData.paths[j];

                    if (path != null)
                    {
                        tempHeader.Add(path.name);
                    }
                }
            }

            pathHeaders = tempHeader.ToArray();

            EditorSaver.SetObjectDirty(this);
        }

        public float GetTotalCycleTime(int index)
        {
            float totalTime = 0;

            if (trafficLightHandlerData[index])
            {
                totalTime = trafficLightHandlerData[index].GetTotalLightCycleTime();
            }

            return totalTime;
        }

        public float GetMaxTotalCycleTime()
        {
            if (trafficLightHandlerData == null || trafficLightHandlerData.Keys.Count == 0)
            {
                return 0;
            }

            float maxTime = 0;

            foreach (var item in trafficLightHandlerData)
            {
                if (item.Value == null)
                {
                    continue;
                }

                var totalTime = GetTotalCycleTime(item.Key);

                if (totalTime > maxTime)
                {
                    maxTime = totalTime;
                }
            }

            return maxTime;
        }

        public bool HasNullTrafficLightHandler()
        {
            foreach (var handlerEntry in trafficLightHandlerData)
            {
                if (handlerEntry.Value == null)
                {
                    return true;
                }
            }

            return false;
        }

        public CustomArrowLightSettings[] GetRelatedLightArrows(TrafficLightHandler sourceHandler)
        {
            if (customArrowLights.IsEmpty())
            {
                return null;
            }

            return customArrowLights.Where(item => item.relatedTrafficLightHandler == sourceHandler).ToArray();
        }

        public CustomArrowLightSettings[] GetCustomArrows()
        {
            if (customArrowLights.IsEmpty())
            {
                return null;
            }

            return customArrowLights.Where(item => item.relatedTrafficLightHandler != null && item.path != null).ToArray();
        }

        public CustomArrowLightSettings TryToGetCustomArrow(TrafficLightHandler sourceHandler)
        {
            if (customArrowLights.IsEmpty())
            {
                return null;
            }

            var arr = customArrowLights.Where(item => item.currentTrafficLightHandler == sourceHandler).ToArray();

            if (arr?.Length > 0)
            {
                if (arr.Length > 1)
                {
                    UnityEngine.Debug.Log($"Crossroad {name} InstanceID {GetInstanceID()} Handler[{sourceHandler.RelatedLightIndex}] has multiple arrows.");
                }

                return arr[0];
            }

            return null;
        }

        public bool IsArrowLight(TrafficLightHandler sourceHandler)
        {
            if (customArrowLights.IsEmpty())
            {
                return false;
            }

            return TryToGetCustomArrow(sourceHandler) != null;
        }

        public int GetHandlerCount()
        {
            var arrows = GetCustomArrows();
            int count = 0;

            if (arrows != null)
            {
                count += arrows.Length;
            }

            count += trafficLightHandlerData.Count;

            return count;
        }

        public bool LoopTime(int selectedIndexList, int sourceDataIndex, float customTimeOffset = 0)
        {
            var selectedHandler = GetTrafficLightHandler(selectedIndexList);
            var sourceHandler = GetTrafficLightHandler(sourceDataIndex);

            if (sourceHandler != null)
            {
                var offsetTime = TrafficLightTimingUtils.GetTotalDuration(sourceHandler.LightStates);

                offsetTime /= 2;
                offsetTime += customTimeOffset;

                var newStates = TrafficLightTimingUtils.GetLightStatesWithTimeOffset(sourceHandler.LightStates, offsetTime);

                if (newStates?.Count > 0)
                {
#if UNITY_EDITOR
                    Undo.RegisterCompleteObjectUndo(selectedHandler, "Undo TrafficLightHandler");
#endif

                    selectedHandler.LightStates.Clear();
                    selectedHandler.lightStates = newStates;
                    EditorSaver.SetObjectDirty(selectedHandler);
                }
                else
                {
                    UnityEngine.Debug.Log($"TrafficLightHandler Index {sourceDataIndex} looped states not found");
                    return false;
                }

                return true;
            }
            else
            {
                UnityEngine.Debug.Log($"TrafficLightHandler Index {sourceDataIndex} not found");
            }

            return false;
        }

        public void ResetToDefaultTimings()
        {
            for (int handlerIndex = 0; handlerIndex < sharedStateContainer.LightCount; handlerIndex++)
            {
                if (trafficLightHandlerData.ContainsKey(handlerIndex))
                {
                    var states = sharedStateContainer.GetStates(handlerIndex);
                    trafficLightHandlerData[handlerIndex].lightStates = new List<LightStateInfo>(states);
                    EditorSaver.SetObjectDirty(trafficLightHandlerData[handlerIndex]);
                }
                else
                {
                    UnityEngine.Debug.LogError($"Traffic Light [{handlerIndex}] handler doesn't exist");
                }
            }
        }

        public int TrafficNodeDefaultCount()
        {
            var defaultNodes = trafficNodes.Where(a => a != null && a.TrafficNodeType == TrafficNodeType.Default);

            if (defaultNodes != null)
            {
                return defaultNodes.Count();
            }

            return 0;
        }

        public void SetSettings(SharedLightStateContainer trafficCrossRoadSettings, bool syncHandlers, bool recordUndo = false, bool applyPrefab = false)
        {
            if (applyPrefab)
            {
                LoadPrefab();
            }

#if UNITY_EDITOR
            if (applyPrefab && prefab)
            {
                prefab.sharedStateContainer = trafficCrossRoadSettings;
            }
            else
            {
                this.sharedStateContainer = trafficCrossRoadSettings;
                EditorSaver.SetObjectDirty(this);
            }
#endif

            if (syncHandlers)
            {
                var lightCount = trafficCrossRoadSettings.LightCount;

                for (int i = 0; i < lightCount; i++)
                {
                    var handler = GetTrafficLightHandler(i);

                    if (!handler)
                    {
                        CreateCustomTrafficLightHandler(i, true, applyPrefab);
                    }
                }

                foreach (var item in trafficLightHandlerData)
                {
                    if (!item.Value)
                    {
                        continue;
                    }

                    bool isActive = item.Key < lightCount;

                    if (item.Value.gameObject.activeSelf != isActive)
                    {
#if UNITY_EDITOR
                        if (recordUndo)
                        {
                            Undo.RegisterCompleteObjectUndo(item.Value.gameObject, "Disable handler");
                        }
#endif

                        item.Value.gameObject.SetActive(isActive);
                    }
                }
            }

            SavePrefab();
            UnloadPrefab();
        }

        public void TryToGenerateID(bool force = false)
        {
            if (uniqueId == 0 || force)
            {
                uniqueId = TryToGenerateIDStatic();
                EditorSaver.SetObjectDirty(this);
            }
        }

        public static int TryToGenerateIDStatic() => Guid.NewGuid().ToString().GetHashCode();

        public void ClearNodes()
        {
            trafficNodes.Clear();
        }

        private void LoadPrefab()
        {
#if UNITY_EDITOR
            if (contentLink)
            {
                return;
            }

            GameObject prefabGo = PrefabUtility.GetCorrespondingObjectFromSource(this.gameObject);

            if (prefabGo != null)
            {
                prefabPath = AssetDatabase.GetAssetPath(prefabGo);
                contentLink = PrefabUtility.LoadPrefabContents(prefabPath);

                if (contentLink)
                {
                    prefab = contentLink.GetComponent<TrafficLightCrossroad>();
                }
            }
#endif
        }

        private void UnloadPrefab()
        {
#if UNITY_EDITOR
            if (contentLink)
            {
                PrefabUtility.UnloadPrefabContents(contentLink);
                contentLink = null;
                prefab = null;
                prefabPath = string.Empty;
            }
#endif
        }

        private void SavePrefab()
        {
#if UNITY_EDITOR
            if (prefab)
            {
                EditorSaver.SetObjectDirty(prefab);
                PrefabUtility.SaveAsPrefabAsset(prefab.gameObject, prefabPath);
            }
#endif
        }

    }

    #endregion
}