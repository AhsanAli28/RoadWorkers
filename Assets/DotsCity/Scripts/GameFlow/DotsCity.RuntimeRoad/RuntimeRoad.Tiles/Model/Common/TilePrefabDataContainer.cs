using Spirit604.Attributes;
using Spirit604.Collections.Dictionary;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Spirit604.DotsCity.RuntimeRoad
{
    [CreateAssetMenu(menuName = "Spirit604/RuntimeDemo/Tile Prefab Data Container")]
    public class TilePrefabDataContainer : ScriptableObjectBase
    {
        [Serializable]
        public class PrefabDictionary : AbstractSerializableDictionary<PrefabType, PrefabData> { }

        [FormerlySerializedAs("recalcPrefabs")]
        [SerializeField] private PrefabDictionary prefabContainer = new PrefabDictionary();

        private Dictionary<PrefabType, int> selectedVariantData = new Dictionary<PrefabType, int>();

        public PrefabDictionary PrefabContainer => prefabContainer;

        public IList<PrefabData> GetPrefabs()
        {
            var prefabs = new List<PrefabData>();

            foreach (var item in prefabContainer)
            {
                prefabs.Add(item.Value);
            }

            return prefabs.AsReadOnly();
        }

        public RuntimeRoadTile GetPrefab(PrefabType type, int variant = -1)
        {
            if (prefabContainer.TryGetValue(type, out var prefabData))
            {
                if (variant == -1)
                {
                    variant = GetPrefabVariant(type);
                }

                if (variant >= 0 && prefabData.Variants.Count > variant)
                {
                    return prefabData.Variants[variant];
                }
            }

            return null;
        }

        public int GetPrefabVariant(PrefabType prefabType)
        {
            if (selectedVariantData.TryGetValue(prefabType, out var variant)) return variant;

            return 0;
        }

        public void SetPrefabVariant(PrefabType prefabType, int variant)
        {
            if (!selectedVariantData.ContainsKey(prefabType))
            {
                selectedVariantData.Add(prefabType, variant);
            }
            else
            {
                selectedVariantData[prefabType] = variant;
            }
        }

        [Button]
        public void BakeAllPrefabs()
        {
#if UNITY_EDITOR
            foreach (var container in prefabContainer)
            {
                foreach (var item in container.Value.Variants)
                {
                    if (item == null)
                    {
                        Debug.LogError($"{name} has null entry");
                        continue;
                    }

                    item.runtimeSegment.BakePrefab();
                }
            }
#endif
        }
    }
}
