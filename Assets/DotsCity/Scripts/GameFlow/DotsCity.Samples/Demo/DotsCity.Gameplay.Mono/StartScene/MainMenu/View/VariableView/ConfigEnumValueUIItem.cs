using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Spirit604.MainMenu.UI
{
    public class ConfigEnumValueUIItem : ConfigItemBase
    {
        [SerializeField] private TextMeshProUGUI valueNameText;
        [SerializeField] private TMP_Dropdown dropDown;

        private Type enumType;
        private List<string> values;

        public void Initialize(string valueName, object value, Type enumType, Action<object> onChangeValueCallback)
        {
            valueNameText.SetText(valueName);
            this.enumType = enumType;

            if (values == null)
            {
                dropDown.ClearOptions();
                values = Enum.GetNames(enumType).ToList();
                dropDown.AddOptions(values);
            }

            if (values?.Count > 0)
            {
                SetValue(value);

                dropDown.onValueChanged.AddListener((index) =>
                {
                    if (dropDown.options.Count > index)
                    {
                        var option = dropDown.options[index].text;

                        try
                        {
                            var result = Enum.Parse(enumType, option);
                            onChangeValueCallback(result);
                        }
                        catch { }
                    }
                });
            }
        }

        public void SetValue(object value)
        {
            if (value == null)
            {
                return;
            }

            int index = values.IndexOf(value.ToString());
            dropDown.value = index;
        }
    }
}