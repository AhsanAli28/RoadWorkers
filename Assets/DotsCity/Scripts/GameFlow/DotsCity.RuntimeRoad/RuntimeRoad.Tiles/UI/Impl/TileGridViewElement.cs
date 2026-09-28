using System;
using UnityEngine;
using UnityEngine.UI;

namespace Spirit604.DotsCity.RuntimeRoad
{
    [DisallowMultipleComponent]
    public class TileGridViewElement : TileGridViewBase<TileGridViewElement>
    {
        [SerializeField] private Transform variantPopupPanel;
        [SerializeField] private Button openPopupButton;

        public Transform VariantPopupPanel => variantPopupPanel;

        public event Action<TileGridViewElement> OnPopupButtonClicked = delegate { };

        protected override void Awake()
        {
            base.Awake();

            if (openPopupButton)
            {
                openPopupButton.onClick.RemoveAllListeners();
                openPopupButton.onClick.AddListener(() => OnPopupButtonClicked(this));
            }
        }

        public void SwitchPopupButton(bool isActive)
        {
            openPopupButton.gameObject.SetActive(isActive);
        }

        public void SwitchPopup(bool isActive)
        {
            VariantPopupPanel.gameObject.SetActive(isActive);
        }
    }
}
