using UnityEditor;

namespace Spirit604.Collections.Dictionary.Editor
{
    public static class SerializableDictionaryConstans
    {
        private const bool DefaultShowPages = false;
        private const int DefaultItemsPerPage = 15;

        private const string DefaultShowPagesKey = "604Spirit_ShowPagesKey";
        private const string DefaultItemsPerPageKey = "604Spirit_ItemsPerPageKey";

        public static bool ShowPages
        {
            get
            {
                return EditorPrefs.GetBool(DefaultShowPagesKey, DefaultShowPages);
            }
            set
            {
                EditorPrefs.SetBool(DefaultShowPagesKey, value);
            }
        }

        public static int ItemsPerPageCount
        {
            get
            {
                return EditorPrefs.GetInt(DefaultItemsPerPageKey, DefaultItemsPerPage);
            }
            set
            {
                EditorPrefs.SetInt(DefaultItemsPerPageKey, value);
            }
        }

        public static void RestoreDefaults()
        {
            ShowPages = DefaultShowPages;
            ItemsPerPageCount = DefaultItemsPerPage;
        }
    }
}