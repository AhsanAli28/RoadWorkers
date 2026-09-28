using Spirit604.DotsCity.Simulation.Level.Props;
using Spirit604.DotsCity.Simulation.Level.Streaming.Authoring;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Road.Authoring
{
    [DisallowMultipleComponent]
    public class TrafficLightObjectAuthoring : MonoBehaviour, IRelatedObjectProvider
    {
        [SerializeField] private TrafficLightObject trafficLightObject;

        [HideInInspector]
        [SerializeField] private bool autoRegister = true;

        public GameObject RelatedObject
        {
            get
            {
                if (trafficLightObject && trafficLightObject.TrafficLightCrossroad)
                {
                    return trafficLightObject.TrafficLightCrossroad.gameObject;
                }

                return null;
            }
        }

        public bool AutoRegister { get => autoRegister; set => autoRegister = value; }

        public TrafficLightObject TrafficLightObject => trafficLightObject;

        private void OnEnable()
        {
            if (TrafficLightHybridService.Instance && autoRegister)
                RegisterFrames();
        }

        private void OnDisable()
        {
            if (TrafficLightHybridService.Instance)
                RemoveFrames();
        }

        public void RegisterFrames()
        {
            if (trafficLightObject.ConnectedId == 0)
                return;

            var frames = trafficLightObject.TrafficLightFrames;

            foreach (var frameData in frames)
            {
                int frameID = frameData.Key + trafficLightObject.ConnectedId;

                var localFrames = frameData.Value.TrafficLightFrames;

                for (int i = 0; i < localFrames.Count; i++)
                {
                    var localFrame = localFrames[i];

                    TrafficLightHybridService.Instance.AddListener(localFrame, frameID);
                }
            }
        }

        public void RemoveFrames()
        {
            if (trafficLightObject.ConnectedId == 0)
                return;

            var frames = trafficLightObject.TrafficLightFrames;

            foreach (var frameData in frames)
            {
                int frameID = frameData.Key + trafficLightObject.ConnectedId;

                var localFrames = frameData.Value.TrafficLightFrames;

                for (int i = 0; i < localFrames.Count; i++)
                {
                    var localFrame = localFrames[i];

                    TrafficLightHybridService.Instance.RemoveListener(localFrame, frameID);
                }
            }
        }

        public void Reset()
        {
            trafficLightObject = GetComponent<TrafficLightObject>();
            EditorSaver.SetObjectDirty(this);
        }

        class TrafficLightObjectAuthoringBaker : Baker<TrafficLightObjectAuthoring>
        {

#if UNITY_EDITOR

            private static TrafficLightCrossroad[] trafficLightCrossRoads;
            private static System.Collections.Generic.Dictionary<int, TrafficLightCrossroad> lightBinding;

#endif

            public override void Bake(TrafficLightObjectAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

                AddComponent(entity, typeof(LightPropTag));

                var frameBuffer = AddBuffer<LightFrameEntityHolderElement>(entity);

                if (!authoring.trafficLightObject || authoring.trafficLightObject.TrafficLightFrames == null)
                    return;

                var frames = authoring.trafficLightObject.TrafficLightFrames;

                foreach (var frameData in frames)
                {
                    if (frameData.Value == null)
                        continue;

                    var tempFrameBakingEntity = CreateAdditionalEntity(TransformUsageFlags.None, true);

                    var relatedHandlerEntity = Entity.Null;

                    if (authoring.trafficLightObject.TrafficLightCrossroad)
                    {
                        var handler = authoring.trafficLightObject.TrafficLightCrossroad.GetTrafficLightHandler(frameData.Key);

                        if (handler != null)
                        {
                            relatedHandlerEntity = GetEntity(handler, TransformUsageFlags.Dynamic);
                        }
                        else
                        {
                            Debug.Log($"{GetHeader(authoring)} <b>TrafficLightHandler</b> not found. Make sure that <b>TrafficLight</b> has correct <>LightIndex</b> (current <b>{frameData.Key}</b>) & <b>TrafficLightHandler</b> is assigned to <b>TrafficLightCrossroad</b>.{TrafficObjectFinderMessage.GetMessage()}");
                        }
                    }
                    else
                    {
                        bool hasBinding = authoring.trafficLightObject.ConnectedId != 0;

#if UNITY_EDITOR
                        InitBinding();
                        hasBinding = lightBinding != null && lightBinding.ContainsKey(authoring.trafficLightObject.ConnectedId);
#endif

                        if (!hasBinding)
                        {
                            Debug.Log($"{GetHeader(authoring)} doens't have link to 'TrafficLightCrossroad'.{TrafficObjectFinderMessage.GetMessage()}");
                        }
                        else
                        {
                            Debug.Log($"{GetHeader(authoring)} doens't have link to 'TrafficLightCrossroad'. Open <a href=\"https://dotstrafficcity.readthedocs.io/en/latest/trafficLight.html#how-to-customize-city-crossroads\">GlobalTrafficLightSettings</a> & press <b>Reconnect Lights</b> button.{TrafficObjectFinderMessage.GetMessage()}");
                        }
                    }

                    var localFrames = frameData.Value.TrafficLightFrames;

                    int frameCount = localFrames?.Count ?? 0;

                    var frameEntities = new NativeList<LightEntityElementTemp>(frameCount, Allocator.TempJob);

                    if (frameCount == 0)
                    {
                        Debug.Log($"{GetHeader(authoring)} index {frameData.Key} has 0 assigned light frames{TrafficObjectFinderMessage.GetMessage()}");
                    }

                    for (int i = 0; i < frameCount; i++)
                    {
                        var frameBase = localFrames[i];

                        if (frameBase == null)
                            continue;

                        var frameEntity = GetEntity(frameBase.gameObject, TransformUsageFlags.Dynamic);

                        Entity redEntity = Entity.Null;
                        Entity yellowEntity = Entity.Null;
                        Entity greenEntity = Entity.Null;

                        var frame = frameBase as TrafficLightFrame;

                        if (frame != null)
                        {
                            if (frame.RedLight != null)
                            {
                                redEntity = GetEntity(frame.RedLight, TransformUsageFlags.Dynamic);
                            }

                            if (frame.YellowLight != null)
                            {
                                yellowEntity = GetEntity(frame.YellowLight, TransformUsageFlags.Dynamic);
                            }

                            if (frame.GreenLight != null)
                            {
                                greenEntity = GetEntity(frame.GreenLight, TransformUsageFlags.Dynamic);
                            }
                        }

                        frameEntities.Add(new LightEntityElementTemp()
                        {
                            FrameEntity = frameEntity,
                            IndexPosition = frameBase.GetIndexPosition(),
                            RedEntity = redEntity,
                            YellowEntity = yellowEntity,
                            GreenEntity = greenEntity
                        });

                        frameBuffer.Add(new LightFrameEntityHolderElement()
                        {
                            FrameEntity = frameEntity
                        });
                    }

                    AddComponent(tempFrameBakingEntity, new TrafficLightObjectBakingData()
                    {
                        RelatedEntityHandler = relatedHandlerEntity,
                        FrameEntities = frameEntities.ToArray(Allocator.Temp)
                    });

                    frameEntities.Dispose();
                }
            }

            private string GetHeader(TrafficLightObjectAuthoring authoring) => $"TrafficLight '{authoring.name}' InstanceID {authoring.GetInstanceID()}";

#if UNITY_EDITOR
            private void InitBinding()
            {
                if (trafficLightCrossRoads != null && (trafficLightCrossRoads.Length == 0 || trafficLightCrossRoads[0] != null) && lightBinding != null)
                    return;

                trafficLightCrossRoads = Extensions.ObjectUtils.FindObjectsOfType<TrafficLightCrossroad>();
                lightBinding = new System.Collections.Generic.Dictionary<int, TrafficLightCrossroad>();

                foreach (var crossroad in trafficLightCrossRoads)
                {
                    if (crossroad.UniqueId != 0 && !lightBinding.ContainsKey(crossroad.UniqueId))
                        lightBinding.Add(crossroad.UniqueId, crossroad);
                }
            }
#endif
        }
    }
}