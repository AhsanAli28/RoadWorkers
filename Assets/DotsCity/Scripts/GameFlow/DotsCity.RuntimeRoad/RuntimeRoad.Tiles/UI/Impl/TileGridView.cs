using Spirit604.Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    [DisallowMultipleComponent]
    public class TileGridView : MonoBehaviour
    {
        [SerializeField] private Transform gridParent;
        [SerializeField] private TileGridViewElement elementPrefab;
        [SerializeField] private PreviewTileGridViewElement variantPrefab;
        [SerializeField] private TileGridViewIconData tileGridViewIconData;

        private Dictionary<PrefabType, TileGridViewElement> tileViewBinding = new Dictionary<PrefabType, TileGridViewElement>();
        private Dictionary<int, PreviewTileGridViewElement> variantViewBinding = new Dictionary<int, PreviewTileGridViewElement>();
        private TileGridViewElement previousSelectedPopup;

        public event Action<int> OnTileSelected = delegate { };
        public event Action<PrefabType, int> OnVariantSelected = delegate { };
        public event Action OnVariantPopupOpened = delegate { };

        public void Populate(IList<PrefabData> tiles)
        {
            Clear();

            if (tiles == null) return;

            for (int i = 0; i < tiles.Count; i++)
            {
                var prefabTileData = tiles[i];
                var tile = prefabTileData.Variants[0];
                var tileView = Instantiate(elementPrefab, gridParent);

                tileView.OnClicked += TileView_OnClicked;
                tileView.SwitchPopupButton(prefabTileData.HasVariants);

                tileViewBinding.Add(tile.PrefabType, tileView);

                var variantPanel = tileView.VariantPopupPanel;
                variantPanel.gameObject.SetActive(false);

                if (!prefabTileData.HasVariants)
                {
                    UpdateTileView(tileView, tile);
                    continue;
                }

                tileView.OnPopupButtonClicked += TileView_OnPopupButtonClicked;

                for (int j = prefabTileData.Variants.Count - 1; j >= 0; j--)
                {
                    var variantIndex = j;

                    var item = prefabTileData.Variants[j];

                    var variantView = Instantiate(variantPrefab, variantPanel);

                    variantView.OnClicked += (view) => OnVariantSelected(item.PrefabType, variantIndex);

                    var variantIcon = tileGridViewIconData.GetIcon(item.gameObject.name);
                    variantView.Initialize(item.ID, variantIcon);

                    if (item.Selected)
                    {
                        var selectedVariant = item;
                        UpdateTileView(tileView, selectedVariant);
                    }

                    var key = GetKey(tile.PrefabType, variantIndex);

                    variantViewBinding.Add(key, variantView);
                }
            }
        }

        private void TileView_OnPopupButtonClicked(TileGridViewElement newPopup)
        {
            DisablePopup();
            previousSelectedPopup = newPopup;
            newPopup.SwitchPopupButton(false);
            newPopup.SwitchPopup(true);
            OnVariantPopupOpened();
        }

        private void DisablePopup()
        {
            if (previousSelectedPopup == null) return;

            previousSelectedPopup.SwitchPopupButton(true);
            previousSelectedPopup.SwitchPopup(false);
            previousSelectedPopup = null;
        }

        private void UpdateTileView(TileGridViewElement tileView, RuntimeRoadTile selectedVariant)
        {
            var icon = tileGridViewIconData.GetIcon(selectedVariant.gameObject.name);
            tileView.Initialize(selectedVariant.ID, icon);
        }

        public void SetVariant(PrefabType prefabType, int variant)
        {
            var element = tileViewBinding[prefabType];
            var key = GetKey(prefabType, variant);
            var variantElement = variantViewBinding[key];

            element.Initialize(variantElement.ID, variantElement.Icon);

            element.SwitchPopupButton(true);
            element.SwitchPopup(false);
        }

        private void Clear()
        {
            //var tiles = new List<Transform>();

            //for (int i = 0; i < gridParent.childCount; i++)
            //{
            //    tiles.Add(gridParent.GetChild(i));
            //}

            TransformExtensions.ClearChilds(gridParent);
            //while (tiles.Count > 0)
            //{
            //    var tile = tiles[0];
            //    Destroy(tile.gameObject);
            //    tiles.RemoveAt(0);
            //}

            variantViewBinding.Clear();
            tileViewBinding.Clear();
        }

        private int GetKey(PrefabType type, int variant)
        {
            int key = (int)type * 100;

            key += variant;

            return key;
        }

        private void TileView_OnClicked(TileGridViewElement tileView)
        {
            DisablePopup();
            OnTileSelected(tileView.ID);
        }
    }
}
