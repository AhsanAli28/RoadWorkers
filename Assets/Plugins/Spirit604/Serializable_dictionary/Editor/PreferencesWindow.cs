using UnityEditor;
using UnityEngine;

namespace Spirit604.Collections.Dictionary.Editor
{
    public class PreferencesWindow
    {
        #region GUIContent
        static readonly GUIContent gui_pagesTitle = new GUIContent("Pages", "Section that has all the pages settings for the drawer");
        static readonly GUIContent gui_showPages = new GUIContent("Show Pages", "Should the drawer be divided in pages?");
        static readonly GUIContent gui_pageCount = new GUIContent("Items Per Page Count", "How many elements per page are going to be drawn");
        #endregion

        // Have we loaded the prefs yet
        private static bool prefsLoaded = false;

        //Default values
        private static bool showPages;
        private static int itemsPerPageCount;

        // Add preferences section named "My Preferences" to the Preferences Window
#pragma warning disable 0618
        [PreferenceItem("604Spirit/Dictionary")]
#pragma warning restore 0618
        public static void PreferencesGUI()
        {
            if (!prefsLoaded)
            {
                showPages = SerializableDictionaryConstans.ShowPages;
                itemsPerPageCount = SerializableDictionaryConstans.ItemsPerPageCount;

                prefsLoaded = true;
            }

            EditorGUILayout.LabelField(gui_pagesTitle, EditorStyles.boldLabel);
            EditorGUILayout.Space();

            showPages = EditorGUILayout.Toggle(gui_showPages, showPages);

            GUI.enabled = showPages;

            itemsPerPageCount = Mathf.Clamp(EditorGUILayout.IntField(gui_pageCount, itemsPerPageCount), 5, int.MaxValue);

            GUI.enabled = true;

            if (GUI.changed)
            {
                SerializableDictionaryConstans.ShowPages = showPages;
                SerializableDictionaryConstans.ItemsPerPageCount = itemsPerPageCount;
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Restore Default"))
            {
                SerializableDictionaryConstans.RestoreDefaults();
                prefsLoaded = false;
            }
        }
    }
}