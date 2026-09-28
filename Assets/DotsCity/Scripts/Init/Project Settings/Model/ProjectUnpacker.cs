using Spirit604.Attributes;
using Spirit604.CityEditor;
using Spirit604.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

#if DOTS_CITY
using Spirit604.DotsCity.Core;
#endif

namespace Spirit604.PackageManagerExtension
{
    [CreateAssetMenu(menuName = "Spirit604/ProjectUnpacker")]
    public class ProjectUnpacker : ScriptableObjectBase
    {
        public const string InternalPath = "Prefabs/Init/Internal/";
        private const string PackagesPath = InternalPath + "Packages/";

        [Serializable]
        public class GroupData
        {
            public Object Pack;
            public string Name;
            public List<string> Paths = new List<string>();
        }

        [Serializable]
        public class ClonablePrefabData
        {
            public GameObject SourcePrefab;
            public string TargetPath;
            public string Guid;
        }

        [Serializable]
        public class GroupData2
        {
            public string Name;
            public List<Object> objects = new List<Object>();
        }

        public List<GroupData> groupDatas = new List<GroupData>();
        public List<ClonablePrefabData> prefabs = new List<ClonablePrefabData>();

        [HideInInspector]
        public List<GroupData2> sourceGroups = new List<GroupData2>();

        [HideInInspector]
        public List<GameObject> singlePrefabs = new List<GameObject>();

        [HideInInspector]
        public List<GameObject> sourcePrefabs = new List<GameObject>();

#if !DOTS_CITY_DEV
        [HideInInspector]
#endif
        public bool deleteAssets;

#if UNITY_EDITOR

        public static ProjectUnpacker GetUnpacker()
        {
            return AssetDatabase.LoadAssetAtPath<ProjectUnpacker>(CityEditorBookmarks.GetPath("Prefabs/Init/Project Unpacker.asset"));
        }

#if DOTS_CITY_DEV
        [Button]
#endif
        public void Clone()
        {
            foreach (var group in sourceGroups)
            {
                var groupData = new ProjectUnpacker.GroupData()
                {
                    Name = group.Name,
                };

                foreach (var asset in group.objects)
                {
                    var path = GetRelativePath(AssetDatabase.GetAssetPath(asset));

                    groupData.Paths.Add(path);
                }

                groupDatas.Add(groupData);
            }

            foreach (var prefab in singlePrefabs)
            {
                var groupData = new ProjectUnpacker.GroupData()
                {
                    Name = prefab.name
                };

                var path = GetRelativePath(AssetDatabase.GetAssetPath(prefab));

                groupData.Paths.Add(path);

                groupDatas.Add(groupData);
            }

            EditorSaver.SetObjectDirty(this);

            //return;
            //foreach (var prefab in sourcePrefabs)
            //{
            //    var path = GetRelativePath(AssetDatabase.GetAssetPath(prefab));

            //    prefabs.Add(new ProjectUnpacker.ClonablePrefabData()
            //    {
            //        TargetPath = path,
            //    });
            //}

            //EditorSaver.SetObjectDirty(this);
        }

#if DOTS_CITY_DEV
        [Button]
#endif
        public void ExportPackages()
        {
            foreach (var group in groupDatas)
            {
                var assetPaths = new string[group.Paths.Count];

                for (var i = 0; i < group.Paths.Count; i++)
                {
                    assetPaths[i] = $"{GetPath(group.Paths[i])}";
                }

                var packagePath = GetUnityPackagePath(group);
                AssetDatabase.ExportPackage(assetPaths, packagePath, ExportPackageOptions.Default);
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

                var pack = AssetDatabase.LoadAssetAtPath(packagePath, typeof(Object));
                group.Pack = pack;

                if (deleteAssets)
                {
                    for (var i = 0; i < assetPaths.Length; i++)
                    {
                        if (AssetDatabase.LoadAssetAtPath(assetPaths[i], typeof(Object)))
                            AssetDatabase.DeleteAsset(assetPaths[i]);
                    }
                }
            }

            if (deleteAssets)
            {
                foreach (var prefab in prefabs)
                {
                    var path = $"{GetPath(prefab.TargetPath)}";

                    if (AssetDatabase.LoadAssetAtPath(path, typeof(Object)))
                        AssetDatabase.DeleteAsset(path);
                }
            }

            EditorSaver.SetObjectDirty(this);
        }

        [Button]
        public void TryToImportPackages()
        {
            foreach (var group in groupDatas)
            {
                if (group.Pack == null || group.Paths.Count == 0)
                    continue;

                var targetPath = GetPath(group.Paths[0]);
                var asset = AssetDatabase.LoadAssetAtPath(targetPath, typeof(Object));

                if (asset == null)
                {
                    var packagePath = AssetDatabase.GetAssetPath(group.Pack);

                    AssetDatabase.ImportPackage(packagePath, false);
                }
            }

            TryToImportPrefabs();
        }

