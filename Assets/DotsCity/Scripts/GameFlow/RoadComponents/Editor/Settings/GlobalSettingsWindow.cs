#if UNITY_EDITOR
using Spirit604.CityEditor.Road;
using Spirit604.Extensions;
using Spirit604.Gameplay.Road;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Level
{
    public class GlobalSettingsWindow : EditorWindowBase
    {
        #region Constans

        private const string DeleteButtonIcon = "CrossIcon";
        private const float DeleteButtonSize = 25f;

        private readonly string[] Tabs = { "Traffic Group Types", "Traffic Group Settings" };

        #endregion

        #region Constructor

        [MenuItem(CityEditorBookmarks.CITY_WINDOW_PATH + "Global Settings")]
        public static GlobalSettingsWindow ShowWindow()
        {
            GlobalSettingsWindow globalTrafficLightWindow = (GlobalSettingsWindow)GetWindow(typeof(GlobalSettingsWindow));
            globalTrafficLightWindow.titleContent = new GUIContent("Global Settings");

            return globalTrafficLightWindow;
        }

        protected override Vector2 GetDefaultWindowSize() => new Vector2(350, 500);

        #endregion

        #region Variables

        private int tabIndex;
        private List<int> flags;
        private List<string> flagTexts;
        private string newEnumText;
        private bool changed;
        private bool saving;
        private TrafficGroupMaskSettings settings;

        private bool CreateAvailable => !string.IsNullOrEmpty(newEnumText) && !flagTexts.Contains(newEnumText, StringComparer.OrdinalIgnoreCase);

        #endregion

        #region Unity methods

        protected override void OnEnable()
        {
            base.OnEnable();
            settings = TrafficGroupMaskSettings.Init();
            Reset();
        }

        private void OnGUI()
        {
            tabIndex = GUILayout.Toolbar(tabIndex, Tabs);

            switch (tabIndex)
            {
                case 0:
                    {
                        DrawGroupTab();
                        break;
                    }
                case 1:
                    {
                        DrawGroupSettingsTab();
                        break;
                    }
            }

            if (saving)
            {
                if (EditorApplication.isCompiling)
                {
                    EditorGUILayout.HelpBox("Compiling", MessageType.Info);
                }
                else
                {
                    saving = false;
                }
            }
        }

        #endregion

        #region Methods

        private void DrawGroupTab()
        {
            EditorGUILayout.BeginVertical("GroupBox");

            int removeIndex = -1;

            for (int i = 0; i < flags.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUI.BeginChangeCheck();

                var newEnumTextNew = EditorGUILayout.TextField(flagTexts[i]);

                if (EditorGUI.EndChangeCheck())
                {
                    if (!string.IsNullOrEmpty(newEnumTextNew) && flagTexts[i] != newEnumTextNew)
                    {
                        flagTexts[i] = newEnumTextNew;
                        changed = true;
                    }
                }

                GUI.enabled = i != 0;

                if (GUILayout.Button(EditorGUIUtility.IconContent(DeleteButtonIcon), GUILayout.Width(DeleteButtonSize), GUILayout.Height(DeleteButtonSize)))
                {
                    removeIndex = i;
                }

                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical("GroupBox");

            EditorGUILayout.PrefixLabel("New Group");

            newEnumText = EditorGUILayout.TextField(newEnumText);

            GUI.enabled = CreateAvailable;

            if (GUILayout.Button("Create"))
            {
                AddNew();
            }

            GUI.enabled = true;

            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical("GroupBox");

            GUI.enabled = changed;

            if (GUILayout.Button("Save"))
            {
                Save();
            }

            GUI.enabled = true;

            if (GUILayout.Button("Reset"))
            {
                Reset();
            }

            EditorGUILayout.EndVertical();

            if (removeIndex != -1)
            {
                RemoveFlag(removeIndex);
            }
        }

        private void DrawGroupSettingsTab()
        {
            if (settings != null)
            {
                var so = new SerializedObject(settings);
                TrafficGroupMaskSettingsEditor.DrawSettings(settings, so);
            }
            else
            {
                settings = TrafficGroupMaskSettings.Init();
            }
        }

        private void RemoveFlag(int index)
        {
            flags.RemoveAt(index);
            flagTexts.RemoveAt(index);
            changed = true;
            Repaint();
        }

        private void AddNew()
        {
            if (!CreateAvailable)
            {
                return;
            }

            var flag = EditorEnumExtension.GetClosestEmptyFlag(flags, out var localIndex);

            flags.Insert(localIndex, flag);
            flagTexts.Insert(localIndex, newEnumText);

            changed = true;
            newEnumText = string.Empty;
            Repaint();
        }

        private void Save()
        {
            EditorEnumExtension.SaveEnums<TrafficGroupType>(flagTexts, flags, true);
            changed = false;
            saving = true;
        }

        private void Reset()
        {
            var enumFlags = EnumExtension.GetAllFlags<TrafficGroupType>();
            this.flags = enumFlags.Select(a => (int)a).ToList();
            this.flagTexts = enumFlags.Select(a => a.ToString()).ToList();
            changed = false;
            newEnumText = string.Empty;
            Repaint();
        }

        #endregion
    }
}
#endif