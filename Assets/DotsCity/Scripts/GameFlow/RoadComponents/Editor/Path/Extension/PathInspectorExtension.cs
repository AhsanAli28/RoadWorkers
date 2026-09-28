#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Spirit604.CityEditor.Road
{
    public static class PathInspectorExtension
    {
        public const string PathEditorSettingsKey = "Path_EditorSettings";

        public static PathSharedEditorSettings GetPrefsSettings()
        {
            var jsonSettings = EditorPrefs.GetString(PathEditorSettingsKey, string.Empty);

            PathSharedEditorSettings pathSharedEditorSettings;

            if (string.IsNullOrEmpty(jsonSettings))
            {
                pathSharedEditorSettings = PathSharedEditorSettings.GetDefault();
            }
            else
            {
                pathSharedEditorSettings = JsonUtility.FromJson<PathSharedEditorSettings>(jsonSettings);
            }

            return pathSharedEditorSettings;
        }
    }
}
#endif