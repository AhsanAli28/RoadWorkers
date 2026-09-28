using Spirit604.CityEditor;
using Spirit604.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Spirit604.Gameplay.Road
{
    public class TrafficGroupMaskSettings : ScriptableObject
    {
        private const string DefaultName = "TrafficGroupMaskSettings";

        [Serializable]
        public class CustomGroupData
        {
            public string GroupName;
            public TrafficGroupType TrafficGroup;
        }

        public TrafficGroupType DefaultGroup = ~TrafficGroupType.Tram;

        public List<CustomGroupData> CustomGroups = new List<CustomGroupData>()
        {
            new CustomGroupData()
            {
                GroupName = "Default",
                TrafficGroup = TrafficGroupType.Default,
            },
            new CustomGroupData()
            {
                GroupName = "Parking",
                TrafficGroup = TrafficGroupType.Default | TrafficGroupType.Taxi | TrafficGroupType.Police,
            }
        };

        private static TrafficGroupMaskSettings trafficTypeMaskSettings;
        private static bool loaded;
        private static string[] cachedGroupHeaders;

        public static string[] GroupHeaders
        {
            get
            {
                if (cachedGroupHeaders == null && GroupCount > 0)
                {
                    MakeHeaderCache();
                }

                return cachedGroupHeaders;
            }
        }

        public static int GroupCount => trafficTypeMaskSettings?.CustomGroups?.Count ?? 0;

        public static bool Loaded { get => loaded; set => loaded = value; }

        public static bool HasSettings => trafficTypeMaskSettings != null;

        public static TrafficGroupType GetDefault()
        {
            var trafficTypeMaskSettings = GetSettingsInternal();

            return trafficTypeMaskSettings?.DefaultGroup ?? TrafficGroupType.Default;
        }

        public static TrafficGroupType GetCustomGroup(int groupIndex)
        {
            var groupData = GetGroupData(groupIndex);

            if (groupData != null)
            {
                return groupData.TrafficGroup;
            }

            return GetDefault();
        }

        public static void SetCustomGroup(int groupIndex, TrafficGroupType newGroup)
        {
            var trafficTypeMaskSettings = GetSettingsInternal();
            var groupData = GetGroupData(groupIndex);

            if (groupData != null)
            {
                if (groupData.TrafficGroup != newGroup)
                {
                    groupData.TrafficGroup = newGroup;
                    EditorSaver.SetObjectDirty(trafficTypeMaskSettings);
                }
            }
        }

        public static TrafficGroupMaskSettings Init()
        {
            if (trafficTypeMaskSettings == null)
            {
                var path = $"{CityEditorBookmarks.WINDOW_ROOT_PATH}{DefaultName}";

                trafficTypeMaskSettings = Resources.Load<TrafficGroupMaskSettings>(path);

                if (trafficTypeMaskSettings)
                {
                    return trafficTypeMaskSettings;
                }
            }

#if UNITY_EDITOR
            if (trafficTypeMaskSettings == null)
            {
                try
                {
                    trafficTypeMaskSettings = ScriptableObject.CreateInstance<TrafficGroupMaskSettings>();

                    var path = $"Assets/Resources/{CityEditorBookmarks.WINDOW_ROOT_PATH}";
                    AssetDatabaseExtension.CheckForFolderExist(path);
                    AssetDatabaseExtension.SavePersistScriptableObject(trafficTypeMaskSettings, path, DefaultName);
                    UnityEngine.Debug.Log($"Assets/Resources/TrafficTypeMaskSettings.asset automatically generated.");
                }
                catch { }
                //catch (Exception ex)
                //{
                //    UnityEngine.Debug.LogError($"TrafficTypeMaskSettings. {ex.Message}");
                //}
            }
#endif

            return trafficTypeMaskSettings;
        }

        private static TrafficGroupMaskSettings GetSettingsInternal()
        {
            if (!loaded)
            {
                var settings = Init();

                if (settings != null)
                    loaded = true;
            }

            return trafficTypeMaskSettings;
        }

        private static CustomGroupData GetGroupData(int groupIndex)
        {
            var trafficTypeMaskSettings = GetSettingsInternal();

            if (trafficTypeMaskSettings.CustomGroups.Count > groupIndex)
            {
                return trafficTypeMaskSettings.CustomGroups[groupIndex];
            }

            return null;
        }

        public static void MakeHeaderCache()
        {
            var trafficTypeMaskSettings = GetSettingsInternal();

            if (trafficTypeMaskSettings)
            {
                cachedGroupHeaders = trafficTypeMaskSettings.CustomGroups.Select(a => a.GroupName).ToArray();
            }
            else
            {
                cachedGroupHeaders = new string[1];
            }
        }
    }
}
