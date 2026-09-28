using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public partial class RoadSegmentCreator : MonoBehaviour
    {
        [Serializable]
        public class LightObjectData
        {
            public bool Enabled = true;
            public LightPrefabType SelectedLightPrefabType = LightPrefabType.FourWay;
            public LightLocation LightLocation = LightLocation.Right;
            public Vector3 TrafficLightOffset;
            public Vector3 PedestrianLightOffset;
            public List<float> AngleOffsets;
            public List<bool> FlipAngleOffsets;

            public int PedestrianAngleOffset = 90;

            public int LocalLightCount => LightLocation == LightLocation.RightLeft ? 2 : 1;

            public void Init()
            {
                int lightCount = LocalLightCount;

                if (AngleOffsets == null)
                {
                    AngleOffsets = new List<float>(lightCount);
                }

                if (AngleOffsets.Count < lightCount)
                {
                    int count = lightCount - AngleOffsets.Count;

                    for (int i = 0; i < count; i++)
                    {
                        AngleOffsets.Add(0);
                    }
                }

                if (FlipAngleOffsets == null)
                {
                    FlipAngleOffsets = new List<bool>(lightCount);
                }

                if (FlipAngleOffsets.Count < lightCount)
                {
                    int count = lightCount - FlipAngleOffsets.Count;

                    for (int i = 0; i < count; i++)
                    {
                        FlipAngleOffsets.Add(false);
                    }
                }
            }
        }
    }
}