using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using System.Reflection;

namespace Spirit604.CityEditor
{
    public class SceneObjectDataFilter<T> where T : MonoBehaviour
    {
        #region Variables

        private SceneDataViewerConfig sceneDataViewerConfig;
        private Dictionary<T, List<FilteredVariableData>> filteredData = new Dictionary<T, List<FilteredVariableData>>();
        private Dictionary<string, object> prefabSourceParamValuesDict = new Dictionary<string, object>();
        private string[] availableParamNames;

        #endregion

        #region Properties

        public string[] AvailableParamNames => availableParamNames;

        public Dictionary<T, List<FilteredVariableData>> FilteredData => filteredData;

        #endregion

        #region Constructor

        public SceneObjectDataFilter() { }

        public SceneObjectDataFilter(T prefab, SceneDataViewerConfig sceneDataViewerConfig)
        {
            SetupConfig(prefab, sceneDataViewerConfig);
        }

        #endregion

        #region Methods

        public void SetupConfig(T prefab, SceneDataViewerConfig sceneDataViewerConfig)
        {
            this.sceneDataViewerConfig = sceneDataViewerConfig;
            var targetPrefab = prefab;

            prefabSourceParamValuesDict.Clear();

            if (sceneDataViewerConfig == null || prefab == null)
            {
                return;
            }

            List<string> availableParamsTemp = new List<string>();

            foreach (var paramData in sceneDataViewerConfig.VariableDataDict)
            {
                var prefabFieldValue = GetFieldValue(targetPrefab, paramData.Value.SerializedParamName);

                if (prefabFieldValue != null)
                {
                    prefabSourceParamValuesDict.Add(paramData.Value.SerializedParamName, prefabFieldValue);
                    availableParamsTemp.Add(paramData.Value.SerializedParamName);
                }
                else
                {
                    Debug.Log($"Prefab Field '{paramData.Value.SerializedParamName}' not found!");
                }
            }

            availableParamNames = availableParamsTemp.ToArray();
        }

        public void ClearFilterData()
        {
            filteredData.Clear();
        }

        public void CreateDefaultFilter(ICollection<T> targetObjects)
        {
            ClearFilterData();

            if (!sceneDataViewerConfig)
            {
                return;
            }

            foreach (var targetObject in targetObjects)
            {
                TryToFilterNode(targetObject);
            }
        }

        public void UpdateFilterNode(T targetObject)
        {
            if (filteredData.ContainsKey(targetObject))
            {
                filteredData[targetObject].Clear();
            }

            TryToFilterNode(targetObject);
        }

        public void TryToFilterNode(T targetObject)
        {
            foreach (var variableData in sceneDataViewerConfig.VariableDataDict)
            {
                TryToFilterNode(targetObject, variableData.Value.SerializedParamName);
            }
        }

        public void TryToFilterNode(T targetObject, string filterName)
        {
            if (!sceneDataViewerConfig)
            {
                return;
            }

            var variableData = sceneDataViewerConfig.VariableDataDict;

            if (!variableData.TryGetValue(filterName, out var paramData))
            {
                return;
            }

            object prefabFieldObject;

            if (!prefabSourceParamValuesDict.TryGetValue(paramData.SerializedParamName, out prefabFieldObject))
            {
                return;
            }

            object sceneFieldObject = GetFieldValue(targetObject, paramData.SerializedParamName, out var targetObjectField);

            var paramType = paramData.Type;
            var matched = CompareObjectValue(prefabFieldObject, sceneFieldObject, paramType);

            if (!matched)
            {
                if (!filteredData.ContainsKey(targetObject))
                {
                    filteredData.Add(targetObject, new List<FilteredVariableData>());
                }

                float floatValue = 0;
                float minValue = 0;
                float maxValue = 0;
                bool boolValue = false;
                Enum enumValue = default;

                bool hasRange = false;

                RangeAttribute rangeAttribute = (RangeAttribute)targetObjectField.GetCustomAttribute(typeof(RangeAttribute));

                if (rangeAttribute != null)
                {
                    hasRange = true;
                    minValue = rangeAttribute.min;
                    maxValue = rangeAttribute.max;
                }

                if (paramType == typeof(int) || paramType == typeof(float))
                {
                    floatValue = (float)Convert.ToDouble(sceneFieldObject);
                }
                if (paramType == typeof(bool))
                {
                    boolValue = Convert.ToBoolean(sceneFieldObject);
                }
                if (paramType == typeof(Enum))
                {
                    enumValue = (Enum)Convert.ChangeType(sceneFieldObject, typeof(Enum));
                }

                filteredData[targetObject].Add(new FilteredVariableData()
                {
                    SceneViewName = paramData.ViewParamName,
                    SceneViewShortName = paramData.ViewParamShortName,
                    Type = paramType,
                    FloatValue = floatValue,
                    BoolValue = boolValue,
                    EnumValue = enumValue,
                    HasRange = hasRange,
                    MinValue = minValue,
                    MaxValue = maxValue
                });
            }
        }

        private object GetFieldValue(T targetObject, string fieldName)
        {
            return GetFieldValue(targetObject, fieldName, out var field);
        }

        private object GetFieldValue(T targetObject, string fieldName, out FieldInfo fieldInfo)
        {
            fieldInfo = targetObject.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (fieldInfo != null)
            {
                return fieldInfo.GetValue(targetObject);
            }

            return null;
        }

        private bool CompareObjectValue(object val1, object val2, Type type)
        {
            if (type == typeof(float))
            {
                return Convert.ToDouble(val1) == Convert.ToDouble(val2);
            }
            if (type == typeof(int))
            {
                return Convert.ToInt32(val1) == Convert.ToInt32(val2);
            }
            if (type == typeof(bool))
            {
                return Convert.ToBoolean(val1) == Convert.ToBoolean(val2);
            }
            if (type == typeof(Enum))
            {
                return Convert.ChangeType(val1, typeof(Enum)).Equals(Convert.ChangeType(val2, typeof(Enum)));
            }

            return false;
        }

        #endregion
    }
}
#endif