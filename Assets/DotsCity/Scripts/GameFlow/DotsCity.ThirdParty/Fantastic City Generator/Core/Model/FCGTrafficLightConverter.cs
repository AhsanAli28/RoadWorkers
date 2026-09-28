#if FCG
using FCG;
using Spirit604.Attributes;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.DotsCity.Simulation.Road.Authoring;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.FCG
{
    public class FCGTrafficLightConverter : MonoBehaviourBase
    {
        private const string RedName1 = "P1-Red";
        private const string RedName2 = "P2-Red";
        private const string GreenName1 = "P1-Green";
        private const string GreenName2 = "P2-Green";

        private const string GreenName = "Green";
        private const string RedName = "Red";
        private const string YellowName = "Yellow";

        [SerializeField] private TrafficLights2 trafficLights;

        [Button]
        public void Destroy()
        {
            DestroyInternal(true);
        }

        [Button]
        public void Create()
        {
            CreateInternal(true);
        }

        public void CreateInternal(bool report)
        {
            if (!trafficLights.enabled)
            {
                if (report)
                    UnityEngine.Debug.Log("Traffic Light already converted");

                return;
            }

            trafficLights.enabled = false;
            Create(trafficLights.trafficLight_N, 1);
            Create(trafficLights.trafficLight_S, 1);
            Create(trafficLights.trafficLight_E, 0);
            Create(trafficLights.trafficLight_W, 0);

            if (report)
                UnityEngine.Debug.Log("Traffic Light successfully converted");
        }

        public void DestroyInternal(bool report)
        {
            if (trafficLights.enabled)
            {
                if (report)
                    UnityEngine.Debug.Log("DOTS Traffic Light doesn't exist");

                return;
            }

            trafficLights.enabled = true;
            Destroy(trafficLights.trafficLight_N, 1);
            Destroy(trafficLights.trafficLight_S, 1);
            Destroy(trafficLights.trafficLight_E, 0);
            Destroy(trafficLights.trafficLight_W, 0);

            if (report)
                UnityEngine.Debug.Log("DOTS Traffic Light successfully destroyed");
        }

        public void Create(TrafficLight trafficLight, int lightIndex)
        {
            if (!trafficLight.gameObject.activeSelf)
                return;

            CreateTrafficLight(trafficLight, lightIndex);
            CreatePedetrianLight(trafficLight);
        }

        public void Destroy(TrafficLight trafficLight, int lightIndex)
        {
            CreateTrafficLight(trafficLight, lightIndex, false);
            CreatePedetrianLight(trafficLight, false);
        }

        private void CreateTrafficLight(TrafficLight trafficLight, int lightIndex, bool create = true)
        {
            if (create)
            {
                var p1 = TrafficLightConverterUtils.GetChild(trafficLight.Green.transform, RedName1);

                if (p1)
                {
                    Instantiate(p1, trafficLight.Pedestrians.transform);
                }

                var p2 = TrafficLightConverterUtils.GetChild(trafficLight.Green.transform, RedName2);

                if (p2)
                {
                    Instantiate(p2, trafficLight.Pedestrians.transform);
                }
            }
            else
            {
                var clones = TrafficLightConverterUtils.GetChilds(trafficLight.Pedestrians.transform, "Clone");

                clones.DestroyGameObjects();
            }

            TryToSwitchChild(trafficLight.Green.transform, RedName1, !create);
            TryToSwitchChild(trafficLight.Green.transform, RedName2, !create);

            TryToSwitchChild(trafficLight.Red.transform, RedName1, !create);
            TryToSwitchChild(trafficLight.Red.transform, RedName2, !create);

            TryToSwitchChild(trafficLight.Yellow.transform, RedName1, !create);
            TryToSwitchChild(trafficLight.Yellow.transform, RedName2, !create);

            TryToSwitchChild(trafficLight.Pedestrians.transform, RedName, !create);

            trafficLight.StopCollider?.gameObject.SetActive(!create);
            trafficLight.StopPedestrianCollider?.gameObject.SetActive(!create);

            if (create)
            {
#if UNITY_EDITOR
                var lightObject = TrafficLightConverterUtils.AddLightComponents(trafficLight.gameObject);

                TrafficLightFrame frame = TrafficLightConverterUtils.CreateFrame(lightObject, lightIndex);

                frame.RedLight = TrafficLightConverterUtils.GetChild(trafficLight.Red.transform, RedName).gameObject;
                frame.YellowLight = TrafficLightConverterUtils.GetChild(trafficLight.Yellow.transform, YellowName).gameObject;
                frame.GreenLight = TrafficLightConverterUtils.GetChild(trafficLight.Green.transform, GreenName).gameObject;

                if (lightIndex == 0)
                    frame.IndexDirection = new Vector3(1, 0, 0);
                else
                    frame.IndexDirection = new Vector3(0, 0, 1);

                lightObject.AddFrame(lightIndex, frame);
                EditorSaver.SetObjectDirty(frame);
#endif
            }
            else
            {
                DestroyLightObject(trafficLight.transform);
            }
        }

        private void DestroyLightObject(Transform lightParent)
        {
            var lightObject = lightParent.gameObject.GetComponent<TrafficLightObject>();
            var lightAuthoring = lightParent.gameObject.GetComponent<TrafficLightObjectAuthoring>();

            if (lightObject)
            {
                var frames = lightObject.TrafficLightFrames;

                if (frames != null)
                {
                    foreach (var frameData in frames)
                    {
                        if (frameData.Value != null && frameData.Value.TrafficLightFrames != null)
                        {
                            foreach (var frame in frameData.Value.TrafficLightFrames)
                            {
                                if (frame)
                                {
                                    DestroyImmediate(frame.gameObject);
                                }
                            }
                        }
                    }
                }

                DestroyImmediate(lightObject);
            }

            if (lightAuthoring)
            {
                DestroyImmediate(lightAuthoring);
            }
        }

        private void CreatePedetrianLight(TrafficLight trafficLight, bool create = true)
        {
            if (create)
            {
#if UNITY_EDITOR
                var pedLight = TrafficLightConverterUtils.AddLightComponents(trafficLight.Pedestrians.gameObject);

                var pedestrianParent = trafficLight.Pedestrians.transform;

                var pedRedP1 = TrafficLightConverterUtils.GetChild(pedestrianParent, RedName1);
                var pedGreenP1 = TrafficLightConverterUtils.GetChild(pedestrianParent, GreenName1);

                if (pedRedP1 && pedGreenP1)
                {
                    int frameIndex1 = 0;

                    if (trafficLight.name.Contains("(S)") && trafficLights.transform.parent != null && trafficLights.transform.parent.name.Contains("Light T"))
                    {
                        frameIndex1 = 1;
                    }

                    TrafficLightFrame pedFrame1 = TrafficLightConverterUtils.CreateFrame(pedLight, frameIndex1);
                    pedFrame1.InitialLightIndex = frameIndex1;
                    pedFrame1.RedLight = pedRedP1.gameObject;
                    pedFrame1.GreenLight = pedGreenP1.gameObject;
                    pedFrame1.IndexDirection = new Vector3(0, 0, 1);
                    pedLight.AddFrame(frameIndex1, pedFrame1);
                    EditorSaver.SetObjectDirty(pedFrame1);
                }

                var pedRedP2 = TrafficLightConverterUtils.GetChild(pedestrianParent, RedName2);
                var pedGreenP2 = TrafficLightConverterUtils.GetChild(pedestrianParent, GreenName2);

                if (pedRedP2 && pedGreenP2)
                {
                    const int frameIndex2 = 1;
                    TrafficLightFrame pedFrame2 = TrafficLightConverterUtils.CreateFrame(pedLight, frameIndex2);
                    pedFrame2.InitialLightIndex = frameIndex2;
                    pedFrame2.RedLight = pedRedP2.gameObject;
                    pedFrame2.GreenLight = pedGreenP2.gameObject;
                    pedFrame2.IndexDirection = new Vector3(1, 0, 0);
                    pedLight.AddFrame(frameIndex2, pedFrame2);
                    EditorSaver.SetObjectDirty(pedFrame2);
                }
#endif
            }
            else
            {
                DestroyLightObject(trafficLight.Pedestrians.transform);
            }
        }

        private void TryToSwitchChild(Transform parent, string text, bool isActive)
        {
            var child = TrafficLightConverterUtils.GetChild(parent, text);

            if (child)
            {
                child.gameObject.SetActive(isActive);
            }
        }

        public void Reset()
        {
            trafficLights = GetComponentInChildren<TrafficLights2>();
            EditorSaver.SetObjectDirty(this);
        }
    }
}
#endif