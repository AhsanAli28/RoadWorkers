#if UNITY_EDITOR
using System;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

[assembly: InternalsVisibleTo("604Spirit.AnimationBaker.Core")]
[assembly: InternalsVisibleTo("604Spirit.AnimationBaker.Editor")]

namespace Spirit604.AnimationBaker.EditorInternal
{
    internal static class InspectorExtension
    {
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
    }
}
#endif