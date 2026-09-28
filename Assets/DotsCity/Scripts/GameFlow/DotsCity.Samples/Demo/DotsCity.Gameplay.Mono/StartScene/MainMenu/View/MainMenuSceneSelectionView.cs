using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Spirit604.MainMenu.UI
{
    public class MainMenuSceneSelectionView : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown dropdown;

        public event Action<int> OnSelectedSceneChanged = delegate { };

        public void Initialize(List<string> options, int value)
        {
            dropdown.ClearOptions();
            dropdown.AddOptions(options);
            dropdown.value = value;

            dropdown.onValueChanged.AddListener(delegate
            {
                DropdownValueChanged(dropdown);
            });
        }

        private void DropdownValueChanged(TMP_Dropdown change)
        {
            var index = change.value;
            OnSelectedSceneChanged(index);
        }
    }
}