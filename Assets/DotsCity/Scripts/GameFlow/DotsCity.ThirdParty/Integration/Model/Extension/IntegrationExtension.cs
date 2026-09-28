using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Spirit604.DotsCity.ThirdParty.Integration
{
    public class IntegrationExtension
    {
        public static T GetObjectByName<T>(Scene scene, string name) where T : MonoBehaviour
        {
            try
            {
                return scene.GetRootGameObjects().Where(a => a.name.Equals(name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().GetComponent<T>();
            }
            catch { }

            return null;
        }

        public static Scene GetActiveScene()
        {
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);

                if (!scene.isSubScene)
                    return scene;
            }

            return default;
        }

        public static string GetMainAssetNameBySubAsset(Object sourceObject)
        {
            try
            {
                if (sourceObject != null)
                {
                    var mainAsset = GetMainAssetBySubAsset(sourceObject);

                    if (mainAsset)
                        return mainAsset.name;
                }
            }
            catch { }

            return string.Empty;
        }

        public static string GetMainAssetNameBySubAsset(Object sourceObject, out string extension)
        {
            extension = string.Empty;

            try
            {
                if (sourceObject != null)
                {
                    var mainAsset = GetMainAssetBySubAsset(sourceObject);

                    if (mainAsset)
                    {
                        var path = AssetDatabase.GetAssetPath(mainAsset);

                        var index = path.LastIndexOf(".");
                        extension = path.Substring(index, path.Length - index);

                        return mainAsset.name;
                    }
                }
            }
            catch { }

            return string.Empty;
        }

        public static Object GetMainAssetBySubAsset(Object sourceObject)
        {
            try
            {
                if (sourceObject != null)
                {
                    var path = AssetDatabase.GetAssetPath(sourceObject);
                    return AssetDatabase.LoadMainAssetAtPath(path);
                }
            }
            catch { }

            return null;
        }

        public static T FindSubAsset<T>(string mainAssetName, string searchSubObjectName) where T : Object
        {
            return FindSubAsset<T>(mainAssetName, searchSubObjectName, ".fbx");
        }

        public static T FindSubAsset<T>(string mainAssetName, string searchSubObjectName, string extension) where T : Object
        {
            var assetPaths = FindAssetPaths(mainAssetName, extension);

            if (assetPaths == null || assetPaths.Count == 0)
            {
                UnityEngine.Debug.Log($"FindAsset. AssetType {typeof(T)} MainAssetName {mainAssetName} Mesh {searchSubObjectName} not found.");
                return null;
            }

            foreach (var assetPath in assetPaths)
            {
                var meshes = AssetDatabase.LoadAllAssetsAtPath(assetPath);

                var asset = FindSubAsset<T>(meshes, searchSubObjectName);

                if (asset)
                    return asset;
            }

            return null;
        }

        public static T FindSubAsset<T>(Object[] objects, string searchSubObjectName) where T : Object
        {
            if (objects?.Length > 0)
            {
                foreach (var obj in objects)
                {
                    if (obj is T && obj.name.Equals(searchSubObjectName, StringComparison.OrdinalIgnoreCase))
                    {
                        return obj as T;
                    }
                }
            }

            return null;
        }

        public static List<string> FindAssetPaths(string searchName, string extension)
        {
            List<string> paths = null;

            var d = new DirectoryInfo(Application.dataPath);

            foreach (var file in d.GetFiles($"{searchName}{extension}", SearchOption.AllDirectories))
            {
                var assetPath = file.FullName;

                var index = assetPath.IndexOf("Assets");
                assetPath = assetPath.Substring(index, assetPath.Length - index);
                assetPath = assetPath.Replace("\\", "/");

                if (paths == null)
                {
                    paths = new List<string>();
                }

                paths.Add(assetPath);
            }

            return paths;
        }
    }
}

