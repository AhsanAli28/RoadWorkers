#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Spirit604.Extensions
{
    public class SelectablePopup
    {
        private readonly string label;
        private readonly string[] optionNames;
        private readonly Action[] onSelectOptions;
        private readonly Func<bool>[] enabledOptions;

        public int SelectedIndex { get; set; }
        public int Count => optionNames.Length;

        public SelectablePopup(string label, string[] optionNames, bool autoInitActions = true)
        {
            this.label = label;
            this.optionNames = optionNames;

            if (autoInitActions)
            {
                this.onSelectOptions = new Action[optionNames.Length];
            }

            this.enabledOptions = new Func<bool>[optionNames.Length];

            for (int i = 0; i < enabledOptions.Length; i++)
            {
                enabledOptions[i] = () => { return true; };
            }
        }

        public SelectablePopup(string label, string[] optionNames, Action[] onSelectOptions) : this(label, optionNames, false)
        {
            this.optionNames = optionNames;
            this.onSelectOptions = onSelectOptions;
        }

        public SelectablePopup(string label, string[] optionNames, Func<bool>[] enabledOptions, Action[] onSelectOptions)
        {
            this.label = label;
            this.optionNames = optionNames;
            this.enabledOptions = enabledOptions;
            this.onSelectOptions = onSelectOptions;
        }

        public void ShowButton()
        {
            ShowButton(EditorStyles.layerMaskField);
        }

        public void ShowButton(Rect rect)
        {
            ShowButton(rect, EditorStyles.layerMaskField);
        }

        public void ShowButton(GUIStyle buttonGUIStyle)
        {
            EditorGUILayout.BeginHorizontal();

            if (!string.IsNullOrEmpty(label))
            {
                EditorGUILayout.PrefixLabel(label);
            }

            if (GUILayout.Button(optionNames[SelectedIndex], buttonGUIStyle))
            {
                DrawContextMenu();
            }

            EditorGUILayout.EndHorizontal();
        }

        public void ShowButton(Rect rect, GUIStyle buttonGUIStyle)
        {
            if (GUI.Button(rect, optionNames[SelectedIndex], buttonGUIStyle))
            {
                DrawContextMenu();
            }
        }

        public void AddEnabledOption(int index, Func<bool> enabledOption)
        {
            enabledOptions[index] = enabledOption;
        }

        public void AddActionOption(int index, Action onSelectOption)
        {
            onSelectOptions[index] = onSelectOption;
        }

        private void DrawContextMenu()
        {
            var genericMenu = new GenericMenu();

            for (int i = 0; i < optionNames.Length; i++)
            {
                int index = i;

                var isEnabled = enabledOptions[i].Invoke();

                if (isEnabled)
                {
                    var selected = SelectedIndex == index;

                    genericMenu.AddItem(new GUIContent(optionNames[index]), selected, () =>
                    {
                        SelectedIndex = index;
                        onSelectOptions[index]?.Invoke();
                    });
                }
                else
                {
                    genericMenu.AddDisabledItem(new GUIContent(optionNames[index]));
                }
            }

            genericMenu.ShowAsContext();
            Event.current.Use();
        }
    }
}

#endif