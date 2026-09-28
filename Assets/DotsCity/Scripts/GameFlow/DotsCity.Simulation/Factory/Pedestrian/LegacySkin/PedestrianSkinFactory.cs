using Spirit604.Attributes;
using Spirit604.CityEditor;
using Spirit604.DotsCity.Simulation.Pedestrian;
using Spirit604.DotsCity.Simulation.Pedestrian.Authoring;
using Spirit604.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Factory.Pedestrian
{
    public class PedestrianSkinFactory : MonoBehaviourBase, IPedestrianRagdollPrefabProvider, IPedestrianSkinInfoProvider
    {
        #region Consts

        private const string DefaultSavePath = "Prefabs/Gameflow/Pedestrians";

        #endregion

        #region Helper types

        [System.Serializable]
        public class PrefabData
        {
            public GameObject Prefab;
            public SourceFileType FileType;

            public List<SkinnedMeshRenderer> skins { get; set; } = new List<SkinnedMeshRenderer>();
        }

        public enum SourceFileType { ExistPrefab, FBX }

        public enum PrefabCreationType { OverrideExist, CreateNew, CreateNewNested }

        public enum TemplateType { PrefabName, CustomTemplate }

        #endregion

        #region Serialized variables

#pragma warning disable 0414

        [SerializeField] private string prefabSavePath;

        [Expandable]
        [SerializeField] private PedestrianSkinFactoryData pedestrianSkinFactoryData;

        [SerializeField] private bool showAddNewPrefabSettings;

        [SerializeField] private bool addPedestrianComponents = true;

        [SerializeField] private bool checkForAddedPrefabs = true;

        [SerializeField] private bool allowOverwriteExistPrefab;

        [SerializeField] private PrefabCreationType prefabCreationType;

        [OnValueChanged(nameof(UpdatePrefabNames))]
        [SerializeField] private TemplateType templateType;

        [OnValueChanged(nameof(UpdatePrefabNames))]
        [SerializeField] private string templateName = "Pedestrian";

        [OnValueChanged(nameof(UpdatePrefabNames))]
        [SerializeField][Range(0, 20)] private int startNameIndex = 1;

        [OnValueChanged(nameof(UpdatePrefabNames))]
        [SerializeField] private List<PrefabData> newPrefabs = new List<PrefabData>();

        [SerializeField] private List<string> prefabNames = new List<string>();

#pragma warning restore 0414

        #endregion

        #region Variables

        private Dictionary<string, ObjectPool> pedestrianPools = new Dictionary<string, ObjectPool>();
        private Dictionary<int, string> indexSkinToStringData = new Dictionary<int, string>();

        #endregion

        #region Public properties

        public int SkinCount => pedestrianSkinFactoryData?.RagdollPrefabDictionary?.Keys?.Count ?? 0;

        public bool CanCreatePrefabsn => newPrefabs.Count > 0;

#if UNITY_EDITOR
        public PedestrianSkinFactoryData PedestrianSkinFactoryData { get => pedestrianSkinFactoryData; set => pedestrianSkinFactoryData = value; }
#endif


        #endregion

        #region Private properties

        private bool ShowSettings => showAddNewPrefabSettings;

        private bool ShowTemplateName => templateType == TemplateType.CustomTemplate;

        #endregion

        #region Constructor

        [InjectWrapper]
        public void Construct(PedestrianSpawnerConfigHolder pedestrianSpawnerConfigHolder)
        {
            InitPool(pedestrianSpawnerConfigHolder);
        }

        #endregion

        #region Public methods

        public GameObject SpawnSkin(int skinIndex)
        {
            if (indexSkinToStringData.TryGetValue(skinIndex, out var skinName))
            {
                return SpawnSkin(skinName);
            }

            return null;
        }

        public GameObject SpawnSkin(string skinName)
        {
            if (pedestrianPools.TryGetValue(skinName, out var pedestrianPool))
            {
                return pedestrianPool.Pop();
            }

            return null;
        }

        public List<PedestrianRagdoll> GetPrefabs()
        {
            List<PedestrianRagdoll> prefabs = new List<PedestrianRagdoll>();

            var data = pedestrianSkinFactoryData.RagdollPrefabDictionary;

            foreach (var pedestrianPrefab in data)
            {
                prefabs.Add(pedestrianPrefab.Value.Ragdoll);
            }

            return prefabs;
        }

        public PedestrianSkinFactoryData.PedestrianData GetEntry(int skinIndex)
        {
            if (!pedestrianSkinFactoryData)
            {
                return null;
            }

            return pedestrianSkinFactoryData.GetEntry(skinIndex);
        }

        #endregion

        #region Private methods

        private void InitPool(PedestrianSpawnerConfigHolder pedestrianSpawnerConfigHolder)
        {
            var pedestrianSettingsConfig = pedestrianSpawnerConfigHolder.PedestrianSettingsConfig;

            if (!pedestrianSkinFactoryData)
                return;

            int skinIndex = 0;
            var data = pedestrianSkinFactoryData.RagdollPrefabDictionary;

            if (pedestrianSettingsConfig.HybridSkin)
            {
                var poolSize = pedestrianSpawnerConfigHolder.PedestrianSpawnerConfig.PoolSize;

                foreach (var pedestrianPrefab in data)
                {
                    var skinPrefab = pedestrianPrefab.Value.Skin;

                    if (skinPrefab)
                    {
                        ObjectPool pedestrianPool = PoolManager.Instance.PoolForObject(skinPrefab);
                        pedestrianPool.preInstantiateCount = poolSize;
                        pedestrianPools.Add(pedestrianPrefab.Key, pedestrianPool);
                        indexSkinToStringData.Add(skinIndex, pedestrianPrefab.Key);
                        skinIndex++;
                    }
                    else
                    {
                        Debug.Log($"Skin Prefab '{pedestrianPrefab.Key}' is null");
                    }
                }
            }
        }

        private PrefabCreationType GetPrefabCreationType(PrefabData prefabData)
        {
            if (prefabData.FileType == SourceFileType.FBX)
            {
                return PrefabCreationType.CreateNew;
            }

            return prefabCreationType;
        }

        #endregion

        #region Editor methods

        [Button]
        public void UpdatePrefabNames()
        {
            prefabNames.Clear();

            int index = startNameIndex;

            for (int i = 0; i < newPrefabs.Count; i++)
            {
                GameObject sourcePrefab = newPrefabs[i].Prefab;

                if (sourcePrefab == null)
                {
                    continue;
                }

                GameObject prefab = null;

#if UNITY_EDITOR
                if (UnityEditor.PrefabUtility.IsPartOfAnyPrefab(sourcePrefab))
                {
                    if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(sourcePrefab))
                    {
                        prefab = newPrefabs[i].Prefab;
                    }
                    else
                    {
                        prefab = UnityEditor.PrefabUtility.GetNearestPrefabInstanceRoot(newPrefabs[i].Prefab);
                    }
                }
                else
                {
                    prefab = sourcePrefab;
                }
#endif

                if (prefab != null)
                {
                    switch (newPrefabs[i].FileType)
                    {
                        case SourceFileType.ExistPrefab:
                            {
                                string name = GetPrefabName(index, prefab);
                                prefabNames.Add(name);
                                index++;

                                break;
                            }
                        case SourceFileType.FBX:
                            {
                                foreach (var skin in newPrefabs[i].skins)
                                {
                                    string name = GetPrefabName(index, skin.gameObject);
                                    prefabNames.Add(name);
                                    index++;
                                }

                                break;
                            }
                    }
                }

            }
        }

        [Button]
        public void TryToAddPrefabs()
        {
            if (newPrefabs.Count != prefabNames.Count)
            {
                UpdatePrefabNames();
            }

            if (pedestrianSkinFactoryData == null)
            {
                Debug.Log("Factory data not found");
                return;
            }

#if UNITY_EDITOR

            for (int i = 0; i < newPrefabs?.Count; i++)
            {
                var newPrefabData = newPrefabs[i];

                if (newPrefabData == null || newPrefabData.Prefab == null)
                {
                    continue;
                }

                var creationType = GetPrefabCreationType(newPrefabs[i]);
                GameObject prefab = null;

                int sourcePrefabCount = newPrefabData.FileType == SourceFileType.ExistPrefab ? 1 : newPrefabData.skins.Count;

                bool saveAssets = false;

                for (int skinIndex = 0; skinIndex < sourcePrefabCount; skinIndex++)
                {
                    string prefabName = GetPrefabName(newPrefabData, skinIndex);

                    if (string.IsNullOrEmpty(prefabName))
                    {
                        Debug.Log($"Empty prefab name! Prefab {newPrefabData.Prefab.name}");
                        continue;
                    }
                    else
                    {
                        if (pedestrianSkinFactoryData.HasEntry(prefabName))
                        {
                            if (!allowOverwriteExistPrefab)
                            {
                                Debug.Log($"Duplicate key '{prefabName}'");
                                continue;
                            }
                            else
                            {
                                pedestrianSkinFactoryData.TryToRemoveEntry(prefabName);
                            }
                        }
                    }

                    bool prefabIsCreated = true;

                    switch (creationType)
                    {
                        case PrefabCreationType.OverrideExist:
                            {
                                prefab = GetPrefab(newPrefabs[i].Prefab);
                                break;
                            }
                        case PrefabCreationType.CreateNew:
                            {
                                string savePath = GetAssetSavePath(prefabName);

                                var sourcePrefab = Instantiate(newPrefabData.Prefab);
                                sourcePrefab.name = prefabName;

                                if (newPrefabData.FileType == SourceFileType.FBX)
                                {
                                    var skin = newPrefabData.skins[skinIndex];

                                    var targetSkin = sourcePrefab.GetComponentsInChildren<SkinnedMeshRenderer>().Where(a => a.sharedMesh == skin.sharedMesh).FirstOrDefault();

                                    var deleteSkins = sourcePrefab.GetComponentsInChildren<SkinnedMeshRenderer>().Where(a => a != targetSkin).ToArray();

                                    foreach (var deleteSkin in deleteSkins)
                                    {
                                        DestroyImmediate(deleteSkin.gameObject);
                                    }
                                }

                                prefab = PrefabExtension.SaveAsPrefabAsset(sourcePrefab, savePath, out prefabIsCreated);

                                DestroyImmediate(sourcePrefab);

                                if (!prefab)
                                {
                                    continue;
                                }

                                saveAssets = true;
                                break;
                            }
                        case PrefabCreationType.CreateNewNested:
                            {
                                prefab = GetPrefab(newPrefabData.Prefab);
                                var savePath = GetAssetSavePath(prefabName);

                                if (!AssetDatabaseExtension.CheckForExistAsset(savePath, allowOverwriteExistPrefab, true))
                                {
                                    continue;
                                }

                                prefab = UnityEditor.PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                                prefab.name = prefabName;

                                prefab = PrefabExtension.SaveAsPrefabAsset(prefab, savePath, out prefabIsCreated);

                                if (!prefab)
                                {
                                    continue;
                                }

                                saveAssets = true;
                                break;
                            }
                    }

                    if (prefab == null || !prefabIsCreated)
                        continue;

                    if (prefab.TryGetComponent<Animator>(out var animator))
                    {
                        animator.applyRootMotion = false;
                    }

                    var hasDuplicate = HasDuplicate(prefab, out var addedKey);

                    if (!hasDuplicate)
                    {
                        if (addPedestrianComponents)
                        {
                            if (prefab.GetComponent<PedestrianEntityRef>() == null)
                            {
                                prefab.AddComponent<PedestrianEntityRef>();
                                EditorSaver.SetObjectDirty(prefab);
                            }
                        }

                        if (!pedestrianSkinFactoryData.TryToAddEntry(prefab, prefabName))
                        {
                            Debug.Log($"Duplicate dictionary key '{prefabName}' Prefab {prefab.name}");
                        }
                    }
                    else
                    {
                        Debug.Log($"Key '{addedKey}'. Skin '{prefab.name}' - Already added!");
                    }
                }

                if (saveAssets)
                {
                    UnityEditor.AssetDatabase.SaveAssets();
                }
            }

            newPrefabs.Clear();
            prefabNames.Clear();

            UnityEditor.Selection.activeObject = null;
            UnityEditor.Selection.activeObject = this;
#endif
        }

        public void RemoveEntry(int index)
        {
            if (newPrefabs.Count > index && index >= 0)
            {
                newPrefabs.RemoveAt(index);
                UpdatePrefabNames();
                EditorSaver.SetObjectDirty(this);
            }
        }

        public void AddEntry()
        {
            newPrefabs.Add(new PrefabData());
            EditorSaver.SetObjectDirty(this);
        }

        public PrefabData GetPrefabData(int index)
        {
            return newPrefabs[index];
        }

        public void UpdatePrefabData(int index)
        {
            var prefabData = GetPrefabData(index);
            UpdatePrefabData(prefabData);
        }

        public void UpdatePrefabData(PrefabData prefabData, bool includePrefabNames = true)
        {
            if (prefabData == null)
            {
                return;
            }

            prefabData.skins.Clear();

            if (prefabData.Prefab != null)
            {
                if (prefabData.FileType == SourceFileType.FBX)
                {
                    prefabData.skins = prefabData.Prefab.GetComponentsInChildren<SkinnedMeshRenderer>().ToList();
                }
            }

            if (includePrefabNames)
            {
                UpdatePrefabNames();
                EditorSaver.SetObjectDirty(this);
            }
        }

#if UNITY_EDITOR
        public PrefabData AddNewPrefab(GameObject newEntry)
        {
            if (!newEntry)
            {
                return null;
            }

            var fileType = SourceFileType.ExistPrefab;

            if (UnityEditor.PrefabUtility.IsPartOfAnyPrefab(newEntry))
            {
                var prefab = GetPrefab(newEntry);

                if (prefab)
                {
                    newEntry = prefab;

                    var assetPath = UnityEditor.AssetDatabase.GetAssetPath(prefab);

                    if (assetPath.Contains(".fbx"))
                    {
                        fileType = SourceFileType.FBX;
                    }
                }
            }

            var prefabData = new PrefabData()
            {
                Prefab = newEntry,
                FileType = fileType
            };

            newPrefabs.Add(prefabData);
            UpdatePrefabData(prefabData);

            return prefabData;
        }
#endif

        private int GetNameIndex(PrefabData prefabData, int localIndex)
        {
            int index = 0;

            for (int i = 0; i < newPrefabs.Count; i++)
            {
                var currentPrefabData = newPrefabs[i];

                if (currentPrefabData == prefabData)
                {
                    if (currentPrefabData.FileType == SourceFileType.ExistPrefab)
                    {
                        return index;
                    }
                    else
                    {
                        return index + localIndex;
                    }
                }
                else
                {
                    if (currentPrefabData.FileType == SourceFileType.ExistPrefab)
                    {
                        index++;
                    }
                    else
                    {
                        index += currentPrefabData.skins.Count;
                    }
                }
            }

            return index;
        }

        private int GetEntryCount()
        {
            int count = 0;

            for (int i = 0; i < newPrefabs.Count; i++)
            {
                var currentPrefabData = newPrefabs[i];

                if (currentPrefabData.FileType == SourceFileType.ExistPrefab)
                {
                    count++;
                }
                else
                {
                    count += currentPrefabData.skins.Count;
                }
            }

            return count;
        }

        private string GetPrefabName(PrefabData prefabData, int localIndex)
        {
            var index = GetNameIndex(prefabData, localIndex);

            if (prefabNames.Count > index)
            {
                return prefabNames[index];
            }

            return string.Empty;
        }

#if UNITY_EDITOR
        private string GetAssetSavePath(string prefabName)
        {
            return AssetDatabaseExtension.GetSavePath(prefabSavePath, prefabName, typeof(GameObject));
        }

        private GameObject GetPrefab(GameObject newEntry)
        {
            GameObject prefab;

            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(newEntry))
            {
                prefab = newEntry;
            }
            else
            {
                prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(newEntry);
            }

            return prefab;
        }
