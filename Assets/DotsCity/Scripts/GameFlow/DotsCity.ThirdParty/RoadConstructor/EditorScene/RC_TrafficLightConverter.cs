using Spirit604.Attributes;
using Spirit604.DotsCity.Simulation.Road;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using UnityEngine;

namespace Spirit604.DotsCity.ThirdParty.RoadConstructor
{
    public class RC_TrafficLightConverter : MonoBehaviourBase
    {
        private const string CarGreen = "Car_Green";
        private const string CarRed = "Car_Red";
        private const string CarYellow = "Car_Yellow";
        private const string PedestrianRed = "Pedestrian_Stop";
        private const string PedestrianGreen = "Pedestrian_Walk";

        public TrafficLightObject trafficLight;
        public TrafficLightObject pedestrianLight;

        [Button]
        public void Create()
        {
            var component = GetComponent("PampelGames.RoadConstructor.TrafficLightSwitch") as MonoBehaviour;

            if (component == null)
            {
                Debug.LogError("TrafficLightSwitch not found");
                return;
            }

            component.enabled = false;

            CreateTrafficLight();
            CreatePedestrianLight();

            EditorSaver.SetObjectDirty(this);

            Debug.Log($"{name} converted.");
        }

        private void CreateTrafficLight()
        {
            const int lightIndex = 0;

            trafficLight = TrafficLightConverterUtils.AddLightComponents(gameObject);
            TrafficLightFrame frame = TrafficLightConverterUtils.CreateFrame(trafficLight, lightIndex);

            frame.RedLight = TrafficLightConverterUtils.GetChild(trafficLight.transform, CarRed).gameObject;
            frame.YellowLight = TrafficLightConverterUtils.GetChild(trafficLight.transform, CarYellow).gameObject;
            frame.GreenLight = TrafficLightConverterUtils.GetChild(trafficLight.transform, CarGreen).gameObject;

            frame.IndexDirection = GetDirection(lightIndex);

            trafficLight.AddFrame(lightIndex, frame);
            EditorSaver.SetObjectDirty(frame);
        }

        private void CreatePedestrianLight()
        {
            const int lightIndex = 1;

            var pedestrianLightObj = TrafficLightConverterUtils.GetChild(gameObject.transform, "PedestrianLight");

            if (pedestrianLightObj == null)
            {
                pedestrianLightObj = new GameObject("PedestrianLight").transform;

#if UNITY_EDITOR
                try
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MoveGameObjectToScene(pedestrianLightObj.gameObject, gameObject.scene);
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                }

                pedestrianLightObj.SetParent(gameObject.transform);
                pedestrianLightObj.localPosition = default;
                pedestrianLightObj.localRotation = Quaternion.identity;
#endif
            }

            pedestrianLight = TrafficLightConverterUtils.AddLightComponents(pedestrianLightObj.gameObject);
            TrafficLightFrame frame = TrafficLightConverterUtils.CreateFrame(pedestrianLight, lightIndex);

            frame.RedLight = TrafficLightConverterUtils.GetChild(gameObject.transform, PedestrianRed).gameObject;
            frame.GreenLight = TrafficLightConverterUtils.GetChild(gameObject.transform, PedestrianGreen).gameObject;

            frame.IndexDirection = GetDirection(lightIndex);

            pedestrianLight.AddFrame(lightIndex, frame);
            EditorSaver.SetObjectDirty(frame);
        }

        private static Vector3 GetDirection(int lightIndex)
        {
            if (lightIndex == 0)
                return new Vector3(-1, 0, 0);
            else
                return new Vector3(0, 0, 1);
        }
    }
}
