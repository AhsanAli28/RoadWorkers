using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    public abstract class TileGameUIBase : MonoBehaviour
    {
        public event Action<int> OnPageSelected = delegate { };
        public event Action<GameModeType> OnGameModeSelected = delegate { };
        public event Action<int> OnTileSelected = delegate { };
        public event Action<PrefabType, int> OnVariantSelected = delegate { };
        public event Action<PlacingType> OnModeClicked = delegate { };
        public event Action OnVariantPopupOpened = delegate { };

        public abstract void InitPages(int pageCount);

        public abstract void Populate(IList<PrefabData> tilePrefabs);

        public abstract void SetVariant(PrefabType prefabType, int variant);

        public virtual void SetMode(GameModeType mode) { }

        public virtual void Unselect() { }

        protected virtual void OnTileSelectedInternal(int id)
        {
            OnTileSelected(id);
        }

        protected virtual void OnModeSelectedInternal(PlacingType placingType)
        {
            OnModeClicked(placingType);
        }

        protected void OnPageSelectedInternal(int page)
        {
            OnPageSelected(page);
        }

        protected void OnGameModeSelectedInternal(GameModeType mode)
        {
            OnGameModeSelected(mode);
        }

        protected void OnVariantSelectedInternal(PrefabType prefabType, int variant)
        {
            OnVariantSelected(prefabType, variant);
        }

        protected void OnPopupOpenedInternal()
        {
            OnVariantPopupOpened();
        }
    }
}
