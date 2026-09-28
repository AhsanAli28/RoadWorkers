using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Spirit604.CityEditor.Road
{
    public partial class RoadSegmentCreator : MonoBehaviour
    {
        public LightObjectData GetLightSettings(int lightIndex) => lightIndex == -1 ? commonLightObjectData : GetLightData(lightIndex);

        public void AddLightOffset(Vector3 offset, LightType lightType)
        {
            var lightSettings = GetLightSettings(selectedLightNodeIndex);

            switch (lightType)
            {
                case LightType.Traffic:
                    lightSettings.TrafficLightOffset += offset;
                    break;
                case LightType.Pedestrian:
                    lightSettings.PedestrianLightOffset += offset;
                    break;
            }

#if UNITY_EDITOR
            OnLightSettingsChanged();
#endif
        }

        public void AddLightRotation(Quaternion offset, LightType lightType)
        {
            var lightSettings = GetLightSettings(selectedLightNodeIndex);

            switch (lightType)
            {
                case LightType.Traffic:
                    {
                        for (int i = 0; i < lightSettings.AngleOffsets?.Count; i++)
                        {
                            lightSettings.AngleOffsets[i] += offset.eulerAngles.y;
                            lightSettings.AngleOffsets[i] = ClampAngle(lightSettings.AngleOffsets[i]);
                        }

                        break;
                    }
                case LightType.Pedestrian:
                    {
                        lightSettings.PedestrianAngleOffset += Mathf.RoundToInt(offset.eulerAngles.y);
                        lightSettings.PedestrianAngleOffset = Mathf.RoundToInt(ClampAngle((float)lightSettings.PedestrianAngleOffset));
                        break;
                    }
            }

#if UNITY_EDITOR
            OnLightSettingsChanged();
#endif
        }

        private void CreatePedestrianLights()
        {
            for (int i = 0; i < createdTrafficNodes.Count; i++)
            {
                if (createdTrafficNodes[i].TrafficNodeType != TrafficNodeType.Default)
                {
                    continue;
                }

                for (int side = 1; side >= -1; side -= 2)
                {
                    var offset = GetPedestrianLightOffset(i);

                    var enabled = GetCrosswalkEnabledState(i);

                    if (!enabled)
                    {
                        continue;
                    }

                    var sideOffset = createdTrafficNodes[i].LaneCount * LaneWidth;

                    if (createdTrafficNodes[i].IsOneWay)
                    {
                        sideOffset /= 2;
                    }

                    var spawnPoint = createdTrafficNodes[i].transform.position + createdTrafficNodes[i].transform.rotation * new Vector3(-(offset.x + sideOffset) * side, offset.y, offset.z);

                    int rotator = side == -1 ? 1 : 0;
                    var spawnRotation = Quaternion.Euler(0, GetPedestrianAngleOffset(i) + createdTrafficNodes[i].transform.rotation.eulerAngles.y + 180 * rotator, 0);

                    GameObject pedestrianLight = null;

#if UNITY_EDITOR
                    pedestrianLight = PrefabUtility.InstantiatePrefab(roadSegmentCreatorConfig.PedestrianLightPrefab, GetLightParent(pedestrianLights)) as GameObject;
#endif

                    pedestrianLight.transform.position = spawnPoint;
                    pedestrianLight.transform.rotation = spawnRotation;

                    var trafficObjectsLight = pedestrianLight.GetComponentInChildren<TrafficLightObject>();

                    createdLights.Add(trafficObjectsLight);

                    lightBinding.Add(new LightObjectBindingData()
                    {
                        Index = i,
                        Side = -side,
                        LightType = LightType.Pedestrian,
                    });

                    var offsetIndex = i % 2 == 0 ? 1 : 0;
                    trafficObjectsLight.SetIndexOffset(offsetIndex);

                    trafficLightCrossroad.AddChildLight(trafficObjectsLight);

                    EditorSaver.SetObjectDirty(trafficLightCrossroad);
                    EditorSaver.SetObjectDirty(trafficObjectsLight);
                }
            }
        }

        private void CreateTrafficLights()
        {
            CheckLightData();

            for (int lightIndex = 0; lightIndex < createdTrafficNodes.Count; lightIndex++)
            {
                if (createdTrafficNodes[lightIndex].TrafficNodeType != TrafficNodeType.Default)
                {
                    continue;
                }

                var lightSettings = GetLightSettings(lightIndex);

                if (!lightSettings.Enabled)
                {
                    continue;
                }

                GameObject lightPrefab = GetLightPrefab(lightIndex);
                var lightLocation = GetLightLocation(lightIndex);

                int sideLightsCount = lightLocation == LightLocation.RightLeft ? 2 : 1;

                for (int j = 0; j < sideLightsCount; j++)
                {
                    var localLightIndex = j;
                    var offset = GetLightOffset(lightIndex);

                    int side = 1;

                    if (lightLocation == LightLocation.Left)
                    {
                        side = -1;
                    }
                    else if (lightLocation == LightLocation.RightLeft)
                    {
                        side = j == 0 ? -1 : side;
                    }

                    var point = createdTrafficNodes[lightIndex].transform.position - createdTrafficNodes[lightIndex].transform.rotation * new Vector3(LaneWidth / 2 + createdTrafficNodes[lightIndex].LaneCount * LaneWidth, 0) * side;

                    var spawnPoint = point + createdTrafficNodes[lightIndex].transform.rotation * new Vector3(offset.x * side, offset.y, offset.z);

                    var spawnRotation = Quaternion.Euler(0, GetLocalAngleOffset(lightIndex, localLightIndex), 0) * createdTrafficNodes[lightIndex].transform.rotation;

                    var trafficLight = CreateTrafficLight(lightPrefab, lightIndex, spawnPoint, spawnRotation, GetLightFlipState(lightIndex, localLightIndex));
                    createdLights.Add(trafficLight);

                    lightBinding.Add(new LightObjectBindingData()
                    {
                        Index = lightIndex,
                        Side = side,
                        LightType = LightType.Traffic,
                    });

                    trafficLightCrossroad.AddChildLight(trafficLight);

                    localLightIndex++;
                }
            }

            TrafficLightObject CreateTrafficLight(GameObject prefabToCreate, int lightIndex, Vector3 spawnPoint, Quaternion spawnRotation, bool reverted)
            {
                GameObject trafficLight = null;

#if UNITY_EDITOR
                trafficLight = PrefabUtility.InstantiatePrefab(prefabToCreate, GetLightParent(trafficLights)) as GameObject;
#endif

                trafficLight.transform.position = spawnPoint;
                trafficLight.transform.rotation = spawnRotation;

                TrafficLightObject trafficObjectsLight = trafficLight.GetComponentInChildren<TrafficLightObject>();

                int index = 0;

                if (!reverted)
                {
                    index = lightIndex % 2 == 0 ? 0 : 1;
                }
                else
                {
                    index = lightIndex % 2 == 0 ? 1 : 0;
                }

                trafficObjectsLight.SetIndexOffset(index);

                EditorSaver.SetObjectDirty(trafficObjectsLight);
                return trafficObjectsLight;
            }
        }

        private void CheckLightData()
        {
            int defaultNodeCount = createdTrafficNodes.Where(a => a && a.TrafficNodeType == TrafficNodeType.Default).Count();
            int lightCount = defaultNodeCount;

            if (lightObjectDatas == null || lightObjectDatas.Length != lightCount)
            {
                lightObjectDatas = new LightObjectData[lightCount];

                for (int i = 0; i < lightCount; i++)
                {
                    lightObjectDatas[i] = new LightObjectData();
                }
            }

            for (int i = 0; i < lightObjectDatas.Length; i++)
            {
                if (lightObjectDatas[i] == null)
                {
                    lightObjectDatas[i] = new LightObjectData();
                }

                lightObjectDatas[i].Init();
            }

            if (createdLights.Count != lightBinding.Count)
            {
#if UNITY_EDITOR
                OnLightSettingsChanged();
#endif
            }
        }

        private Transform GetLightParent(Transform sourceParent)
        {
#if UNITY_EDITOR
            if (currentLightParent)
            {
                return currentLightParent;
            }
#endif

            return sourceParent;
        }

        private LightObjectData GetLightData(int lightIndex)
        {
            if (lightObjectDatas == null || lightObjectDatas.Length <= lightIndex)
            {
                CheckLightData();
            }

            return lightObjectDatas[lightIndex];
        }

        private Vector3 GetLightOffset(int lightIndex) => commonLightObjectData.TrafficLightOffset + GetLightData(lightIndex).TrafficLightOffset;

        private Vector3 GetPedestrianLightOffset(int lightIndex) => commonLightObjectData.PedestrianLightOffset + GetLightData(lightIndex).PedestrianLightOffset;

        private GameObject GetLightPrefab(int lightIndex) => roadSegmentCreatorConfig.LightPrefabs[GetLightData(lightIndex).SelectedLightPrefabType];

        private LightLocation GetLightLocation(int lightIndex) => GetLightData(lightIndex).LightLocation;

        private int GetPedestrianAngleOffset(int lightIndex) => GetLightData(lightIndex).PedestrianAngleOffset;

        private float GetLocalAngleOffset(int lightIndex, int localIndex) => GetLightData(lightIndex).AngleOffsets[localIndex];

        private bool GetLightFlipState(int lightIndex, int localIndex) => GetLightData(lightIndex).FlipAngleOffsets[localIndex];

        private float ClampAngle(float angle)
        {
            if (angle > 360)
            {
                angle -= 360;
            }

            if (angle < 0)
            {
                angle += 360f;
            }

            return MathF.Round(angle, 1);
        }
    }
}