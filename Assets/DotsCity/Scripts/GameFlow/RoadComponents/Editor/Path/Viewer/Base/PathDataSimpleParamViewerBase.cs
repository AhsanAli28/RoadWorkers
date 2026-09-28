#if UNITY_EDITOR
using Spirit604.Collections.Dictionary;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public abstract class PathDataSimpleParamViewerBase<T> : PathDataViewerBase
    {
        #region Helper types

        [System.Serializable]
        private class ColorDictionary : AbstractSerializableDictionary<T, ColorPathData> { }

        #endregion

        #region Variables

        private ColorDictionary colorDictionary = new ColorDictionary();
        private bool colorsFoldout = true;
        private bool shouldRemoveKey;
        private T removeKey;

        #endregion

        #region Properties

        protected virtual string PathTypeSaveKey { get; set; } = $"{nameof(T)}_ColorDictionary";
        protected virtual bool Editable => true;

        #endregion

        #region Overriden methods

        public override void SwitchEnabledState(bool isEnabled)
        {
            SwitchCustomColoringState(isEnabled);
        }

        public override void UpdateData(Path[] paths)
        {
            SwitchCustomColoringState(paths, true);
        }

        public override void DrawCustomSettings()
        {
            Action colorsContent = () =>
            {
                foreach (var nodeData in colorDictionary)
                {
                    EditorGUILayout.BeginHorizontal();

                    DrawLabel(nodeData);

                    EditorGUI.BeginChangeCheck();

                    nodeData.Value.Color = EditorGUILayout.ColorField(nodeData.Value.Color);

                    if (EditorGUI.EndChangeCheck())
                    {
                        SwitchCustomColoringState(true);
                        SceneView.RepaintAll();
                    }

                    EditorGUI.BeginChangeCheck();

                    if (nodeData.Value.Enabled)
                    {
                        DrawHideButton(() => nodeData.Value.Enabled = false);
                    }
                    else
                    {
                        DrawShowButton(() => nodeData.Value.Enabled = true);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        SwitchCustomColoringState(true);
                        SceneView.RepaintAll();
                    }

                    GUI.enabled = Editable;

                    DrawRemoveButton(() =>
                    {
                        shouldRemoveKey = true;
                        removeKey = nodeData.Key;
                    });

                    GUI.enabled = true;

                    EditorGUILayout.EndHorizontal();
                }
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Colors Info", colorsContent, ref colorsFoldout);

            if (shouldRemoveKey)
            {
                shouldRemoveKey = false;
                SwitchCustomColoringState(true);
                SceneView.RepaintAll();
            }
        }

        public override void SaveData()
        {
            var data = JsonUtility.ToJson(colorDictionary, false);
            EditorPrefs.SetString(PathTypeSaveKey, data);
        }

        public override void LoadData()
        {
            var data = EditorPrefs.GetString(PathTypeSaveKey);

            if (!string.IsNullOrEmpty(data))
            {
                try
                {
                    var savedData = JsonUtility.FromJson<ColorDictionary>(data);

                    if (savedData != null)
                    {
                        colorDictionary = savedData;
                    }
                    else
                    {
                        colorDictionary.Clear();
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"Json convert Error. Json data {data}. Message {e.Message}");
                }
            }
        }

        #endregion

        #region Protected virtual methods

        protected virtual void DrawLabel(KeyValuePair<T, ColorPathData> nodeData)
        {
            string rowText = GetLabelRowText(nodeData.Key);

            EditorGUILayout.LabelField(rowText);
        }

        protected virtual string GetLabelRowText(T keyValue)
        {
            var keyString = typeof(T).ToString();
            var lastDotIndex = keyString.LastIndexOf(".");

            if (lastDotIndex >= 0)
            {
                keyString = keyString.Substring(lastDotIndex + 1, keyString.Length - lastDotIndex - 1);
            }

            return $"{keyString}: {keyValue} ";
        }

        protected virtual Color GetColor(T value, out bool found)
        {
            if (colorDictionary.TryGetValue(value, out var colorData))
            {
                found = true;
                return colorData.GetColor();
            }

            found = false;
            return GetDefaultParamColor(value);
        }

        protected virtual ColorPathData GetData(T value, out bool found, bool autoFill = false)
        {
            if (colorDictionary.TryGetValue(value, out var colorData))
            {
                found = true;
                return colorData;
            }

            if (autoFill)
            {
                colorDictionary.Add(value, new ColorPathData()
                {
                    Color = GetDefaultParamColor(value)
                });

                return GetData(value, out found);
            }

            found = false;
            return default;
        }

        protected abstract T GetValue(Path path);

        protected virtual Color GetDefaultParamColor(T value) => Color.white;

        #endregion

        #region Private methods

        private void SwitchCustomColoringState(bool state)
        {
            SwitchCustomColoringState(PathDataViewerWindow.Paths, state);
        }

        private void SwitchCustomColoringState(Path[] paths, bool state)
        {
            foreach (var path in paths)
            {
                T value = GetValue(path);
                bool found;
                Color color = GetColor(value, out found);

                if (!colorDictionary.ContainsKey(value))
                {
                    colorDictionary.Add(value, new ColorPathData()
                    {
                        Color = color
                    });
                }
                else
                {
                    color = colorDictionary[value].GetColor();
                }

                SetPathColor(path, color, state);

                foreach (var pathNode in path.WayPoints)
                {
                    if (PathDataViewerWindow.DrawCustomColors && state)
                    {
                        pathNode.HasPathCustomColor = true;
                        pathNode.PathCustomColor = color;
                    }
                    else
                    {
                        pathNode.HasPathCustomColor = false;
                    }
                }
            }
        }

        #endregion
    }
}
#endif