using Spirit604.CityEditor;
using Spirit604.Collections.Dictionary;
using Spirit604.Extensions;
using System;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [CreateAssetMenu(fileName = "PedestrianSkinFactoryData", menuName = CityEditorBookmarks.CITY_EDITOR_LEVEL_FACTORY_PRESETS_PATH + "PedestrianSkinFactoryPresetData")]
    public class PedestrianSkinFactoryData : ScriptableObject
    {
        [Serializable]
        public class PedestrianData
        {
            public GameObject Skin;
            public PedestrianRagdoll Ragdoll;
        }

        [Serializable]
        public class PedestrianRagdollPrefabDataDictionary : AbstractSerializableDictionary<string, PedestrianData> { }

        [SerializeField] private PedestrianRagdollPrefabDataDictionary ragdollPrefabDataDictionary;

        public PedestrianRagdollPrefabDataDictionary RagdollPrefabDictionary => ragdollPrefabDataDictionary;

        public bool TryToAddEntry(GameObject skin, string key)
        {
            if (!HasEntry(key))
            {
                ragdollPrefabDataDictionary.Add(key, new PedestrianData()
                {
                    Skin = skin
                });

                EditorSaver.SetObjectDirty(this);
                return true;
            }

            return false;
        }

        public PedestrianData GetEntry(int skinIndex)
        {
            int index = 0;
            foreach (var item in ragdollPrefabDataDictionary)
            {
                if (index == skinIndex)
                {
                    return item.Value;
                }

                index++;
            }

            return null;
        }

        public bool TryToRemoveEntry(string key)
        {
            if (HasEntry(key))
            {
                ragdollPrefabDataDictionary.Remove(key);
                EditorSaver.SetObjectDirty(this);
                return true;
            }

            return false;
        }

        public bool HasEntry(string key)
        {
            return ragdollPrefabDataDictionary.ContainsKey(key);
        }

        public bool HasDuplicateSkin(GameObject sourcePrefab, out string key)
        {
            key = string.Empty;

            foreach (var item in ragdollPrefabDataDictionary)
            {
                if (item.Value.Skin == sourcePrefab)
                {
                    key = item.Key;
                    return true;
                }
            }

            return false;
        }
    }
}
