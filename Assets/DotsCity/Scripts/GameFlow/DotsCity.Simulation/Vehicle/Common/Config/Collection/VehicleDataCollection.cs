using Spirit604.CityEditor;
using Spirit604.Collections.Dictionary;
using Spirit604.DotsCity.Simulation.Car.Sound;
using Spirit604.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car
{
    [CreateAssetMenu(fileName = "VehicleDataCollection", menuName = CityEditorBookmarks.CITY_EDITOR_LEVEL_CONFIG_LEVEL_PATH + "Traffic/VehicleDataCollection")]
    public class VehicleDataCollection : ScriptableObject
    {
        [Serializable]
        public class VehicleDataDictionary : AbstractSerializableDictionary<string, VehicleData> { }

        public enum ShowType { All, ByIndex, Toolbar, ByName }

        public enum SettingsType
        {
            None = 0,
            CustomEngine = 1 << 0,
            CustomSound = 1 << 1,
            Overwrite = 1 << 2 & ~(CustomEngine | CustomSound),
        }

        [Range(0f, 5f)]
        [Tooltip("Minimum pitch of the car engine")]
        [SerializeField]
        private float minPitch = 0.6f;

        [Range(0f, 5f)]
        [Tooltip("Maximum pitch of the car engine")]
        [SerializeField]
        private float maxPitch = 3f;

        [Range(0f, 100f)]
        [Tooltip("Speed at which the engine has the maximum pitch")]
        [SerializeField]
        private float maxLoadSpeed = 60f;

        [Range(0f, 100f)]
        [Tooltip("Speed at which the engine has the maximum volume")]
        [SerializeField]
        private float maxVolumeSpeed = 20f;

        [Range(0f, 1f)]
        [Tooltip("Minimum engine volume")]
        [SerializeField]
        private float minVolume = 0.4f;

        [SerializeField]
        private CarSoundDictionary sharedCarSoundData = new CarSoundDictionary()
        {
            { CarSoundType.Ignition, null },
            { CarSoundType.Idle, null },
            { CarSoundType.Driving, null },
            { CarSoundType.Horn, null },
            { CarSoundType.EnterCar, null },
            { CarSoundType.ExitCar, null },
        };

        [SerializeField] private List<VehicleData> vehicleDataList = new List<VehicleData>();

        [Tooltip("List of vehicle unique ID's (linked to 'vehicleDataList' like a dictionary)")]
        [SerializeField] private List<string> vehicleDataKeys = new List<string>();

        [SerializeField] private string[] cachedOptions = new string[0];

#pragma warning disable 0414

        [SerializeField] private bool showCommonSettings = true;
        [SerializeField] private bool showCustomData = true;
        [SerializeField] private ShowType showType;
        [SerializeField] private string searchName;
        [SerializeField] private int showIndex = -1;
        [SerializeField] private bool showSoundData = true;
        [SerializeField] private bool showVehicleData;

#pragma warning restore 0414

        private List<int> showIndexes = new List<int>();

        public string[] Options
        {
            get
            {
                if (cachedOptions.Length != vehicleDataList.Count)
                {
                    UpdateCache();
                }

                return cachedOptions;
            }
        }

        public List<VehicleData> VehicleDataList => vehicleDataList;

        public List<string> VehicleDataKeys { get => vehicleDataKeys; }

        public CarSoundDictionary SharedCarSoundData => sharedCarSoundData;

        public ShowType CurrentShowType { get => showType; set => showType = value; }

        public float MinPitch { get => minPitch; set => minPitch = value; }

        public float MaxPitch { get => maxPitch; set => maxPitch = value; }

        public float MaxLoadSpeed { get => maxLoadSpeed; set => maxLoadSpeed = value; }

        public float MaxVolumeSpeed { get => maxVolumeSpeed; set => maxVolumeSpeed = value; }

        public float MinVolume { get => minVolume; set => minVolume = value; }

        public bool AddData(string name, string id, bool report = false)
        {
            if (!vehicleDataKeys.Contains(id))
            {
                vehicleDataKeys.Add(id);
                vehicleDataList.Add(new VehicleData()
                {
                    Name = name,
                    CarSoundData = new CarSoundDictionary()
                });

                UpdateCache();
                EditorSaver.SetObjectDirty(this);

                if (report)
                {
                    UnityEngine.Debug.Log($"VehicleDataCollection. ID '{id}' added.");
                }

                return true;
            }

            return false;
        }

        public void SetClone(string sourceID, string clonedID)
        {
            var sourceData = GetDataByID(sourceID);
            var cloneData = GetDataByID(clonedID);

            if (sourceData != null && cloneData != null)
            {
                sourceData.SourceVehicleID = clonedID;
                sourceData.SettingsType = SettingsType.Overwrite;

                EditorSaver.SetObjectDirty(this);
            }
            else
            {
                DiscardClone(sourceID);
            }
        }

        public void DiscardClone(string sourceID)
        {
            var sourceData = GetDataByID(sourceID);

            if (sourceData != null)
            {
                sourceData.SourceVehicleID = string.Empty;
                sourceData.SettingsType = sourceData.SettingsType.RemoveFlag(SettingsType.Overwrite);
                EditorSaver.SetObjectDirty(this);
            }
        }

        public bool RemoveData(string id)
        {
            var allDataIndex = vehicleDataKeys.IndexOf(id);
            return RemoveDataAt(allDataIndex);
        }

        public bool RemoveDataAt(int removeIndex)
        {
            if (removeIndex >= 0 && vehicleDataKeys.Count > removeIndex)
            {
                vehicleDataKeys.RemoveAt(removeIndex);
                vehicleDataList.RemoveAt(removeIndex);
                UpdateCache();
                EditorSaver.SetObjectDirty(this);
                return true;
            }

            return false;
        }

        public bool Contains(string meskKey) => vehicleDataKeys.Contains(meskKey);

        public void UpdateName(int carModelIndex, string name)
        {
            vehicleDataList[carModelIndex].Name = name;
            UpdateCache(carModelIndex, name);

            EditorSaver.SetObjectDirty(this);
        }

        public void UpdateCache(int carModelIndex, string name)
        {
            if (cachedOptions == null || cachedOptions.Length <= carModelIndex)
            {
                UpdateCache();
            }

            cachedOptions[carModelIndex] = name;
        }

        public void UpdateCache()
        {
            cachedOptions = vehicleDataList.Select(a => a.Name).ToArray();
        }

        public void ClearData(bool saveUndo = true)
        {
            if (saveUndo)
            {
#if UNITY_EDITOR
                Undo.RegisterCompleteObjectUndo(this, "Undo clear");
#endif
            }

            vehicleDataList.Clear();
            vehicleDataKeys.Clear();
        }

        public int GetCarModelIndexByID(string id)
        {
            return vehicleDataKeys.IndexOf(id);
        }

        /// <param name="idHash">Hash is taken from id.GetHashCode() </param>
        public int GetLocalIndexByHash(int idHash)
        {
            for (int i = 0; i < vehicleDataKeys.Count; i++)
            {
                if (vehicleDataKeys[i].GetHashCode() == idHash)
                {
                    return i;
                }
            }

            return -1;
        }

        public void UpdateSearchIndexes()
        {
            showIndexes.Clear();

            if (string.IsNullOrEmpty(searchName))
            {
                return;
            }

            for (int i = 0; i < vehicleDataList.Count; i++)
            {
                var name = vehicleDataList[i].Name;

                if (name.Contains(searchName, StringComparison.OrdinalIgnoreCase))
                {
                    showIndexes.TryToAdd(i);
                }
            }
        }

        public bool CanShow(int carModelIndex)
        {
            if (CurrentShowType == ShowType.ByName)
            {
                return showIndexes.Contains(carModelIndex);
            }

            return CurrentShowType == ShowType.All || showIndex == -1 || showIndex == carModelIndex;
        }

        public string GetName(int carModelIndex)
        {
            if (carModelIndex >= 0 && carModelIndex < vehicleDataKeys.Count)
            {
                return vehicleDataKeys[carModelIndex];
            }

            return "NaN";
        }

        private VehicleData GetDataByID(string id)
        {
            var carModelIndex = GetCarModelIndexByID(id);

            if (carModelIndex >= 0 && carModelIndex < vehicleDataList.Count)
            {
                return vehicleDataList[carModelIndex];
            }

            return null;
        }
    }
}
