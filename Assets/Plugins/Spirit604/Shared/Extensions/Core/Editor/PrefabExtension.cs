#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Spirit604.Extensions
{
    public static class PrefabExtension
    {
        public static List<T> FindAllPrefabInstances<T>(GameObject myPrefab, bool onlyRoot = false) where T : Component
        {
            List<T> result = new List<T>();
            T[] allObjects = (T[])ObjectUtils.FindObjectsOfType(typeof(T));
            foreach (T GO in allObjects)
            {
                var GO_prefab = PrefabUtility.GetCorrespondingObjectFromSource(GO.gameObject);

                if (myPrefab == GO_prefab)
                {
                    if (onlyRoot)
                    {
                        if (PrefabUtility.IsPartOfPrefabInstance(GO.gameObject))
                        {
                            result.Add(GO);
                        }
                    }
                    else
                    {
                        result.Add(GO);
                    }
                }
            }
            return result;
        }

        public static void AddPrefabComponent<T>(GameObject prefab, Action<GameObject> callback = null) where T : MonoBehaviour
        {
            if (PrefabUtility.IsPartOfPrefabAsset(prefab))
            {
                var path = AssetDatabase.GetAssetPath(prefab);
                var prefabRoot = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    prefabRoot.AddComponent<T>();

                    callback?.Invoke(prefabRoot);

                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                }
                catch (Exception ex) { Debug.LogException(ex); }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
            }
            else
                Debug.Log($"{prefab.name} is not prefab");
        }

        public static void RemovePrefabComponent<T>(GameObject prefab, Action<GameObject> callback = null) where T : MonoBehaviour
        {
            if (PrefabUtility.IsPartOfPrefabAsset(prefab))
            {
                var path = AssetDatabase.GetAssetPath(prefab);
                var prefabRoot = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    prefabRoot.AddComponent<T>();

                    callback?.Invoke(prefabRoot);

                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                }
                catch (Exception ex) { Debug.LogException(ex); }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
            }
            else
                Debug.Log($"{prefab.name} is not prefab");
        }

        public static void EditPrefab<T>(GameObject prefab, Action<T> callback = null) where T : MonoBehaviour
        {
            if (prefab == null)
                return;

            if (PrefabUtility.IsPartOfPrefabAsset(prefab))
            {
                var path = AssetDatabase.GetAssetPath(prefab);
                var prefabRoot = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (callback != null)
                    {
                        var comp = prefabRoot.GetComponent<T>();

                        if (comp)
                        {
                            callback(comp);
                        }
                        else
                        {
                            Debug.Log($"{comp.name} not added to prefab");
                        }
                    }

                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                }
                catch (Exception ex) { Debug.LogException(ex); }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
            }
            else
                Debug.Log($"{prefab.name} is not prefab");
        }

        public static void EditPrefab(string prefabPath, Action<GameObject> callback = null)
        {
            var newPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            EditPrefab(newPrefab, callback);
        }

        public static void EditPrefab(GameObject prefab, Action<GameObject> callback = null)
        {
            if (prefab == null)
                return;

            if (PrefabUtility.IsPartOfPrefabAsset(prefab))
            {
                var path = AssetDatabase.GetAssetPath(prefab);
                var prefabRoot = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (callback != null)
                    {
                        callback(prefabRoot);
                    }

                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                }
                catch (Exception ex) { Debug.LogException(ex); }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
            }
            else
                Debug.Log($"{prefab.name} is not prefab");
        }

        public static void ApplyPropertyOverride<T>(GameObject prefab, T targetObj, params string[] props) where T : MonoBehaviour
        {
            if (prefab == null || targetObj == null)
                return;

            if (PrefabUtility.IsPartOfPrefabAsset(prefab))
            {
                var path = AssetDatabase.GetAssetPath(prefab);
                var so = new SerializedObject(targetObj);

                try
                {
                    for (int i = 0; i < props.Length; i++)
                    {
                        var prop = so.FindProperty(props[i]);

                        if (prop != null)
                        {
                            PrefabUtility.ApplyPropertyOverride(prop, path, InteractionMode.UserAction);
                        }
                        else
                        {
                            Debug.LogError($"Apply Property Error. Property '{props[i]}' not found");
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError(e);
                }
            }
            else
                Debug.Log($"{prefab.name} is not prefab");
        }

        public static void RevertObjectOverride<T>(T targetObj, InteractionMode interactionMode = InteractionMode.UserAction) where T : MonoBehaviour
        {
            if (targetObj == null)
                return;

            if (PrefabUtility.IsPartOfPrefabInstance(targetObj.gameObject))
            {
                try
                {
                    PrefabUtility.RevertObjectOverride(targetObj, interactionMode);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"{targetObj.gameObject.name} {e.Message}");
                }
            }
            else
                Debug.Log($"{targetObj.gameObject.name} is not prefab instance");
        }

        public static bool IsPrefab(GameObject gameObject)
        {
            return gameObject.scene.rootCount == 0;
        }

        public static GameObject GetPrefabRoot(GameObject currentParent, GameObject previousParent)
        {
            if (currentParent == null)
                return previousParent;

            var root = PrefabUtility.GetOutermostPrefabInstanceRoot(currentParent);

            if (root != null)
            {
                if (root.transform.parent != null && PrefabUtility.IsPartOfAnyPrefab(root.transform.parent.gameObject))
                {
                    var result = GetPrefabRoot(root.transform.parent.gameObject, root);
                    return result;
                }
                else
                {
                    return root;
                }
            }

            return previousParent;
        }

        public static void CopyPrefabOverridenProperties(GameObject srcObject, GameObject dstObject)
        {
            var revertMods = PrefabUtility.GetPropertyModifications(dstObject);

            foreach (var mod in revertMods)
            {
                if (!mod.target)
                {
                    continue;
                }

                try
                {
                    var newTarget = dstObject.GetComponentInChildren(mod.target.GetType());

                    if (!newTarget)
                        continue;

                    var targetGo = new SerializedObject(newTarget);
                    targetGo.Update();

                    var targetProp = targetGo.FindProperty(mod.propertyPath);

                    if (targetProp != null)
                    {
                        PrefabUtility.RevertPropertyOverride(targetProp, InteractionMode.AutomatedAction);
                    }

                    targetGo.ApplyModifiedPropertiesWithoutUndo();
                    targetGo.Update();
                }
                catch { }
            }

            var mods = PrefabUtility.GetPropertyModifications(srcObject);

            foreach (var mod in mods)
            {
                var target = mod.target as MonoBehaviour;

                if (target == null)
                {
                    //UnityEngine.Debug.Log($"target {mod.target.GetType()} is null");
                    continue;
                }

                Transform parent = target.transform.parent;

                while (parent)
                {
                    if (parent.name == srcObject.transform.name)
                    {
                        var newTarget = dstObject.GetComponentInChildren(mod.target.GetType());

                        if (!newTarget)
                            break;

                        var sourceTarget = srcObject.GetComponentInChildren(mod.target.GetType());

                        if (!sourceTarget)
                            break;

                        var sourceSo = new SerializedObject(sourceTarget);
                        sourceSo.UpdateIfRequiredOrScript();

                        var sourceProp = sourceSo.FindProperty(mod.propertyPath);

                        if (sourceProp == null)
                        {
                            //UnityEngine.Debug.Log($"ConfigRoot. Overriden property not found. PropertyPath: '{mod.propertyPath}'");
                            break;
                        }

                        var targetGo = new SerializedObject(newTarget);
                        targetGo.UpdateIfRequiredOrScript();

                        var currentTargetProp = targetGo.FindProperty(mod.propertyPath);

#if UNITY_2022_1_OR_NEWER
                        if (currentTargetProp != null)
                        {
                            if (currentTargetProp.boxedValue != sourceProp.boxedValue)
                                currentTargetProp.boxedValue = sourceProp.boxedValue;
                        }
#else

                        UnityEngine.Debug.Log("Copying not implemented");
#endif

                        targetGo.ApplyModifiedPropertiesWithoutUndo();
                        targetGo.Update();

                        EditorSaver.SetObjectDirty(newTarget);
                        break;
                    }
                    else
                    {
                        parent = parent.parent;
                    }
                }
            }

            EditorSaver.SetObjectDirty(dstObject);
        }

        public static GameObject SaveAsPrefabAsset(GameObject instanceRoot, string assetPath)
        {
            return SaveAsPrefabAsset(instanceRoot, assetPath, out var success);
        }

        public static GameObject SaveAsPrefabAsset(GameObject instanceRoot, string assetPath, out bool success)
        {
            success = false;

            if (AssetDatabaseExtension.CheckForExistAsset(assetPath, false))
            {
                return PrefabUtility.SaveAsPrefabAsset(instanceRoot, assetPath, out success);
            }
            else
            {
                return null;
            }
        }

        public static GameObject FindPrefabByName(string name)
        {
            var searchName = SceneNameToPrefabName(name);
            var pathSearchName = searchName + ".prefab";

            var prefabGuids = AssetDatabase.FindAssets($"{searchName} t:Prefab");

            for (int i = 0; i < prefabGuids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);

                if (path.Contains(pathSearchName))
                {
                    return AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
            }

            return null;
        }

        public static string SceneNameToPrefabName(string searchName)
        {
            if (searchName.Contains("(Clone)"))
            {
                searchName = searchName.Replace("(Clone)", string.Empty);
            }

            var index = searchName.IndexOf(" (");

            if (index > 0)
            {
                searchName = searchName.Substring(0, index);
            }

            return searchName.Trim();
        }

        public static GameObject InstantiatePrefab(UnityEngine.Object prefab, bool unpackRoot = false, Transform parent = null)
        {
            return InstantiatePrefab(prefab as GameObject, unpackRoot, parent);
        }

        public static T InstantiatePrefab<T>(GameObject prefab, bool unpackRoot = false, Transform parent = null) where T : MonoBehaviour
        {
            return InstantiatePrefab(prefab, unpackRoot, parent).GetComponent<T>();
        }

        public static GameObject InstantiatePrefab(GameObject prefab, bool unpackRoot = false, Transform parent = null)
        {
            GameObject createdObject = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;

            if (unpackRoot)
            {
                PrefabUtility.UnpackPrefabInstance(createdObject, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            }

            return createdObject;
        }
    }
}
#endif