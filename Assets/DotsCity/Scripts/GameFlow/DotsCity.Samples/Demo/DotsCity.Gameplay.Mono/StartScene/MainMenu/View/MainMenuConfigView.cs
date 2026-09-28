using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.Extensions;
using Spirit604.MainMenu.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using UnityEngine;

namespace Spirit604.MainMenu.UI
{
    public class MainMenuConfigView : MonoBehaviour
    {
        [SerializeField] private Transform panel;
        [SerializeField] private ConfigSliderValueUIItem configSliderValueUIItemPrefab;
        [SerializeField] private ConfigBoolValueUIItem configBoolValueUIItem;
        [SerializeField] private ConfigEnumValueUIItem configEnumValueUIItemPrefab;

        private List<ConfigItemBase> items = new List<ConfigItemBase>();
        private PlayerMenuSettings playerSelectionData;

        public Dictionary<string, string> InitialValues { get; } = new Dictionary<string, string>();
        public bool HasCustomParam { get; private set; }

        public event Action<ParamData> OnConfigChanged = delegate { };

        private void Update()
        {
            for (int i = 0; i < items?.Count; i++)
            {
                var item = items[i];

                if (!item)
                {
                    continue;
                }

                item.CheckShowState();
            }
        }

        public void Init(PlayerMenuSettings playerSelectionData)
        {
            this.playerSelectionData = playerSelectionData;
        }

        public void DrawConfig(object configObj, bool initial = false)
        {
            Clear();

            if (configObj == null)
            {
                return;
            }

            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var fields = configObj.GetType().GetFields(flags);

            foreach (var field in fields)
            {
                InitializeField(configObj, field, initial);
            }
        }

        private void InitializeField(object configObj, FieldInfo field, bool initial = false)
        {
            var hideInViewAttr = AttributeExtension.GetAttribute<HideInViewAttribute>(field);

            if (hideInViewAttr != null)
            {
                return;
            }

            bool canShow = true;
            bool orCondition = false;
            List<Func<bool>> showCallbacks = null;

            var showAttr = AttributeExtension.GetAttribute<ShowIfAttribute>(field);

            if (showAttr != null)
            {
                showCallbacks = AddConditions(configObj, new string[] { showAttr.Condition }, ref canShow);
            }
            else
            {
                var hideAttr = AttributeExtension.GetAttribute<HideIfAttribute>(field);

                if (hideAttr != null)
                {
                    showCallbacks = AddConditions(configObj, new string[] { hideAttr.Condition }, ref canShow, true);
                }
            }

            var rangeAttr = AttributeExtension.GetAttribute<RangeAttribute>(field);

            float maxValue = 1000f;

            if (rangeAttr != null)
            {
                maxValue = rangeAttr.max;
            }

            ConfigItemBase item = null;

            if (field.FieldType == typeof(Single))
            {
                item = Instantiate(configSliderValueUIItemPrefab, panel);
                var sliderItem = item as ConfigSliderValueUIItem;

                TryToSetValue(configObj, field, typeof(Single), initial);
                var boxedValue = field.GetValue(configObj);
                float value = Convert.ToSingle(boxedValue);

                SetInitialValue(configObj, field, value.ToString());

                Action<float> callback = (value) =>
                {
                    field.SetValue(configObj, value);
                    InvokeConfigChanged(configObj, field, value, typeof(Single));
                };

                sliderItem.Initialize(field.Name.CamelToLabel(), value, callback, false, maxValue);
            }
            else if (field.FieldType == typeof(Int32))
            {
                item = Instantiate(configSliderValueUIItemPrefab, panel);
                var sliderItem = item as ConfigSliderValueUIItem;

                TryToSetValue(configObj, field, typeof(int), initial);
                var boxedValue = field.GetValue(configObj);
                float value = Convert.ToSingle(boxedValue);

                SetInitialValue(configObj, field, value.ToString());

                Action<float> callback = (value) =>
                {
                    field.SetValue(configObj, (int)value);
                    InvokeConfigChanged(configObj, field, value, typeof(int));
                };

                sliderItem.Initialize(field.Name.CamelToLabel(), value, callback, true, maxValue);
            }
            else if (field.FieldType == typeof(bool))
            {
                item = Instantiate(configBoolValueUIItem, panel);
                var toggleItem = item as ConfigBoolValueUIItem;

                TryToSetValue(configObj, field, typeof(bool), initial);
                var boxedValue = field.GetValue(configObj);
                var value = Convert.ToBoolean(boxedValue);

                SetInitialValue(configObj, field, value.ToString());

                Action<bool> callback = (value) =>
                {
                    field.SetValue(configObj, value);
                    InvokeConfigChanged(configObj, field, value, typeof(bool));
                };

                toggleItem.Initialize(field.Name.CamelToLabel(), value, callback);
            }
            else if (field.FieldType.IsEnum)
            {
                item = Instantiate(configEnumValueUIItemPrefab, panel);
                var enumItem = item as ConfigEnumValueUIItem;

                var enumType = field.FieldType;
                TryToSetValue(configObj, field, enumType, initial);
                var value = field.GetValue(configObj);

                SetInitialValue(configObj, field, value.ToString());

                Action<object> callback = (value) =>
                {
                    field.SetValue(configObj, value);
                    InvokeConfigChanged(configObj, field, value, enumType);
                };

                enumItem.Initialize(field.Name.CamelToLabel(), value, enumType, callback);
            }

            if (item)
            {
                item.Initialize(showCallbacks, orCondition, canShow);
                items.Add(item);
            }
        }

