using Spirit604.CityEditor;
using Spirit604.Extensions;
using System.IO;
using UnityEditor;

namespace Spirit604.PackageManagerExtension
{
    public static class UpdateManager
    {
        private const string ProjectVersion = "1.4.0";
        private const string VersionData = "ProjectVersionData.asset";
        private const string Key = "UpdateManager_" + ProjectVersion;

        public static string FirstLaunchKey { get; private set; }

        public static void CheckForUpdate()
        {
            var key = GetKey();

            var updated = EditorPrefs.HasKey(key);

            if (!updated)
            {
                SetOrCreateVersion(true);
            }
        }

        public static void SetOrCreateVersion(bool importPackages = false)
        {
            SaveKey();

            CheckFolderExist();
            var projectVersionData = AssetDatabase.LoadAssetAtPath<ProjectVersionData>(GetVersionPath());

            if (projectVersionData != null && projectVersionData.Version == ProjectVersion)
                return;

            if (projectVersionData == null)
            {
                projectVersionData = AssetDatabaseExtension.CreatePersistScriptableObject<ProjectVersionData>(GetVersionPath());
            }

            projectVersionData.Version = ProjectVersion;
            EditorSaver.SetObjectDirty(projectVersionData);

            if (importPackages)
            {
                var projectUnpacker = ProjectUnpacker.GetUnpacker();
                projectUnpacker.TryToImportPackages();
                ConsoleExtension.ClearErrorsAndWarnings();
                UnityEngine.Debug.Log($"Project updated to {ProjectVersion} version");
            }
        }

        private static string GetVersionPath()
        {
            return GetFolderPath() + VersionData;
        }

        private static string GetFolderPath() => CityEditorBookmarks.GetPath(ProjectUnpacker.InternalPath);

        private static void CheckFolderExist()
        {
            var folderPath = GetFolderPath();

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            }
        }

        public static void SaveKey()
        {
            var key = GetKey();
            EditorPrefs.SetBool(key, true);
        }

        private static string GetKey() => $"{EditorExtension.GetUniquePrefsKey(Key)}";
    }
}
