using System;
using UnityEngine;
using UnityEngine.UI;

namespace Spirit604.DotsCity.Gameplay.UI
{
    public class WeaponButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image overlay;

        public void Initialize(Action onClick)
        {
            button.onClick.AddListener(() => onClick());
        }

        public void SwitchSelectionState(bool isSelected) { overlay.enabled = isSelected; }
    }
}