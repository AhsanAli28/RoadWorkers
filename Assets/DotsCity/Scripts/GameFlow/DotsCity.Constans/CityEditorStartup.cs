using UnityEditor;

namespace Spirit604.CityEditor
{
#if UNITY_EDITOR
    [InitializeOnLoad]
    public static class CityEditorStartup
    {
        private static string SEARCH_ROOT_PHRASE = "Hub source [not for use]";
        private static string PREFAB_ROOT_RELATIVE_PATH_TARGET_TEXT = "Prefabs/Init/Internal/Source Prefabs/Hub source [not for use].prefab";

        static CityEditorStartup()
        {
            UpdateRoot();
        }

        public static void InitConfigs(string rootPath)
        {
#if UNITY_EDITOR
            EditorPrefs.SetString(CityEditorBookmarks.GetRootSaveKey(), rootPath);
#endif
        }

        public static string FindRoot()
        {
            string root = string.Empty;

#if UNITY_EDITOR
            root = Extensions.AssetDatabaseExtension.FindRootProjectPath(SEARCH_ROOT_PHRASE, PREFAB_ROOT_RELATIVE_PATH_TARGET_TEXT);
#endif

            return root;
        }

        public static void UpdateRoot()
        {
            var currentRoot = CityEditorBookmarks.OBJECT_ROOT_PATH;

            if (string.IsNullOrEmpty(currentRoot))
            {
                ForceUpdateRoot();
            }
        }

        public static void ForceUpdateRoot()
        {
            var root = FindRoot();

            if (!string.IsNullOrEmpty(root))
            {
                InitConfigs(root);
            }
            else
            {
                UnityEngine.Debug.LogError($"DotsCity project root not found. Filter assets word '{SEARCH_ROOT_PHRASE}'. Search path '{PREFAB_ROOT_RELATIVE_PATH_TARGET_TEXT}'.");
            }
        }
    }
#endif
}
