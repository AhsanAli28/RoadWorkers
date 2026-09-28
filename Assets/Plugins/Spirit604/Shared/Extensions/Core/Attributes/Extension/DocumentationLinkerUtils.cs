using Spirit604.Extensions;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Spirit604.Attributes
{
    public static class DocumentationLinkerUtils
    {
        private const string IconName = "Read-the-docs";
        private const string DefaultUnityIcon = "d__Help";
        private const float ButtonSize = 25f;

        private static Texture iconTexture;
        private static bool cached;

        public static void ShowButton(string link, float offset = 0)
        {
#if UNITY_EDITOR
            var rect = GUILayoutUtility.GetLastRect();
            rect.x = EditorGUIUtility.labelWidth - 10;
            rect.y -= EditorGUIUtility.singleLineHeight * 1 + 5 + offset;
            rect.height = ButtonSize;
            rect.width = ButtonSize;

            ShowButton(rect, link);
#endif
        }

#if UNITY_EDITOR

        public static void ShowButtonFirst(string link, float yOffset = 0, float xOffset = 0)
        {
            var rect = EditorGUILayout.GetControlRect(false, 0);
            rect.x = EditorGUIUtility.labelWidth - 10 + xOffset;
            rect.y -= 3 + yOffset;
            rect.height = ButtonSize;
            rect.width = ButtonSize;

            ShowButton(rect, link);
        }

        public static void ShowButtonAndHeader(Object target, string link, float offset = 0)
        {
            ShowButtonFirst(link, offset);
            InspectorExtension.DrawDefaultHeaderScript(target);
        }

        public static void ShowButton(Rect rect, string link)
        {
            var tex = GetIcon();

            if (GUI.Button(rect, tex))
            {
                HandleClick(link);
            }
        }

        private static void HandleClick(string link)
        {
            Application.OpenURL(link);
            Event.current.Use();
        }

        private static GUIContent GetIcon()
        {
            if (!cached)
            {
                cached = true;
                var guids = AssetDatabase.FindAssets($"{IconName}");

                if (guids.Length > 0)
                {
                    iconTexture = AssetDatabase.LoadAssetAtPath<Texture>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }

            if (iconTexture == null)
            {
                return EditorGUIUtility.IconContent(DefaultUnityIcon);
            }

            return new GUIContent(iconTexture);
        }
#endif
    }
}