#endif

        private string GetPrefabName(int index, GameObject prefab)
        {
            string name = "";

            switch (templateType)
            {
                case TemplateType.PrefabName:
                    name = prefab.name;
                    break;
                case TemplateType.CustomTemplate:
                    name = $"{templateName}{index}";
                    break;
            }

            return name;
        }

        private bool HasDuplicate(GameObject sourcePrefab, out string key)
        {
            key = string.Empty;

            if (!checkForAddedPrefabs)
            {
                return false;
            }

            return pedestrianSkinFactoryData.HasDuplicateSkin(sourcePrefab, out key);
        }

        private void OnValidate()
        {
            if (GetEntryCount() != prefabNames.Count)
            {
                UpdatePrefabNames();
            }
        }

        #endregion

        #region Editor inspector methods

        public void OnInspectorEnabled()
        {
            var pathExist = !string.IsNullOrEmpty(prefabSavePath);

            if (!pathExist)
            {
                prefabSavePath = CityEditorBookmarks.GetPath(DefaultSavePath);
            }

            int index = 0;

            while (newPrefabs.Count > index)
            {
                if (newPrefabs[index].Prefab != null)
                {
                    UpdatePrefabData(newPrefabs[index], false);
                    index++;
                }
                else
                {
                    newPrefabs.RemoveAt(index);
                }
            }

            OnValidate();
        }

#if UNITY_EDITOR
        public void SelectSavePath()
        {
            var newSavePath = AssetDatabaseExtension.SelectProjectFolderFromDialogWindow("Select new save path", prefabSavePath);

            if (!string.IsNullOrEmpty(newSavePath))
            {
                prefabSavePath = newSavePath;
                EditorSaver.SetObjectDirty(this);
            }
        }

        public void AddObjects(UnityEngine.Object[] objs)
        {
            if (objs == null)
                return;

            foreach (var obj in objs)
            {
                var go = obj as GameObject;

                if (!go)
                    continue;

                AddNewPrefab(go);
            }
        }
#endif
        #endregion
    }
}