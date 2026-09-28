#if UNITY_EDITOR
using Spirit604.Collections.Dictionary;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public abstract class PathWaypointParamViewerBase<T> : PathDataViewerBase
    {
        #region Helper types

        [Serializable]
        protected class ColorDictionary : AbstractSerializableDictionary<T, ColorPathData> { }

        #endregion

        #region Variables

        private bool colorsFoldout = true;
        private SortedDictionary<T, WayInfo> wayNodeInfos = new SortedDictionary<T, WayInfo>();
        protected ColorDictionary colorDictionary = new ColorDictionary();
        private bool remove;
        private T keyToRemove;
        private Path[] paths;

        #endregion

        #region Properties

        protected virtual string PathSaveDictionaryKey { get; set; } = $"{nameof(T)}_ColorDictionary";

        #endregion

        #region Overriden Methods

        public override void SwitchEnabledState(bool isEnabled)
        {
            SwitchCustomColoringState(isEnabled);
        }

        public override void Initialize(PathDataViewerWindow pathDataViewerWindow)
        {
            base.Initialize(pathDataViewerWindow);
            SortDict();
        }

        public override void UpdateData(Path[] paths)
        {
            this.paths = paths;
            wayNodeInfos.Clear();

            foreach (var path in paths)
            {
                foreach (var pathNode in path.WayPoints)
                {
                    var value = GetParamValue(pathNode);
                    Color color = default;

                    if (!colorDictionary.ContainsKey(value))
                    {
                        color = GetDefaultParamColor(value);

                        colorDictionary.Add(value, new ColorPathData()
                        {
                            Color = color
                        });

                        SortDict();
                    }
                    else
                    {
                        color = colorDictionary[value].GetColor();
                    }

                    if (!wayNodeInfos.ContainsKey(value))
                    {
                        wayNodeInfos.Add(value, new WayInfo() { });
                    }

                    path.HasPathCustomColor = true;

                    if (PathDataViewerWindow.DrawCustomColors)
                    {
                        pathNode.HasPathCustomColor = true;
                        pathNode.PathCustomColor = color;
                    }

                    wayNodeInfos[value].Nodes.TryToAdd(pathNode);
                }
            }
        }

        public override void DrawCustomSettings()
        {
            Action colorsContent = () =>
            {
                foreach (var nodeData in colorDictionary)
                {
                    EditorGUILayout.BeginHorizontal();

                    string labelText = GetLabelText(nodeData.Key);

                    EditorGUILayout.LabelField(labelText);

                    EditorGUI.BeginChangeCheck();

                    nodeData.Value.Color = EditorGUILayout.ColorField(nodeData.Value.Color);

                    if (EditorGUI.EndChangeCheck())
                    {
                        if (PathDataViewerWindow.DrawCustomColors)
                        {
                            UpdateColorRoute();
                            SceneView.RepaintAll();
                        }
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
                        UpdateColorRoute();
                        SceneView.RepaintAll();
                    }

                    DrawRemoveButton(() =>
                    {
                        remove = true;
                        keyToRemove = nodeData.Key;
                    });

                    EditorGUILayout.EndHorizontal();

                    if (remove)
                    {
                        break;
                    }
                }
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Colors Info", colorsContent, ref colorsFoldout);

            if (remove)
            {
                remove = false;
                colorDictionary.Remove(keyToRemove);
                keyToRemove = default;
                UpdateColorRoute();
                SceneView.RepaintAll();
            }
        }

        public override void SaveData()
        {
            var data = JsonUtility.ToJson(colorDictionary, false);
            EditorPrefs.SetString(PathSaveDictionaryKey, data);
        }

        public override void LoadData()
        {
            var data = EditorPrefs.GetString(PathSaveDictionaryKey);

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

        public override bool ShouldShowPathButton(Path path)
        {
            bool show = true;

            for (int i = 0; i < path.WayPoints.Count; i++)
            {
                PathNode node = path.WayPoints[i];

                if (node != null)
                {
                    var value = GetParamValue(node);

                    if (colorDictionary.TryGetValue(value, out var data))
                    {
                        if (!data.Enabled)
                        {
                            show = false;
                            break;
                        }
                    }
                }
            }

            return show;
        }

        #endregion

        #region Methods

        private void SwitchCustomColoringState(bool state)
        {
            foreach (var nodeData in wayNodeInfos)
            {
                foreach (var node in nodeData.Value.Nodes)
                {
                    if (!state)
                    {
                        node.HasPathCustomColor = false;
                    }
                    else
                    {
                        if (GetColor(node, out var color))
                        {
                            node.HasPathCustomColor = true;
                            node.PathCustomColor = color;
                        }
                        else
                        {
                            node.HasPathCustomColor = false;
                        }
                    }
                }
            }

            for (int i = 0; i < paths?.Length; i++)
            {
                paths[i].HasPathCustomColor = state;
            }
        }

        private void UpdateColorRoute()
        {
            foreach (var nodeData in wayNodeInfos)
            {
                foreach (var node in nodeData.Value.Nodes)
                {
                    if (GetColor(node, out var color))
                    {
                        node.HasPathCustomColor = true;
                        node.PathCustomColor = color;
                    }
                    else
                    {
                        node.HasPathCustomColor = false;
                    }
                }
            }
        }

        protected bool GetColor(PathNode pathNode, out Color color)
        {
            var value = GetParamValue(pathNode);

            if (colorDictionary.TryGetValue(value, out var data))
            {
                color = data.GetColor();

                return true;
            }
            else
            {
                color = PathDataViewerWindow.DefaultColor;
            }

            return false;
        }

        protected virtual void SortDict()
        {
            if (colorDictionary.Keys.Count > 0)
            {
                var dict = colorDictionary.OrderBy(obj => obj.Key).ToDictionary(obj => obj.Key, obj => obj.Value);
                colorDictionary.Clear();

                foreach (var item in dict)
                {
                    colorDictionary.Add(item.Key, item.Value);
                }
            }
        }

        protected abstract string GetLabelText(T value);
        protected abstract T GetParamValue(PathNode pathNode);
        protected abstract Color GetDefaultParamColor(T value);

        #endregion
    }
}
#endif