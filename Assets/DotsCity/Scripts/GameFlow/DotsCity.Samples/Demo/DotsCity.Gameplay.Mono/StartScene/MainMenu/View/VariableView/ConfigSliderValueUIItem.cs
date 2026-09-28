using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Spirit604.MainMenu.UI
{
    public class ConfigSliderValueUIItem : ConfigItemBase
    {
        [SerializeField] private TextMeshProUGUI valueNameText;
        [SerializeField] private Slider slider;
        [SerializeField] private TMP_InputField inputField;

        public void Initialize(string valueName, float value, Action<float> onChangeValueCallback, bool wholeNumbers, float maxValue = 1000)
        {
            valueNameText.SetText(valueName);
            inputField.text = value.ToString();

            slider.wholeNumbers = wholeNumbers;
            Initialize(value, maxValue);
            slider.onValueChanged.AddListener((val) => onChangeValueCallback(val));
            slider.onValueChanged.AddListener((val) => Slider_onValueChanged(val));

            inputField.onValueChanged.AddListener
            (
                (val) =>
                {
                    if (float.TryParse(val, out var result))
                    {
                        onChangeValueCallback(result);
                        slider.value = result;
                    }
                }
            );
        }

        public void Initialize(float value, float maxValue = 1000)
        {
            slider.maxValue = maxValue;
            slider.value = value;
        }

        private void Slider_onValueChanged(float value)
        {
            var valueStr = value.ToString();
            inputField.text = valueStr;
        }
    }
}
