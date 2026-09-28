using Spirit604.Collections.Dictionary;
using Spirit604.Gameplay.Inventory;
using Spirit604.Gameplay.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.UI
{
    public class PlayerSelectWeaponView : MonoBehaviour
    {
        [Serializable]
        public class WeaponButtonUIPrefabDictionary : AbstractSerializableDictionary<WeaponType, WeaponButton> { }

        [SerializeField] private WeaponButton defaultWeaponButtonPrefab;
        [SerializeField] private WeaponButtonUIPrefabDictionary weaponButtonPrefabData;
        [SerializeField] private Transform panel;

        private Dictionary<WeaponType, WeaponButton> weaponButtons = new Dictionary<WeaponType, WeaponButton>();

        private WeaponButton hideWeaponButton;
        private WeaponButton lastSelectedButton;

        public Action OnHideWeapon = delegate { };
        public Action<WeaponType> OnSelectWeapon = delegate { };

        public void SwitchActivePanelState(bool isActive)
        {
            if (panel.gameObject.activeSelf != isActive)
            {
                panel.gameObject.SetActive(isActive);
            }
        }

        public void InitPanel(IEnumerable<WeaponItem> weaponData)
        {
            ClearButtons();
            InitHideButton();

            if (weaponData != null)
            {
                foreach (var item in weaponData)
                {
                    AddButton(item.WeaponType);
                }
            }
        }

        public void TryToSelectButton(WeaponType weaponType, bool isSelected)
        {
            if (isSelected)
            {
                if (lastSelectedButton != null)
                {
                    lastSelectedButton.SwitchSelectionState(false);
                }
            }

            var weaponButton = GetWeaponButton(weaponType);
            weaponButton.SwitchSelectionState(isSelected);

            lastSelectedButton = weaponButton;
        }

        private void InitHideButton()
        {
            if (hideWeaponButton == null)
            {
                hideWeaponButton = Instantiate(defaultWeaponButtonPrefab, panel);
                hideWeaponButton.Initialize(() => OnHideWeaponClick(true));
            }
            else
            {
                hideWeaponButton.gameObject.SetActive(true);
                hideWeaponButton.Initialize(() => OnHideWeaponClick(false));
            }
        }

        private void AddButton(WeaponType weaponType)
        {
            if (!weaponButtons.ContainsKey(weaponType))
            {
                WeaponButton weaponButtonPrefab = null;

                weaponButtonPrefabData.TryGetValue(weaponType, out weaponButtonPrefab);

                if (weaponButtonPrefab != null)
                {
                    var weaponButton = Instantiate(weaponButtonPrefab, panel);

                    WeaponType localWeaponType = weaponType;
                    weaponButton.Initialize(() => OnSelectWeaponClick(localWeaponType));
                    weaponButtons.Add(weaponType, weaponButton);
                }
                else
                {
                    UnityEngine.Debug.LogError($"Weapon '{weaponType}' weapon view button not found");
                }
            }
        }

        private void RemoveButton(WeaponType weaponType)
        {
            if (weaponButtons.ContainsKey(weaponType))
            {
                var weaponButton = weaponButtons[weaponType];
                Destroy(weaponButton.gameObject);
                weaponButtons.Remove(weaponType);
            }
        }

        private void ClearButtons()
        {
            while (weaponButtons.Keys.Count > 0)
            {
                var weaponType = weaponButtons.ElementAt(0).Key;

                RemoveButton(weaponType);
            }

            if (hideWeaponButton)
                hideWeaponButton.gameObject.SetActive(false);
        }

        private WeaponButton GetWeaponButton(WeaponType weaponType)
        {
            if (weaponType == WeaponType.Default)
            {
                return hideWeaponButton;
            }

            return weaponButtons[weaponType];
        }

        private void OnHideWeaponClick(bool resetWeapon = false)
        {
            OnHideWeapon();

            if (resetWeapon)
            {
                OnSelectWeapon(WeaponType.Default);
            }
        }

        private void OnSelectWeaponClick(WeaponType weaponType)
        {
            OnSelectWeapon(weaponType);
        }
    }
}
