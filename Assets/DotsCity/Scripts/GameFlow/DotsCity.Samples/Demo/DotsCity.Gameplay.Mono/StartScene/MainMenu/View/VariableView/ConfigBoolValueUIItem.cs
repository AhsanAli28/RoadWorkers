using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Spirit604.MainMenu.UI
{
    public class ConfigBoolValueUIItem : ConfigItemBase
    {
        [SerializeField] private TextMeshProUGUI valueNameText;
        [SerializeField] private Toggle toggle;

        public void Initialize(string valueName, bool value, Action<bool> onChangeValueCallback)
        {
            valueNameText.SetText(valueName);

            Initialize(value);
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener((val) => onChangeValueCallback(val));
        }

        public void Initialize(bool value)
        {
            toggle.isOn = value;
        }
    }
}