        private void SetInitialValue(object config, FieldInfo field, string value)
        {
            var data = playerSelectionData.TryToGetParam(config.ToString(), field.Name);

            if (data != null)
            {
                InitialValues.Add(field.Name, data.InitialValue);
                return;
            }

            InitialValues.Add(field.Name, value);
        }

        private List<Func<bool>> AddConditions(object configObj, string[] conditions, ref bool canShow, bool hidden = false)
        {
            List<Func<bool>> callbackList = null;

            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var type = configObj.GetType();

            for (int i = 0; i < conditions?.Length; i++)
            {
                var condition = conditions[i];

                var conditionField = type.GetField(condition, flags);

                if (conditionField != null)
                {
                    var isValid = (bool)conditionField.GetValue(configObj);

                    callbackList = AddCallback(configObj, callbackList, conditionField, hidden);

                    if (!isValid)
                    {
                        canShow = false;
                    }
                }
                else
                {
                    var propertyField = type.GetProperty(condition, flags);

                    if (propertyField != null)
                    {
                        var isValid = !(bool)propertyField.GetValue(configObj);

                        callbackList = AddCallback(configObj, callbackList, propertyField, hidden);

                        if (!isValid)
                        {
                            canShow = false;
                        }
                    }
                }
            }

            return callbackList;
        }

        private List<Func<bool>> AddCallback(object configObj, List<Func<bool>> callbackList, FieldInfo conditionField, bool hideCondition = false)
        {
            if (callbackList == null)
            {
                callbackList = new List<Func<bool>>();
            }

            Func<bool> showCallback = default;

            if (!hideCondition)
            {
                showCallback = () => (bool)conditionField.GetValue(configObj);
            }
            else
            {
                showCallback = () => !(bool)conditionField.GetValue(configObj);
            }

            callbackList.Add(showCallback);

            return callbackList;
        }

        private List<Func<bool>> AddCallback(object configObj, List<Func<bool>> callbackList, PropertyInfo conditionField, bool hideCondition = false)
        {
            if (callbackList == null)
            {
                callbackList = new List<Func<bool>>();
            }

            Func<bool> showCallback = default;

            if (!hideCondition)
            {
                showCallback = () => (bool)conditionField.GetValue(configObj);
            }
            else
            {
                showCallback = () => !(bool)conditionField.GetValue(configObj);
            }

            callbackList.Add(showCallback);

            return callbackList;
        }

        private void InvokeConfigChanged(object config, FieldInfo fieldInfo, object value, Type type)
        {
            var paramData = new ParamData()
            {
                ConfigName = config.ToString(),
                ParamName = fieldInfo.Name,
                InitialValue = InitialValues[fieldInfo.Name],
                Value = value.ToString(),
            };

            HasCustomParam = true;
            OnConfigChanged(paramData);
        }

        private void TryToSetValue(object config, FieldInfo fieldInfo, Type type, bool initial = false)
        {
            var data = playerSelectionData.TryToGetParam(config.ToString(), fieldInfo.Name);

            if (data != null)
            {
                if (!initial)
                {
                    HasCustomParam = true;
                    TryToSetValue(config, fieldInfo, type, data.Value);
                }
                else
                {
                    TryToSetValue(config, fieldInfo, type, data.InitialValue);
                }
            }
        }

        private void TryToSetValue(object config, FieldInfo fieldInfo, Type type, string value)
        {
            try
            {
                var converter = TypeDescriptor.GetConverter(type);
                var result = converter.ConvertFrom(value);
                fieldInfo.SetValue(config, result);
            }
            catch { }
        }

        private void Clear()
        {
            HasCustomParam = false;
            TransformExtensions.ClearChilds(panel);
            items.Clear();
            InitialValues.Clear();
        }
    }
}
