#if UNITY_EDITOR
using Spirit604.Collections.Dictionary;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public class PathTypeDataViewer : PathDataViewerBase
    {
        #region Helper types

        private const string pathTypeSaveKey = "PathDataChangeLaneViewer_PathTypeColorDictionary";

        [System.Serializable]
        private class PathTypeColorDictionary : AbstractSerializableDictionary<PathRoadType, ColorPathData> { }

        #endregion

        #region Variables

        private PathTypeColorDictionary pathTypeColorDictionary = new PathTypeColorDictionary();
        private bool colorsFoldout = true;
        private bool shouldRemoveKey;
        private PathRoadType removeKey;

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
                foreach (var nodeData in pathTypeColorDictionary)
                {
                    EditorGUILayout.BeginHorizontal();

                    string pathTypeText = $"PathType: {nodeData.Key} ";

                    EditorGUILayout.LabelField(pathTypeText);

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
                shouldRemoveKey = false;
                pathTypeColorDictionary.Remove(removeKey);
                SwitchCustomColoringState(true);
                SceneView.RepaintAll();
            }
        }

        public override void SaveData()
        {
            var data = JsonUtility.ToJson(pathTypeColorDictionary, false);
            EditorPrefs.SetString(pathTypeSaveKey, data);
        }

        public override void LoadData()
        {
            var data = EditorPrefs.GetString(pathTypeSaveKey);

            if (!string.IsNullOrEmpty(data))
            {
                try
                {
                    var savedData = JsonUtility.FromJson<PathTypeColorDictionary>(data);

                    if (savedData != null)
                    {
                        pathTypeColorDictionary = savedData;
                    }
                    else
                    {
                        pathTypeColorDictionary.Clear();
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

            if (pathTypeColorDictionary.TryGetValue(path.PathRoadType, out var data))
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
                var type = path.PathRoadType;
                Color color = GetColor(type);

                if (!pathTypeColorDictionary.ContainsKey(type))
                {
                    pathTypeColorDictionary.Add(type, new ColorPathData()
                    {
                        Color = color
                    });
                }
                else
                {
                    color = pathTypeColorDictionary[type].GetColor();
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

        private Color GetColor(PathRoadType priority)
        {
            if (pathTypeColorDictionary.TryGetValue(priority, out var colorData))
            {
                return colorData.GetColor();
            }

            var color = Color.white;

            return color;
        }

        #endregion
    }
}
#endif