        [Button]
        public void TryToImportPrefabs()
        {
            foreach (var prefab in prefabs)
            {
                var sourcePath = AssetDatabase.GetAssetPath(prefab.SourcePrefab);

                var targetPath = GetPath(prefab.TargetPath);
                var asset = AssetDatabase.LoadAssetAtPath(targetPath, typeof(Object));
                var prevPath = targetPath;

                if (asset == null)
                {
                    try
                    {
                        targetPath = AssetDatabase.GUIDToAssetPath(prefab.Guid);

                        if (!string.IsNullOrEmpty(targetPath))
                            asset = AssetDatabase.LoadAssetAtPath(targetPath, typeof(Object));
                    }
                    catch { }
                    finally
                    {
                        if (asset == null)
                        {
                            targetPath = prevPath;
                        }
                    }
                }

                if (asset == null)
                {
                    var dir = Path.GetDirectoryName(targetPath);

                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                    }

                    AssetDatabase.CopyAsset(sourcePath, targetPath);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                    ChangeGuid(targetPath, prefab.Guid);
                }
                else
                {
#if DOTS_CITY
                    var sourceProvider = prefab.SourcePrefab.GetComponent<IVersionProvider>();
                    var existAsset = asset as GameObject;

                    var existProvider = existAsset.GetComponent<IVersionProvider>();

                    if (existProvider != null && sourceProvider.Version == existProvider.Version)
                        continue;

                    string fileName = Path.GetFileName(targetPath);

                    var backupPath = GetBackupPath(targetPath, fileName);

                    if (AssetDatabase.LoadAssetAtPath(backupPath, typeof(Object)))
                    {
                        AssetDatabase.DeleteAsset(backupPath);
                    }

                    AssetDatabase.CopyAsset(targetPath, backupPath);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

                    AssetDatabase.DeleteAsset(targetPath);

                    AssetDatabase.CopyAsset(sourcePath, targetPath);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                    ChangeGuid(targetPath, prefab.Guid);

                    var clonedAsset = AssetDatabase.LoadAssetAtPath(targetPath, typeof(Object)) as GameObject;
                    var oldAsset = AssetDatabase.LoadAssetAtPath(backupPath, typeof(Object)) as GameObject;

                    var components = oldAsset.GetComponentsInChildren<MonoBehaviour>().Where(a => PrefabUtility.IsPartOfPrefabAsset(a) && PrefabUtility.GetOutermostPrefabInstanceRoot(a) == null).ToList();

                    foreach (var sourceComponent in components)
                    {
                        SerializedObject sourceSerializedObject = new SerializedObject(sourceComponent);

                        var type = sourceComponent.GetType();
                        var targetComponent = clonedAsset.GetComponentInChildren(type);

                        SerializedObject targetSerializedObject = new SerializedObject(targetComponent);

                        // Iterate through all properties and copy them from source to target
                        SerializedProperty property = sourceSerializedObject.GetIterator();
                        while (property.NextVisible(true))
                        {
                            bool copy = true;

                            if (property.propertyType == SerializedPropertyType.ObjectReference)
                            {
                                if (property.objectReferenceValue is not ScriptableObject &&
                                    property.objectReferenceValue is not Mesh &&
                                    property.objectReferenceValue is not Material)
                                {
                                    bool isPrefab = false;

                                    if (property.objectReferenceValue != null)
                                        isPrefab = PrefabUtility.GetPrefabAssetType(property.objectReferenceValue) != PrefabAssetType.NotAPrefab;

                                    if (isPrefab)
                                    {
                                        var path = AssetDatabase.GetAssetPath(property.objectReferenceValue);
                                        isPrefab = !string.IsNullOrEmpty(path) && !path.Contains("[BACKUP]");
                                    }

                                    if (!isPrefab)
                                        copy = false;
                                }
                            }
                            else if (property.propertyType == SerializedPropertyType.Generic)
                            {
                                if (!property.isArray || (property.arrayElementType != "PPtr<$ScriptableObject>" && property.arrayElementType != "string" && property.name != "values" && property.name != "keys"))
                                {
                                    copy = false;
                                }
                            }
                            else if (property.propertyType == SerializedPropertyType.ArraySize)
                            {
                                copy = false;
                            }

                            if (copy)
                            {
                                targetSerializedObject.CopyFromSerializedProperty(property);
                            }
                        }

                        targetSerializedObject.ApplyModifiedPropertiesWithoutUndo();
                    }

                    if (AssetDatabase.LoadAssetAtPath(backupPath, typeof(Object)))
                    {
                        AssetDatabase.DeleteAsset(backupPath);
                    }
#endif
                }
            }
        }

        private string GetPath(string path) => CityEditorBookmarks.GetPath(path);

        private string GetRelativePath(string path)
        {
            return path.Replace("Assets/DotsCity/", "");
        }

        private string GetBackupPath(string originalFilePath, string newFileName)
        {
            string directoryPath = Path.GetDirectoryName(originalFilePath);

            // Combine the directory path with the new file name to get the new file path
            string newFilePath = Path.Combine(directoryPath, newFileName);

            return newFilePath + " [BACKUP].prefab";
        }

        private void ChangeGuid(string assetPath, string newGUID)
        {
            string metaFilePath = assetPath + ".meta";
            if (!File.Exists(metaFilePath))
            {
                Debug.LogError("Meta file not found.");
                return;
            }

            // Read the meta file
            string[] metaFileLines = File.ReadAllLines(metaFilePath);

            // Modify the GUID in the meta file
            for (int i = 0; i < metaFileLines.Length; i++)
            {
                if (metaFileLines[i].StartsWith("guid: "))
                {
                    metaFileLines[i] = "guid: " + newGUID;
                    break;
                }
            }

            // Write the modified meta file
            File.WriteAllLines(metaFilePath, metaFileLines);

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            // Refresh the Asset Database
        }

        private string GetUnityPackagePath(GroupData group)
        {
            return $"{GetPath(PackagesPath)}{group.Name}.unitypackage";
        }
#endif
    }
}