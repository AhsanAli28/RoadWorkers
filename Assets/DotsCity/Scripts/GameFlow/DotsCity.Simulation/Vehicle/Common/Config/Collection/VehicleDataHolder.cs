using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car.Sound;
using Spirit604.Extensions;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car
{
    public class VehicleDataHolder : SyncConfigBase
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/vehicleCollection.html")]
        [SerializeField] private string link;

        [OnValueChanged(nameof(Sync))]
        [SerializeField] private VehicleDataCollection vehicleDataCollection;

        public VehicleDataCollection VehicleDataCollection { get => vehicleDataCollection; private set => vehicleDataCollection = value; }

#if UNITY_EDITOR
        [field: SerializeField] public ScriptableObject CacheContainer { get; private set; }
#endif

        public void SetConfig(VehicleDataCollection vehicleDataCollection)
        {
            if (VehicleDataCollection != vehicleDataCollection)
            {
                VehicleDataCollection = vehicleDataCollection;
                EditorSaver.SetObjectDirty(this);
            }
        }

        public void SetConfig(ScriptableObject cacheContainer)
        {
#if UNITY_EDITOR
            if (CacheContainer != cacheContainer)
            {
                CacheContainer = cacheContainer;
                EditorSaver.SetObjectDirty(this);
            }
#endif
        }

        class VehicleDataHolderBaker : Baker<VehicleDataHolder>
        {
            public override void Bake(VehicleDataHolder authoring)
            {
                this.DependsOn(authoring.VehicleDataCollection);

                if (authoring.VehicleDataCollection == null)
                {
                    return;
                }

                var entity = CreateAdditionalEntity(TransformUsageFlags.Dynamic);

                int configCount = 1;

                var collection = authoring.VehicleDataCollection;
                var vehicleDataList = collection.VehicleDataList;
                int vehicleCount = collection.VehicleDataList.Count;

                int customConfigCount = collection.VehicleDataList.Count(a => a.GetSettingsFlag().HasFlag(VehicleDataCollection.SettingsType.CustomSound));
                int customEngineConfigCount = collection.VehicleDataList.Count(a => a.GetSettingsFlag().HasFlag(VehicleDataCollection.SettingsType.CustomEngine)) + 1;
                var sharedCarSoundData = collection.SharedCarSoundData;
                int soundCount = collection.SharedCarSoundData.Keys.Count;

                configCount += customConfigCount;

                using (var builder = new BlobBuilder(Allocator.Temp))
                {
                    ref var root = ref builder.ConstructRoot<CarSharedConfig>();

                    var carDataArray = builder.Allocate(ref root.CarDatas, vehicleCount);
                    var soundDataArray = builder.Allocate(ref root.SoundConfigs, configCount);
                    var engineDataArray = builder.Allocate(ref root.SoundEngineConfigs, customEngineConfigCount);

                    var sharedSounds = builder.Allocate(ref soundDataArray[0].Sounds, soundCount);

                    for (int i = 0; i < soundCount; i++)
                    {
                        var soundType = (CarSoundType)i;
                        int soundId = -1;

                        if (sharedCarSoundData.TryGetValue(soundType, out var soundData) && soundData != null)
                        {
                            soundId = soundData.Id;
                        }
                        else
                        {
                            UnityEngine.Debug.Log($"VehicleDataHolderBaker. {soundType} sound is missing");
                        }

                        sharedSounds[i] = new BlobSoundData()
                        {
                            SoundId = soundId
                        };
                    }

                    engineDataArray[0] = new BlobEngineSoundData()
                    {
                        MinPitch = collection.MinPitch,
                        MaxPitch = collection.MaxPitch,
                        MaxLoadSpeed = collection.MaxLoadSpeed,
                        MaxVolumeSpeed = collection.MaxVolumeSpeed,
                        MinVolume = collection.MinVolume
                    };

                    int soundConfigIndex = 0;
                    int soundEngineIndex = 0;

                    Dictionary<int, List<int>> awaitingSoundIndexes = null;

                    for (int i = 0; i < vehicleCount; i++)
                    {
                        var data = vehicleDataList[i];

                        var flag = data.GetSettingsFlag();

                        if (!flag.HasFlag(VehicleDataCollection.SettingsType.Overwrite))
                        {
                            if (!flag.HasFlag(VehicleDataCollection.SettingsType.CustomSound))
                            {
                                carDataArray[i].SoundConfigIndex = 0;
                            }
                            else
                            {
                                carDataArray[i].SoundConfigIndex = ++soundConfigIndex;

                                var customSounds = builder.Allocate(ref soundDataArray[soundConfigIndex].Sounds, soundCount);

                                for (int j = 0; j < soundCount; j++)
                                {
                                    var soundType = (CarSoundType)j;
                                    int soundId = -1;

                                    if (data.CarSoundData.ContainsKey(soundType) && data.CarSoundData[soundType] != null)
                                    {
                                        var soundData = data.CarSoundData[soundType];
                                        soundId = soundData.Id;
                                    }
                                    else if (sharedCarSoundData.TryGetValue(soundType, out var soundData) && soundData != null)
                                    {
                                        soundId = soundData.Id;
                                    }

                                    customSounds[j] = new BlobSoundData()
                                    {
                                        SoundId = soundId
                                    };
                                }
                            }

                            if (!flag.HasFlag(VehicleDataCollection.SettingsType.CustomEngine))
                            {
                                carDataArray[i].SoundEngineIndex = 0;
                            }
                            else
                            {
                                carDataArray[i].SoundEngineIndex = ++soundEngineIndex;

                                engineDataArray[soundEngineIndex] = new BlobEngineSoundData()
                                {
                                    MinPitch = data.MinPitch,
                                    MaxPitch = data.MaxPitch,
                                    MaxLoadSpeed = data.MaxLoadSpeed,
                                    MaxVolumeSpeed = data.MaxVolumeSpeed,
                                    MinVolume = data.MinVolume
                                };
                            }

                            if (awaitingSoundIndexes != null && awaitingSoundIndexes.ContainsKey(i))
                            {
                                var list = awaitingSoundIndexes[i];

                                for (int p = 0; p < list.Count; p++)
                                {
                                    var copyIndex = list[p];

                                    carDataArray[copyIndex].SoundConfigIndex = carDataArray[i].SoundConfigIndex;
                                    carDataArray[copyIndex].SoundEngineIndex = carDataArray[i].SoundEngineIndex;
                                }

                                awaitingSoundIndexes.Remove(i);
                            }
                        }
                        else
                        {
                            var vehicleLocalIndex = collection.GetCarModelIndexByID(data.SourceVehicleID);

                            if (vehicleLocalIndex != -1)
                            {
                                if (i > vehicleLocalIndex)
                                {
                                    carDataArray[i].SoundConfigIndex = carDataArray[vehicleLocalIndex].SoundConfigIndex;
                                }
                                else
                                {
                                    if (awaitingSoundIndexes == null)
                                    {
                                        awaitingSoundIndexes = new Dictionary<int, List<int>>();
                                    }

                                    if (!awaitingSoundIndexes.ContainsKey(vehicleLocalIndex))
                                    {
                                        awaitingSoundIndexes.Add(vehicleLocalIndex, new List<int>());
                                    }

                                    awaitingSoundIndexes[vehicleLocalIndex].Add(i);
                                }
                            }
                        }
                    }

                    var blobRef = builder.CreateBlobAssetReference<CarSharedConfig>(Allocator.Persistent);

                    AddBlobAsset(ref blobRef, out var hash);

                    AddComponent(entity, new CarSharedDataConfigReference() { Config = blobRef });
                }
            }
        }
    }
}