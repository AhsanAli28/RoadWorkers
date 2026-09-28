#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Spirit604.Extensions
{
    public static class InspectorExtension
    {
        public static readonly GUIStyle s_rightButton = "ButtonRight";
        public static readonly GUIStyle s_radioButton = "Radio";

        public static void DrawDefaultInspectorGroupBlock(string title, Action content)
        {
            GUILayout.BeginVertical("HelpBox");

            if (!string.IsNullOrEmpty(title))
            {
                GUILayout.Label(title);
            }

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical("GroupBox");

            content?.Invoke();

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        public static void DrawDefaultInspectorGroupBlock(string title, Action content, ref bool foldOutValue)
        {
            GUILayout.BeginVertical("HelpBox");

            foldOutValue = EditorGUILayout.Foldout(foldOutValue, title);

            if (foldOutValue)
            {
                GUILayout.BeginHorizontal();

                GUILayout.BeginVertical("GroupBox");

                content?.Invoke();

                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }

        public static void DrawDefaultInspectorGroupBlock(string title, Action content, SerializedProperty sp)
        {
            GUILayout.BeginVertical("HelpBox");

            var newFoldOutValue = EditorGUILayout.Foldout(sp.boolValue, title);

            if (sp.boolValue != newFoldOutValue)
            {
                sp.boolValue = newFoldOutValue;
            }

            if (newFoldOutValue)
            {
                GUILayout.BeginHorizontal();

                GUILayout.BeginVertical("GroupBox");

                content?.Invoke();

                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }

        public static void DrawInspectorLine(Color color, int thickness = 2, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            DrawInspectorLine(r, color, thickness, padding);
        }

        public static void DrawInspectorLine(Rect r, Color color, int thickness = 2, int padding = 10)
        {
            r.height = thickness;
            r.y += padding / 2;
            r.x -= 2;
            r.width += 6;
            EditorGUI.DrawRect(r, color);
        }

        public static void DrawGroupBox(string title, Action content, bool highlightTitle = false)
        {
            GUILayout.BeginVertical("GroupBox");

            if (highlightTitle)
            {
                GUILayout.BeginVertical("HelpBox");
            }

            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            if (highlightTitle)
            {
                GUILayout.EndVertical();
            }

            content?.Invoke();

            GUILayout.EndVertical();
        }

        public static void DrawGroupBox(string title, Action content, ref bool foldoutFlag, bool highlightTitle = false)
        {
            GUILayout.BeginVertical("GroupBox");

            if (highlightTitle)
            {
                GUILayout.BeginVertical("HelpBox");
            }

            foldoutFlag = EditorGUILayout.ToggleLeft(title, foldoutFlag, EditorStyles.boldLabel);

            if (highlightTitle)
            {
                GUILayout.EndVertical();
            }

            if (foldoutFlag)
            {
                content?.Invoke();
            }

            GUILayout.EndVertical();
        }

        public static void DrawGroupBox(string title, Action content, string editorPrefsFlagName, bool highlightTitle = false)
        {
            GUILayout.BeginVertical("GroupBox");

            if (highlightTitle)
            {
                GUILayout.BeginVertical("HelpBox");
            }

            var foldoutFlag = EditorPrefs.GetBool(editorPrefsFlagName, true);

            var newFoldoutFlag = EditorGUILayout.ToggleLeft(title, foldoutFlag, EditorStyles.boldLabel);

            if (highlightTitle)
            {
                GUILayout.EndVertical();
            }

            if (newFoldoutFlag)
            {
                content?.Invoke();
            }

            if (foldoutFlag != newFoldoutFlag)
            {
                EditorPrefs.SetBool(editorPrefsFlagName, newFoldoutFlag);
            }

            GUILayout.EndVertical();
        }

        public static void DrawGroupBox(string title, Action content, SerializedObject so, string foldoutPropName, bool highlightTitle = false)
        {
            var foldoutProp = so.FindProperty(foldoutPropName);
            DrawGroupBox(title, content, foldoutProp, highlightTitle);
        }

        public static void DrawGroupBox(string title, Action content, SerializedProperty foldoutProp, bool highlightTitle = false)
        {
            GUILayout.BeginVertical("GroupBox");

            if (highlightTitle)
            {
                GUILayout.BeginVertical("HelpBox");
            }

            var newFoldoutFlag = EditorGUILayout.ToggleLeft(title, foldoutProp.boolValue, EditorStyles.boldLabel);

            if (foldoutProp.boolValue != newFoldoutFlag)
            {
                foldoutProp.boolValue = newFoldoutFlag;
            }

            if (highlightTitle)
            {
                GUILayout.EndVertical();
            }

            if (foldoutProp.boolValue)
            {
                content?.Invoke();
            }

            GUILayout.EndVertical();
        }

        public static void DrawGroupBox(Action content)
        {
            GUILayout.BeginVertical("GroupBox");

            content?.Invoke();

            GUILayout.EndVertical();
        }

        public static void DrawHelpBox(string title, Action content, bool highlightTitle = false)
        {
            GUILayout.BeginVertical("HelpBox");

            if (highlightTitle)
            {
                GUILayout.BeginVertical("GroupBox");
            }

            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            if (highlightTitle)
            {
                GUILayout.EndVertical();
            }

            content?.Invoke();

            GUILayout.EndVertical();
        }

        public static void DrawHelpBox(Action content)
        {
            GUILayout.BeginVertical("HelpBox");

            content?.Invoke();

            GUILayout.EndVertical();
        }

        public static Object[] DrawDropAreaGUI()
        {
            Rect myRect = GUILayoutUtility.GetRect(0, 100, GUILayout.ExpandWidth(true));

            return DrawDropAreaGUI(myRect);
        }

        public static Object[] DrawDropAreaGUI(Rect rect, string dropBoxText = "Drag and Drop Prefabs to this Box!")
        {
            GUI.Box(rect, dropBoxText);

            if (rect.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.DragUpdated)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.DragPerform)
                {
                    Event.current.Use();

                    return DragAndDrop.objectReferences;
                }
            }

            return null;
        }

        public static void DrawVerticalArrowButton(Action onClick, GUIStyle arrowButtonStyle = null, float arrowOffset = 115f, int arrowFontSize = 25, float arrowWidth = 25f, float arrowHeight = 40f)
        {
            if (DrawVerticalArrowButton(arrowButtonStyle, arrowOffset, arrowFontSize, arrowWidth, arrowHeight))
            {
                onClick?.Invoke();
            }
        }

        public static bool DrawVerticalArrowButton(GUIStyle arrowButtonStyle = null, float arrowOffset = 115f, int arrowFontSize = 25, float arrowWidth = 25f, float arrowHeight = 40f)
        {
            if (arrowButtonStyle == null)
            {
                arrowButtonStyle = new GUIStyle("button");
                arrowButtonStyle.fontSize = arrowFontSize;
            }

            var rect = GUILayoutUtility.GetLastRect();

            rect.width = arrowWidth;
            rect.height = arrowHeight;
            rect.x += arrowOffset;

            if (GUI.Button(rect, "⇕", arrowButtonStyle))
            {
                return true;
            }

            return false;
        }

        public static void DrawEnumToolbar<T>(SerializedProperty prop, bool addGroup = false, string header = "") where T : Enum
        {
            var selectedFlags = (T)(object)prop.enumValueIndex;

            var newFlag = (int)(object)DrawEnumToolbar<T>(selectedFlags, addGroup, header);

            if (prop.enumValueIndex != newFlag)
            {
                prop.enumValueIndex = newFlag;
            }
        }

        public static T DrawEnumToolbar<T>(T selectedFlag, bool addGroup = false, string header = "") where T : Enum
        {
            if (addGroup)
            {
                GUILayout.BeginVertical("HelpBox");
            }

            if (!string.IsNullOrEmpty(header))
            {
                EditorGUILayout.PrefixLabel(header, EditorStyles.boldLabel);
            }

            var enumType = typeof(T);

            string[] names;
            int[] values;

            GetEnumdata<T>(enumType, out names, out values);

            var name = StringExtension.CamelToLabel(selectedFlag.ToString());
            var localIndex = Array.IndexOf(names, name);

            var newFlag = selectedFlag;

            GUILayout.BeginHorizontal();

            for (int i = 0; i < names.Length; i++)
            {
                var sourceVal = i == localIndex;
                var text = names[i];

                var newVal = GUILayout.Toggle(sourceVal, text, "Button");

                if (sourceVal != newVal)
                {
                    newFlag = (T)(object)values[i];
                }
            }

            GUILayout.EndHorizontal();

            if (addGroup)
            {
                GUILayout.EndVertical();
            }

            return newFlag;
        }

        public static void DrawEnumFlagsToolbar<T>(SerializedProperty prop, bool addGroup = false, string header = "") where T : Enum
        {
            var selectedFlags = (T)(object)prop.enumValueFlag;

            var newFlags = (int)(object)DrawEnumFlagsToolbar<T>(selectedFlags, addGroup, header);

            if (prop.enumValueFlag != newFlags)
            {
                prop.enumValueFlag = newFlags;
            }
        }

        public static T DrawEnumFlagsToolbar<T>(T selectedFlags, bool addGroup = false, string header = "") where T : Enum
        {
            if (addGroup)
            {
                GUILayout.BeginVertical("HelpBox");
            }

            if (!string.IsNullOrEmpty(header))
            {
                EditorGUILayout.PrefixLabel(header, EditorStyles.boldLabel);
            }

            var enumType = typeof(T);

            string[] names;
            int[] values;

            GetEnumdata<T>(enumType, out names, out values);

            GUILayout.BeginHorizontal();

            for (int i = 0; i < names.Length; i++)
            {
                var flag = (T)(object)values[i];
                var sourceVal = selectedFlags.HasFlag(flag);
                var text = names[i];

                var newVal = GUILayout.Toggle(sourceVal, text, "Button");

                if (sourceVal != newVal)
                {
                    var sourceFlags = (int)(object)selectedFlags;
                    var currentFlag = (int)(object)flag;

                    if (newVal)
                    {
                        selectedFlags = (T)(object)(sourceFlags | currentFlag);
                    }
                    else
                    {
                        selectedFlags = (T)(object)(sourceFlags & ~currentFlag);
                    }
                }
            }

            GUILayout.EndHorizontal();

            var newFlags = (T)(object)selectedFlags;

            if (addGroup)
            {
                GUILayout.EndVertical();
            }

            return newFlags;
        }

        private static Dictionary<Type, CachedEnumData> CachedData = new Dictionary<Type, CachedEnumData>();

        private class CachedEnumData
        {
            public string[] CachedEnumNames;
            public int[] CachedEnumValues;
        }

        private static void GetEnumdata<T>(Type enumType, out string[] names, out int[] values) where T : Enum
        {
            if (!CachedData.ContainsKey(enumType))
            {
                CachedData.Add(enumType, new CachedEnumData()
                {
                    CachedEnumNames = Enum.GetNames(typeof(T)).Select(a => StringExtension.CamelToLabel(a)).ToArray(),
                    CachedEnumValues = Enum.GetValues(typeof(T)).Cast<int>().ToArray()
                });
            }

            names = CachedData[enumType].CachedEnumNames;
            values = CachedData[enumType].CachedEnumValues;
        }

        public static void DrawDefaultHeaderScript(Object target)
        {
            EditorGUI.BeginDisabledGroup(true);

            var monoScript = (target as MonoBehaviour) != null
                ? MonoScript.FromMonoBehaviour((MonoBehaviour)target)
                : MonoScript.FromScriptableObject((ScriptableObject)target);

            EditorGUILayout.ObjectField("Script", monoScript, monoScript.GetType(), false);

            EditorGUI.EndDisabledGroup();
        }

        public static GUIContent GetPropertyLabel(SerializedProperty serializedProperty, string label)
        {
            if (string.IsNullOrEmpty(serializedProperty.tooltip))
            {
                return new GUIContent(label);
            }
            else
            {
                return new GUIContent(label, serializedProperty.tooltip);
            }
        }

        public static bool DrawClipboardButton(string copyText)
        {
            var rect = GUILayoutUtility.GetLastRect();

            rect.x += rect.width - 27f;
            rect.width = 25f;

            var icon = EditorGUIUtility.IconContent("Clipboard");

            if (GUI.Button(rect, icon))
            {
                EditorGUIUtility.systemCopyBuffer = copyText;
                return true;
            }

            return false;
        }

        public static void DrawConfigField<T>(SerializedObject serializedObject, string propName, string configName, Action generationCallback = null) where T : ScriptableObject
        {
            var configProp = serializedObject.FindProperty(propName);

            EditorGUILayout.PropertyField(configProp);

            if (configProp.objectReferenceValue == null)
            {
                var rect = GUILayoutUtility.GetLastRect();

                const float configWidth = 65f;
                rect.x = EditorGUIUtility.labelWidth - configWidth;
                rect.width = configWidth;

                if (GUI.Button(rect, "Generate"))
                {
                    configProp.objectReferenceValue = GenerateConfig<T>(configName);
                    generationCallback?.Invoke();
                }
            }
        }

        private static T GenerateConfig<T>(string name) where T : ScriptableObject
        {
            var config = AssetDatabaseExtension.CreatePersistScriptableObject<T>($"Assets/{name}.asset");

            EditorGUIUtility.PingObject(config);
            return config as T;
        }
    }
}
#endif