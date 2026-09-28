#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Spirit604.Extensions
{
    public static class EditorTipExtension
    {
        public static bool TryToShowInspectorTip(string key, string message)
        {
            var showed = EditorPrefs.GetBool(key, false);

            if (!showed)
            {
                EditorGUILayout.Space(EditorGUIUtility.singleLineHeight - 6);
                EditorGUILayout.HelpBox(message, MessageType.Info);

                var r = GUILayoutUtility.GetLastRect();

                r.x += r.width - 20;
                r.y -= EditorGUIUtility.singleLineHeight - 4;

                r.width = 20f;
                r.height = 20f;

                if (GUI.Button(r, "x"))
                {
                    EditorPrefs.SetBool(key, true);
                }

                return true;
            }

            return false;
        }
    }
}
#endif