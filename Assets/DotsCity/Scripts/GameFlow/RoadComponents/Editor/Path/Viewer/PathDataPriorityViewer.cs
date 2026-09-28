#if UNITY_EDITOR
using Spirit604.Collections.Dictionary;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public class PathDataPriorityViewer : PathDataViewerBase
    {
        #region Helper types

        private const string priorityDictSaveKey = "PathDataPriorityViewer_PriorityColorDictionary";

        [System.Serializable]
        private class PriorityColorDictionary : AbstractSerializableDictionary<int, ColorPathData> { }

        #endregion

        #region Variables

        private PriorityColorDictionary priorityColorDictionary = new PriorityColorDictionary();
        private bool colorsFoldout = true;
        private bool shouldRemoveKey;
        private int removeKey;

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
                foreach (var nodeData in priorityColorDictionary)
                {
                    EditorGUILayout.BeginHorizontal();

                    string priorityText = $"Priority: {nodeData.Key} ";

                    if (nodeData.Key == 0)
                    {
                        priorityText += "(Default)";
                    }

                    EditorGUILayout.LabelField(priorityText);

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

                    DrawRemoveButton(() =>
                    {
                        shouldRemoveKey = true;
                        removeKey = nodeData.Key;
                    });

                    EditorGUILayout.EndHorizontal();
                }
            };

            InspectorExtension.DrawDefaultInspectorGroupBlock("Colors Info", colorsContent, ref colorsFoldout);

            if (shouldRemoveKey)
            {
                priorityColorDictionary.Remove(removeKey);
                SwitchCustomColoringState(true);
                SceneView.RepaintAll();
            }
        }

        public override void SaveData()
        {
            var data = JsonUtility.ToJson(priorityColorDictionary, false);
            EditorPrefs.SetString(priorityDictSaveKey, data);
        }

        public override void LoadData()
        {
            var data = EditorPrefs.GetString(priorityDictSaveKey);

            if (!string.IsNullOrEmpty(data))
            {
                try
                {
                    var savedData = JsonUtility.FromJson<PriorityColorDictionary>(data);

                    if (savedData != null)
                    {
                        priorityColorDictionary = savedData;
                    }
                    else
                    {
                        priorityColorDictionary.Clear();
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

            if (priorityColorDictionary.TryGetValue(path.Priority, out var data))
            {
                if (!data.Enabled)
                {
                    show = false;
                }
            }

            return show;
        }

        #endregion

        #region Methods

        private void SwitchCustomColoringState(bool state)
        {
            SwitchCustomColoringState(PathDataViewerWindow.Paths, state);
        }

        private void SwitchCustomColoringState(Path[] paths, bool state)
        {
            foreach (var path in paths)
            {
                int priority = (int)path.Priority;
                Color color = GetColor(priority);

                if (!priorityColorDictionary.ContainsKey(priority))
                {
                    priorityColorDictionary.Add(priority, new ColorPathData()
                    {
                        Color = color
                    });

                    SortDict();
                }
                else
                {
                    color = priorityColorDictionary[priority].GetColor();
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

        private void SortDict()
        {
            if (priorityColorDictionary.Keys.Count > 0)
            {
                var dict = priorityColorDictionary.OrderBy(obj => obj.Key).ToDictionary(obj => obj.Key, obj => obj.Value);
                priorityColorDictionary.Clear();

                foreach (var item in dict)
                {
                    priorityColorDictionary.Add(item.Key, item.Value);
                }
            }
        }

        private Color GetColor(int priority)
        {
            if (priorityColorDictionary.TryGetValue(priority, out var colorData))
            {
                return colorData.GetColor();
            }

            var color = Color.white;

            if (priority < 0)
            {
                return Color.red;
            }
            if (priority > 0)
            {
                return Color.green;
            }

            return color;
        }

        #endregion
    }
}
#endif