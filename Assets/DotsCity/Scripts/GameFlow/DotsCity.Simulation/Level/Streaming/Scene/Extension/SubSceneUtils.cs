#if UNITY_EDITOR
using Spirit604.Extensions;
using System.Collections.Generic;
using System.IO;
using Unity.Scenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Spirit604.DotsCity.Simulation.Level.Streaming
{
    public static class SubSceneUtils
    {
        public static SubScene CreateSubScene(string savePrefabPath, string assetName, List<GameObject> initialObjects = null, Transform customParent = null, bool autoCreatePath = false, bool autoLoadScene = false)
        {
            return CreateSubScene(savePrefabPath, assetName, Vector3.zero, initialObjects, customParent, autoCreatePath, autoLoadScene);
        }

        public static SubScene CreateSubScene(string savePrefabPath, string assetName, Vector3 subScenePosition, List<GameObject> initialObjects = null, Transform customParent = null, bool autoCreatePath = false, bool autoLoadScene = false)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            scene.isSubScene = true;

            for (int i = 0; i < initialObjects?.Count; i++)
            {
                var prefab = initialObjects[i].gameObject;
                SceneManager.MoveGameObjectToScene(prefab, scene);
            }

            var sceneAssetPath = Path.Combine(savePrefabPath, $"{assetName}.unity");

            if (autoCreatePath)
            {
                if (!Directory.Exists(savePrefabPath))
                {
                    Directory.CreateDirectory(savePrefabPath);
                }
            }

            EditorSceneManager.SaveScene(scene, sceneAssetPath);

            SceneManager.UnloadSceneAsync(scene);

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(sceneAssetPath);
            var subScene = new GameObject(assetName).AddComponent<SubScene>();

            subScene.SceneAsset = sceneAsset;

            subScene.transform.parent = customParent;
            subScene.transform.position = subScenePosition;
            subScene.AutoLoadScene = autoLoadScene;

            EditorSaver.SetObjectDirty(subScene);

            return subScene;
        }

        public static string GetSubfolderProjectPathOfActiveScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            var path = scene.path.Replace(".unity", "/");
            return path;
        }
    }
}
#endif