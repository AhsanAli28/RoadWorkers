using Spirit604.Gameplay.Road;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using Spirit604.DotsCity.Simulation.Road.Authoring;
using Spirit604.Extensions;
#endif

namespace Spirit604.DotsCity.Simulation.Road
{
    public static class TrafficLightConverterUtils
    {
        private const string FramePatternName = "Frame_";

        public static TrafficLightObject AddLightComponents(GameObject trafficLight)
        {
#if UNITY_EDITOR
            var lightObject = trafficLight.gameObject.GetOrCreateComponent<TrafficLightObject>();
            var lightAuthoring = lightObject.gameObject.GetOrCreateComponent<TrafficLightObjectAuthoring>();
            lightAuthoring.Reset();

            return lightObject;
#else
            return null;
#endif
        }

        public static TrafficLightFrame CreateFrame(TrafficLightObject lightObject, int index)
        {
            var frame = new GameObject(GetFrameName(index)).AddComponent<TrafficLightFrame>();

            frame.TrafficLightObject = lightObject;
            frame.gameObject.transform.SetParent(lightObject.transform);
            frame.gameObject.transform.localPosition = Vector3.zero;
            frame.gameObject.transform.localRotation = Quaternion.identity;
            return frame;
        }

        public static string GetFrameName(int index)
        {
            return $"{FramePatternName}{index}";
        }

        public static Transform GetChild(Transform parent, string text)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);

                if (child.name.Contains(text))
                    return child;
            }

            return null;
        }

        public static List<Transform> GetChilds(Transform parent, string text)
        {
            var list = new List<Transform>();

            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);

                if (child.name.Contains(text))
                    list.Add(child);
            }

            return list;
        }
    }
}
