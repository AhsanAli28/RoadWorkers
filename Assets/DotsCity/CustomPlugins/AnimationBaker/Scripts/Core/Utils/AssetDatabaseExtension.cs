#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[assembly: InternalsVisibleTo("604Spirit.AnimationBaker.Core")]
[assembly: InternalsVisibleTo("604Spirit.AnimationBaker.Editor")]

namespace Spirit604.AnimationBaker.EditorInternal
{
    internal class AssetDatabaseExtension
    {
        public static string GetCreatePath(string path, string name)
        {
            return string.Format(path + "{0}.asset", name);
        }

        public static string GetUniquePath(string path, string sourceName)
        {
            int index = 1;

            while (true)
            {
                var newName = sourceName + index.ToString();
                var assetPath = GetCreatePath(path, newName);

#if UNITY_EDITOR
                if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(assetPath)))
                    return assetPath;
#endif

                index++;
            }

#pragma warning disable CS0162
            return default;
#pragma warning restore CS0162
        }

        public static T CreatePersistScriptableObject<T>(string createPath, string name) where T : ScriptableObject
        {
            createPath = GetAssetSavePath(createPath, name);

            return CreatePersistScriptableObject<T>(createPath);
        }

        public static string GetAssetSavePath(string sourcePath, string name)
        {
            return GetSavePath(sourcePath, name, typeof(ScriptableObject));
        }

        public static string GetPrefabSavePath(string sourcePath, string name)
        {
            return GetSavePath(sourcePath, name, typeof(GameObject));
        }

        public static string GetSavePath(string sourcePath, string name, Type assetType)
        {
            var sb = new StringBuilder(sourcePath);

            if (sb[0] == '/')
            {
                sb.Remove(0, 1);
            }

            if (sb[sb.Length - 1] != '/')
            {
                sb.Append("/");
            }

            sb.Append(name);

            var assetTypeString = AssetTypeToString(assetType);

            sb.Append(assetTypeString);

            return sb.ToString();
        }

        public static string AssetTypeToString(Type type)
        {
            if (type == typeof(GameObject))
            {
                return ".prefab";
            }
            if (type == typeof(ScriptableObject))
            {
                return ".asset";
            }

            return string.Empty;
        }

        public static void SavePersistScriptableObject(ScriptableObject asset, string createPath, string name)
        {
            createPath = GetAssetSavePath(createPath, name);
            SavePersistScriptableObject(asset, createPath);
        }

        public static void SavePersistScriptableObject(ScriptableObject asset, string createPath)
        {
            AssetDatabase.CreateAsset(asset, createPath);
            AssetDatabase.SaveAssets();
        }

        public static T CreatePersistScriptableObject<T>(string createPath) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();

            AssetDatabase.CreateAsset(asset, createPath);
            AssetDatabase.SaveAssets();

            return asset;
        }

        /// <summary>
        /// Adds newly (if not already in the list) found assets.
        /// Returns how many found (not how many added)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <param name="assetsFound">Adds to this list if it is not already there</param>
        /// <returns></returns>
        public static List<T> TryGetUnityObjectsOfTypeFromPath<T>(string path) where T : UnityEngine.Object
        {
            if (!System.IO.Directory.Exists(path))
            {
                return null;
            }

            List<T> assetsFound = new List<T>();

            string[] filePaths = System.IO.Directory.GetFiles(path);

            if (filePaths != null && filePaths.Length > 0)
            {
                for (int i = 0; i < filePaths.Length; i++)
                {
                    UnityEngine.Object obj = UnityEditor.AssetDatabase.LoadAssetAtPath(filePaths[i], typeof(T));
                    if (obj is T asset)
                    {
                        if (!assetsFound.Contains(asset))
                        {
                            assetsFound.Add(asset);
                        }
                    }
                }
            }

            return assetsFound;
        }

        public static string GetAssetPathWithoutName(UnityEngine.Object asset)
        {
            var assetPath = AssetDatabase.GetAssetPath(asset);
            assetPath = assetPath.Substring(0, assetPath.LastIndexOf("/") + 1);
            return assetPath;
        }

        public static void SelectProjectFolder(string path, bool ping = false)
        {
            path = path.ClearPathSlashes();

            Object obj = AssetDatabase.LoadAssetAtPath<Object>(path);

            if (obj != null)
            {
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = obj;

                if (ping)
                {
                    EditorGUIUtility.PingObject(obj);
                }
            }
            else
            {
                UnityEngine.Debug.LogError($"'{path}' not found");
            }
        }

        public static string SelectProjectFolderFromDialogWindow(string title, string folder = "", string defaultName = "", bool addSlashAtEnd = true)
        {
            var path = EditorUtility.OpenFolderPanel(title, folder, defaultName);

            if (!string.IsNullOrEmpty(path))
            {
                path = ConvertToLocalProjectPath(path);

                if (addSlashAtEnd)
                {
                    path += "/";
                }
            }

            return path;
        }

        public static string ConvertToLocalProjectPath(string path)
        {
            var index = path.IndexOf("Assets");

            if (index > 0)
            {
                path = path.Substring(index, path.Length - index);
            }

            return path;
        }

        public static string FindRootProjectPath(string filterPhrase, string rootRelativePath)
        {
            var rootPath = string.Empty;

#if UNITY_EDITOR
            var guides = AssetDatabase.FindAssets(filterPhrase);

            if (guides?.Length > 0)
            {
                foreach (var guid in guides)
                {
                    var tempPath = AssetDatabase.GUIDToAssetPath(guid);

                    if (tempPath.Contains(rootRelativePath))
                    {
                        rootPath = tempPath.Replace(rootRelativePath, string.Empty);
                        break;
                    }
                }
            }
#endif

            return rootPath;
        }
    }
}
#endif