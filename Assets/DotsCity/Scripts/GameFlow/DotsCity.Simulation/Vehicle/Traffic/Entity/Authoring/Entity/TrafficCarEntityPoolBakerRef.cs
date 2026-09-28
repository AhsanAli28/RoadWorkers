using Spirit604.Attributes;
using Spirit604.Collections.Dictionary;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Car.Authoring;
using Spirit604.DotsCity.Simulation.Factory;
using Spirit604.DotsCity.Simulation.Factory.Car;
using Spirit604.DotsCity.Simulation.Factory.Traffic;
using Spirit604.Extensions;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic.Authoring
{
    public class TrafficEntityPoolBakerRef : SyncConfigBase
    {
        [System.Serializable]
        public class TrafficPresetDictionary : AbstractSerializableDictionary<EntityType, TrafficCarPoolPreset> { }

        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/trafficPreset.html")]
        [SerializeField] private string link;

        [SerializeField] private VehicleDataHolder vehicleDataHolder;

        [OnValueChanged(nameof(Sync))]
        [SerializeField] private TrafficPresetDictionary presetData = new TrafficPresetDictionary();

        public TrafficPresetDictionary PresetData { get => presetData; set => presetData = value; }

        public ICollection<EntityType> AddedEntityTypes => presetData.Keys;

        public List<CarPrefabPair> GetPoolData(EntityType trafficEntityType)
        {
            if (presetData.ContainsKey(trafficEntityType))
            {
                return presetData[trafficEntityType].PrefabDatas;
            }

            return null;
        }

        public TrafficCarPoolPreset GetPreset(EntityType trafficEntityType)
        {
            if (presetData.ContainsKey(trafficEntityType))
            {
                return presetData[trafficEntityType];
            }

            return null;
        }

        public bool SetPreset(TrafficCarPoolPreset trafficCarPoolPreset, EntityType trafficEntityType)
        {
            if (presetData.ContainsKey(trafficEntityType))
            {
                presetData[trafficEntityType] = trafficCarPoolPreset;
            }
            else
            {
                presetData.Add(trafficEntityType, trafficCarPoolPreset);
            }

            EditorSaver.SetObjectDirty(this);

            return true;
        }

        public void SetData(EntityType[] entityTypes, TrafficCarPoolPreset[] trafficCarPoolPresets)
        {
            presetData = new TrafficEntityPoolBakerRef.TrafficPresetDictionary();
            presetData.SetDictionary(entityTypes, trafficCarPoolPresets);
        }

        public class TrafficEntityPoolBaker : Baker<TrafficEntityPoolBakerRef>
        {
            public override void Bake(TrafficEntityPoolBakerRef authoring)
            {
                if (authoring.vehicleDataHolder == null || authoring.vehicleDataHolder.VehicleDataCollection == null)
                {
                    UnityEngine.Debug.LogError("TrafficEntityPoolBaker. TrafficPoolPreset VehicleDataCollection is null");
                    return;
                }

                foreach (var presetData in authoring.presetData)
                {
                    var preset = presetData.Value;

                    if (!preset)
                    {
                        Debug.LogError($"TrafficEntityPoolBaker. TrafficPoolPreset '{presetData.Key}' is null");
                    }

                    int localIndex = 0;

                    for (int i = 0; i < preset.PrefabDatas.Count; i++)
                    {
                        Factory.CarPrefabPair item = preset.PrefabDatas[i];

                        if (item == null || item.EntityPrefab == null)
                        {
                            Debug.LogError($"TrafficEntityPoolBaker. Preset '{preset.name}' LocalIndex {i} Prefab Entity is null");
                            continue;
                        }

                        int modelIndex = authoring.vehicleDataHolder.VehicleDataCollection.GetVehicleModelIndex(item.EntityPrefab);

                        if (modelIndex == -1)
                        {
                            bool hasId = false;

                            var idProvider = item.EntityPrefab.GetComponent<ICarIDProvider>();

                            if (idProvider != null)
                            {
                                hasId = !string.IsNullOrEmpty(idProvider.ID);
                            }

                            if (hasId)
                            {
                                UnityEngine.Debug.LogError($"TrafficEntityPoolBaker. Preset '{preset.name}' Vehicle {item.EntityPrefab.name} model index not found. Make sure you have added a vehicle ID to the vehicle collection.");
                            }
                            else
                            {
                                UnityEngine.Debug.LogError($"TrafficEntityPoolBaker. Preset '{preset.name}' Vehicle {item.EntityPrefab.name} ID is null. Make sure the vehicle implements 'ICarIDProvider' interface & has assigned mesh.");
                            }

                            continue;
                        }

                        var entityPrefabContainer = CreateAdditionalEntity(TransformUsageFlags.None);
                        var prefabEntity = GetEntity(item.EntityPrefab, TransformUsageFlags.Dynamic);

                        AddComponent(entityPrefabContainer, new TrafficPrefabData()
                        {
                            ModelIndex = modelIndex,
                            PrefabEntity = prefabEntity,
                            Weight = item.Weight,
                        });

                        AddSharedComponent(entityPrefabContainer, new TrafficPrefabSort()
                        {
                            TrafficEntityType = presetData.Key
                        });

                        AddComponent(entityPrefabContainer, new CarModelBakingData()
                        {
                            VehicleEntity = prefabEntity,
                            CarModel = modelIndex,
                            LocalIndex = localIndex,
                        });

                        if (item.HullPrefab != null && item.HullPrefab.TryGetComponent<IVehicleSettingsProvider>(out var settings))
                        {
                            AddComponent(entityPrefabContainer, new CustomSettingsBakingData()
                            {
                                VehicleEntity = prefabEntity,
                                MaxCarSpeed = settings.MaxSpeed / ProjectConstants.KmhToMs_RATE,
                                MaxSteeringAngle = Mathf.Deg2Rad * settings.MaxSteeringAngle,
                            });
                        }

                        localIndex++;
                    }
                }
            }
        }
    }
}