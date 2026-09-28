using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    /// <summary>
    /// Factory containing all the Tile presets.
    /// </summary>
    public abstract class TileFactoryBase : MonoBehaviour
    {
        [SerializeField] private List<TilePrefabDataContainer> tilePrefabDataContainers = new List<TilePrefabDataContainer>();

        public int PageCount => tilePrefabDataContainers.Count;

        public int SelectedPage { get; set; }

        public TilePrefabDataContainer.PrefabDictionary PrefabContainer => TilePrefabDataContainer.PrefabContainer;

        private TilePrefabDataContainer TilePrefabDataContainer => tilePrefabDataContainers[SelectedPage];

        public abstract RuntimeRoadTile InstantiateTile(RuntimeRoadTile prefabTile, Transform parent);

        public abstract void DestroyTile(GameObject tile);

        public void Init()
        {
            IterateFactory((variant, pageIndex, variantIndex) =>
            {
                variant.Page = pageIndex;
                variant.ID = variant.gameObject.GetInstanceID();
            });
        }

        public virtual IList<PrefabData> GetPrefabs()
        {
            return TilePrefabDataContainer.GetPrefabs();
        }

        public virtual RuntimeRoadTile GetPrefab(PrefabType type, int variant = -1)
        {
            return TilePrefabDataContainer.GetPrefab(type, variant);
        }

        public virtual int GetPrefabVariant(PrefabType prefabType)
        {
            return TilePrefabDataContainer.GetPrefabVariant(prefabType);
        }

        public virtual void SetPrefabVariant(PrefabType prefabType, int variant)
        {
            TilePrefabDataContainer.SetPrefabVariant(prefabType, variant);
        }

        public void Validate()
        {
            var tiles = new HashSet<RuntimeRoadTile>();

            IterateFactory((variant, pageIndex, variantIndex) =>
            {
                variant.Validate();

                if (!tiles.Contains(variant))
                {
                    tiles.Add(variant);
                }
                else
                {
                    Debug.Log($"TileFactory. RuntimeRoadTile prefab '{variant.name}' duplicate found");
                }
            });
        }

        protected void IterateFactory(Action<RuntimeRoadTile, int, int> action)
        {
            for (int pageIndex = 0; pageIndex < tilePrefabDataContainers.Count; pageIndex++)
            {
                var container = tilePrefabDataContainers[pageIndex];

                foreach (var item in container.PrefabContainer)
                {
                    for (int i = 0; i < item.Value.Variants.Count; i++)
                    {
                        var variant = item.Value.Variants[i];
                        action(variant, pageIndex, i);
                    }
                }
            }
        }
    }
}
