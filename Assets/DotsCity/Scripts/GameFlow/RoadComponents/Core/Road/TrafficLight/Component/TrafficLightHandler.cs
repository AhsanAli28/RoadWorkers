using Spirit604.Extensions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spirit604.Gameplay.Road
{
    [ExecuteInEditMode]
    public class TrafficLightHandler : MonoBehaviour
    {
        #region Helper types

        public enum ShowLightConnectionType
        {
            All = Light | TrafficNode | PedestrianNode,
            Light = 1 << 0,
            TrafficNode = 1 << 1,
            PedestrianNode = 1 << 2
        }

        #endregion

        #region Serialized Variables

        [SerializeField] private TrafficLightCrossroad trafficLightCrossroad;
        [SerializeField] private TrafficNode[] triggers;
        [SerializeField] private Transform trafficLightsParents, pedestrianLightsParents;
        [SerializeField][Range(0, 9)] private int relatedLightIndex;
        [SerializeField] private List<TrafficLightFrameBase> childLights = new List<TrafficLightFrameBase>();
        [SerializeField] private List<TrafficLightFrameBase> customLights = new List<TrafficLightFrameBase>();
        [SerializeField] private bool showWorldTrafficLights;
        [SerializeField] private bool showLightConnection;
        [SerializeField] private ShowLightConnectionType visibleLightConnection = ShowLightConnectionType.Light;

        [HideInInspector] public List<LightStateInfo> lightStates = new List<LightStateInfo>();

        #endregion

        #region Variables

        private List<TrafficLightFrameBase> currentLights = new List<TrafficLightFrameBase>();

        #endregion

        #region Properties   

        public TrafficLightCrossroad TrafficLightCrossroad { get => trafficLightCrossroad; set => trafficLightCrossroad = value; }

        public int RelatedLightIndex { get => relatedLightIndex; set => relatedLightIndex = value; }

        public Transform TrafficLightsParents { get => trafficLightsParents; set => trafficLightsParents = value; }

        public Transform PedestrianLightsParents { get => pedestrianLightsParents; set => pedestrianLightsParents = value; }

        public LightState CurrentTrafficLightState { get; private set; }

        public List<LightStateInfo> LightStates
        {
            get
            {
                if (trafficLightCrossroad)
                {
                    if (trafficLightCrossroad.CustomSettings)
                    {
                        return lightStates;
                    }
                    else
                    {
                        if (trafficLightCrossroad.SharedStateContainer != null)
                        {
                            return trafficLightCrossroad.SharedStateContainer.GetStates(relatedLightIndex);
                        }
                    }
                }

                return null;
            }
        }

        public bool ShowWorldTrafficLights => showWorldTrafficLights;

        public bool ShowLightConnection { get => showLightConnection; set => showLightConnection = value; }

        public ShowLightConnectionType VisibleLightConnectionType { get => visibleLightConnection; set => visibleLightConnection = value; }

        public bool HasAttachedObjects => (triggers != null && triggers.Length > 0) || childLights.Count > 0 || customLights.Count > 0;

        public TrafficNode[] TrafficNodes { get => triggers; set => triggers = value; }

        public List<TrafficLightFrameBase> ChildLights { get => childLights; set => childLights = value; }

        public List<TrafficLightFrameBase> CustomLights { get => customLights; set => customLights = value; }

        #endregion

        #region Unity lifecycle

        private void Awake()
        {
            if (Application.isPlaying)
            {
#if UNITY_EDITOR
                for (int i = 0; i < triggers?.Length; i++)
                {
                    if (triggers[i] != null && triggers[i].TrafficLightHandler != this)
                    {
                        var crossRoadName = trafficLightCrossroad ? trafficLightCrossroad.name : string.Empty;
                        var targetLightHandlerName = triggers[i].TrafficLightHandler != null ? triggers[i].TrafficLightHandler.name : "null";

                        UnityEngine.Debug.Log($"Crossroad '{crossRoadName}' Node {triggers[i].name} TrafficLightHandler not matched with related node. Current TrafficLightHandler '{name}'. Assigned to node '{targetLightHandlerName}'");
                    }
                }
#endif

                Initialize();
            }
        }

        private void OnDestroy()
        {
            if (!Application.isPlaying)
            {
                trafficLightCrossroad?.TryToRemoveTrafficLightHandler(this);
            }
        }

        #endregion

        #region Methods

        public void AddNode(TrafficNode trafficNode)
        {
            int count = 1;

            if (triggers != null)
            {
                count += triggers.Length;
            }

            TrafficNode[] trafficNodes = new TrafficNode[count];

            for (int i = 0; i < triggers?.Length; i++)
            {
                trafficNodes[i] = triggers[i];
            }

            trafficNodes[trafficNodes.Length - 1] = trafficNode;

            triggers = trafficNodes;
            trafficNode.TrafficLightHandler = this;
            trafficNode.TrafficLightCrossroad = trafficLightCrossroad;

            EditorSaver.SetObjectDirty(trafficNode);
            EditorSaver.SetObjectDirty(this);
        }

        public bool TryToRemoveNode(TrafficNode trafficNode)
        {
            if (trafficNode == null || triggers == null)
            {
                return false;
            }

            if (!triggers.Contains(trafficNode))
            {
                return false;
            }

            TrafficNode[] trafficNodes = triggers.Where(item => item != trafficNode).ToArray();

            triggers = trafficNodes;
            trafficNode.TrafficLightHandler = null;

            EditorSaver.SetObjectDirty(trafficNode);
            EditorSaver.SetObjectDirty(this);
            return true;
        }

        public bool HasNode(TrafficNode trafficNode)
        {
            return triggers.Contains(trafficNode);
        }

        public bool Connected(TrafficNode trafficNode)
        {
            return triggers.Contains(trafficNode) && trafficNode.TrafficLightHandler == this;
        }

        public void Clear()
        {
            triggers = null;
        }

        public float GetTotalLightCycleTime()
        {
            if (LightStates == null || LightStates.Count == 0)
            {
                return 0;
            }

            return LightStates.Select(light => light.Duration).Sum();
        }

        public float GetStateDuration(LightState lightState)
        {
            var state = LightStates.Where(item => item.LightState == lightState).FirstOrDefault();

            if (!state.Equals(default))
            {
                return state.Duration;
            }

            return 0;
        }

        public List<TrafficLightFrameBase> GetLights()
        {
            var lights = new List<TrafficLightFrameBase>();

            lights.AddRange(childLights);

            for (int i = 0; i < customLights?.Count; i++)
            {
                lights.TryToAdd(customLights[i]);
            }

            return lights;
        }

        public void AddChildTrafficLight(TrafficLightFrameBase newTrafficLightObject)
        {
            if (childLights.TryToAdd(newTrafficLightObject))
            {
                newTrafficLightObject.AssignCrossRoad(trafficLightCrossroad);
                EditorSaver.SetObjectDirty(this);
            }
        }

        public void AddCustomTrafficLight(TrafficLightFrameBase newTrafficLightObject, bool reparent = false)
        {
            if (customLights.TryToAdd(newTrafficLightObject))
            {
                newTrafficLightObject.AssignCrossRoad(trafficLightCrossroad, reparent);

                EditorSaver.SetObjectDirty(this);
            }
        }

        public void RemoveTrafficLight(TrafficLightFrameBase newTrafficLightObject)
        {
            if (customLights.TryToRemove(newTrafficLightObject) || childLights.TryToRemove(newTrafficLightObject))
            {
                EditorSaver.SetObjectDirty(this);
            }
        }

        public bool ContainsTrafficLight(TrafficLightFrameBase newTrafficLightObject)
        {
            return customLights.Contains(newTrafficLightObject) || childLights.Contains(newTrafficLightObject);
        }

        public void FindChildLights()
        {
            childLights.Clear();
            var currentChildTrafficLights = TryToGetChildLights(trafficLightsParents);
            var currentChildPedestrianLights = TryToGetChildLights(pedestrianLightsParents);

            if (currentChildTrafficLights != null)
            {
                childLights.AddRange(currentChildTrafficLights);
            }

            if (currentChildPedestrianLights != null)
            {
                childLights.AddRange(currentChildPedestrianLights);
            }

            EditorSaver.SetObjectDirty(this);
        }

        public bool HasLight(TrafficNode trafficNode)
        {
            if (trafficLightCrossroad != null && trafficLightCrossroad.HasLights)
            {
                return true;
            }

            return false;
        }

        public bool HasCustomLight(Path path)
        {
            if (!path.HasCustomLight)
            {
                return false;
            }

            if (trafficLightCrossroad != null && trafficLightCrossroad.enabled && trafficLightCrossroad.HasLights)
            {
                var customSettings = trafficLightCrossroad.GetCustomLightSettings(path);

                if (customSettings != null && customSettings.path && customSettings.currentTrafficLightHandler && customSettings.relatedTrafficLightHandler)
                {
                    return true;
                }
            }

            return false;
        }

        private List<TrafficLightFrameBase> TryToGetChildLights(Transform targetParent)
        {
            return targetParent.GetComponentsInChildren<TrafficLightObject>().Where(a => a.HasLightIndex(relatedLightIndex)).SelectMany(a => a.GetLightFrames(relatedLightIndex)).ToList();
        }

        private void Initialize()
        {
            currentLights = GetLights();
        }
    }

    #endregion
}