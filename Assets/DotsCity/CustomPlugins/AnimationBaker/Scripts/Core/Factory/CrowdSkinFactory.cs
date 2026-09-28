using Spirit604.AnimationBaker.EditorInternal;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Spirit604.AnimationBaker
{
    public class CrowdSkinFactory : MonoBehaviour
    {
        #region Constans


        #endregion

        #region Helper types

        public enum MaterialTemplateType { TemplateOnly, EntryName, MeshName }

        public enum EntryKeySourceType { SelectedMeshName, Custom }

        public enum AnimBindingState { Default, Success, PartialSuccess, Failed }

        [Serializable]
        public class MeshData
        {
            public Mesh Mesh;
            public Material Material;
        }

        #endregion

        #region Serializable values

#pragma warning disable CS0414

        [SerializeField][Range(0, 1f)] private float minAnimationTextMatchRate = 0.75f;

        [SerializeField] private string createMaterialPath = "Assets/";

        [SerializeField] private string createdMaterialName = "NpcAnimationMaterial";

        [SerializeField] private MaterialTemplateType materialTemplateType;

        [SerializeField] private Material animationMaterialBase;

        [SerializeField] private Texture2D defaultAtlasTexture;

        [SerializeField] private EntryKeySourceType entryKeySourceType = EntryKeySourceType.SelectedMeshName;

        [SerializeField] private bool showFrameInfoOnSelectOnly;

        [Tooltip("Automatic data binding during material generation")]
        [SerializeField] private bool autoBindOnGeneration = true;

        [SerializeField] private bool showOptionalAnimationPopup;

        [SerializeField] private AnimationCollectionContainer animationCollectionContainer;

        [SerializeField] private CharacterAnimationContainer characterAnimationContainer;

        [SerializeField] private AnimationTextureDataContainerBase animationTextureDataContainer;

        [SerializeField] private bool showInfo;

        [SerializeField] private string newKey;

        [SerializeField] private SkinnedMeshRenderer sourceSkinnedMeshRenderer;

        [SerializeField] private Mesh mesh;

        [SerializeField] private int selectedSkinIndex = -1;

        [SerializeField] private int selectedAnimationIndex;

        [SerializeField] private int animationCount;

        [SerializeField] private bool findAnimations;

        [SerializeField] private bool displayAddTab;

        [SerializeField] private List<MeshData> availableContainerMeshes = new List<MeshData>();

        [SerializeField] private bool settingFoldout = true;

        [SerializeField] private bool addTemplateName = true;

#pragma warning restore CS0414

        #endregion

        #region Variables

        private List<AnimBindingState> animBindingStates = new List<AnimBindingState>();
        private List<List<MeshData>> data = new List<List<MeshData>>();
        private int selectedLodLevel = 0;

        #endregion

        #region Properties

        public virtual bool HasRagdoll => true;

        public bool HasAnimationCollection => animationCollectionContainer;

        public AnimationCollectionContainer AnimationCollectionContainer { get => animationCollectionContainer; set => animationCollectionContainer = value; }

        public IEnumerable<string> AnimationNameList => animationCollectionContainer != null ? animationCollectionContainer.GetAnimationNames() : null;

        public List<AnimationCollectionContainer.AnimationData> AnimationsData => animationCollectionContainer.GetAnimations();

        public int SelectedAnimationIndex => selectedAnimationIndex;

        public Mesh AssignedMesh => sourceSkinnedMeshRenderer != null ? sourceSkinnedMeshRenderer.sharedMesh : mesh;

        public CharacterAnimationContainer CharacterAnimationContainer { get => characterAnimationContainer; set => characterAnimationContainer = value; }

        public List<AnimationTextureData> RelatedAnimations { get; private set; } = new List<AnimationTextureData>();

        public int SkinCount => characterAnimationContainer?.Count ?? 0;

        public int AnimationCount
        {
            get
            {
                if (HasAnimationCollection)
                {
                    animationCount = animationCollectionContainer.GetAnimationCount(AnimationUseType.Mandatory);
                }

                return animationCount;
            }
            set
            {
                animationCount = value;
            }
        }

        public bool ReadyToCreate
        {
            get
            {
                var currentKey = GetEntryKey();
                return !string.IsNullOrEmpty(currentKey) && !HasKey(currentKey) && AssignedMesh != null;
            }
        }

        public bool HasSelectedIndex => selectedSkinIndex >= 0 && characterAnimationContainer != null && characterAnimationContainer.Count > selectedSkinIndex;

        public bool SourceDataExist => animationTextureDataContainer != null;

        public bool CanFindAnimation => SourceDataExist && HasSelectedIndex && findAnimations;

        public List<MeshData> AvailableContainerMeshes { get => availableContainerMeshes; }

        public int SelectedLodLevel { get => selectedLodLevel; set => selectedLodLevel = value; }

        #endregion

        #region Public methods

        public void CreateFactory()
        {
            if (!HasAnimationContainer())
            {
                return;
            }

            for (int i = 0; i < characterAnimationContainer.Count; i++)
            {
                var skinData = characterAnimationContainer.GetSkinData(i);
                var listMesh = new List<MeshData>();

                const int lodLevelCount = 1;

                for (int lodLevel = 0; lodLevel < lodLevelCount; lodLevel++)
                {
                    var animations = skinData.GetAnimations(lodLevel);

                    for (int j = 0; j < animations.Count; j++)
                    {
                        var animationData = animations[j];

                        var meshData = new MeshData()
                        {
                            Mesh = Instantiate(skinData.GetMesh(lodLevel)),
                            Material = Instantiate(skinData.GetMaterial(lodLevel))
                        };

                        InitMaterial(meshData.Material, animationData);

                        listMesh.Add(meshData);
                    }
                }

                data.Add(listMesh);
            }
        }

        public void InitMaterial(Material material, AnimationData animationData)
        {
            material.SetFloat(Constans.ClipLengthParam, animationData.ClipLength);
            material.SetFloat(Constans.PlaybackTime, -1);
            material.SetFloat(Constans.VertexCountParam, animationData.VertexCount);
            material.SetFloat(Constans.FrameStepInvParam, animationData.FrameStepInv);
            material.SetFloat(Constans.FrameCountParam, animationData.FrameCount);
            material.SetVector(Constans.FrameOffsetParam, animationData.FrameOffset);
            material.SetInt(Constans.InterpolateParam, animationData.InterpolateValue);
            material.SetVector(Constans.TargetFrameOffsetParam, Vector2.one * -1);
        }

        public SkinData GetSkinData(int skinIndex)
        {
            if (characterAnimationContainer)
            {
                return characterAnimationContainer.GetSkinData(skinIndex);
            }

            return null;
        }

        public void GetSkinData(int skinIndex, out Mesh mesh, out Material material)
        {
            mesh = null;
            material = null;

            if (!HasAnimationContainer())
            {
                return;
            }

            characterAnimationContainer.GetSkinData(skinIndex, out mesh, out material);

            if (mesh == null)
            {
                UnityEngine.Debug.Log($"CrowdSkinFactory. Animation skin index '{skinIndex}' mesh or material not found");
            }
        }

        public void GetSkinData(int skinIndex, int animationIndex, out Mesh mesh, out Material material, bool uniqueMesh = false)
        {
            mesh = null;
            material = null;

            if (!HasAnimationContainer())
            {
                return;
            }

            characterAnimationContainer.GetSkinData(skinIndex, out mesh, out material);

            if (mesh)
            {
                if (uniqueMesh)
                {
                    mesh = Instantiate(mesh);
                    material = Instantiate(material);
                }

                return;
            }

            UnityEngine.Debug.Log($"CrowdSkinFactory. Animation index '{skinIndex}' animationIndex '{animationIndex}' skin mesh or material not found");
        }

        public void AddEntry(string key)
        {
            AddEntry(key, AssignedMesh);
        }

        public void AddEntry(string key, Mesh mesh, Material material = null)
        {
            if (!HasAnimationContainer())
            {
                return;
            }

            characterAnimationContainer.AddEntry(key, mesh, AnimationCount);

            var addedEntry = characterAnimationContainer.GetLastSkinData();

            if (material && material.mainTexture)
            {
                addedEntry.TempMainTexture = (Texture2D)material.mainTexture;
            }
            else
            {
                addedEntry.TempMainTexture = defaultAtlasTexture;
            }

            selectedSkinIndex = characterAnimationContainer.Count - 1;
            AssignDefaultAnimations(addedEntry);
            FindAnimations();
        }

        public void RemoveEntry(string key)
        {
            if (!HasAnimationContainer())
            {
                return;
            }

            characterAnimationContainer.RemoveEntry(key);
        }

        public bool HasKey(string key)
        {
            if (!HasAnimationContainer())
            {
                return false;
            }

            return characterAnimationContainer.HasKey(key);
        }

        public void AddEntries()
        {
            var meshes = availableContainerMeshes;

            for (int i = 0; i < meshes.Count; i++)
            {
                if (!ContainsMesh(meshes[i].Mesh))
                {
                    var key = meshes[i].Mesh.name;
                    AddEntry(key, meshes[i].Mesh, meshes[i].Material);
                }
            }
        }

        public void AddKey()
        {
            if (!ReadyToCreate)
            {
                return;
            }

            string currentKey = GetEntryKey();

            AddEntry(currentKey);
            newKey = string.Empty;
            sourceSkinnedMeshRenderer = null;
            mesh = null;
        }

        public void UpdateKey()
        {
            string currentNewKey = string.Empty;

            if (AssignedMesh != null)
            {
                currentNewKey = AssignedMesh.name;
            }

            newKey = currentNewKey;
        }

        public string GetEntryKey()
        {
            string currentKey = string.Empty;

            switch (entryKeySourceType)
            {
                case EntryKeySourceType.Custom:
                    currentKey = newKey;
                    break;
                case EntryKeySourceType.SelectedMeshName:
                    {
                        if (AssignedMesh)
                        {
                            currentKey = AssignedMesh.name;
                        }

                        break;
                    }
            }

            return currentKey;
        }

        public void FindAnimations()
        {
            RelatedAnimations.Clear();

            if (CanFindAnimation)
            {
                GetSkinData(selectedSkinIndex, out var mesh, out var material);

                PerformDataAction(animationTextureDataContainer =>
                {
                    var relatedAnimations = animationTextureDataContainer.TextureDatas.Where(a => a.SourceMesh == mesh);

                    if (relatedAnimations.Any())
                    {
                        RelatedAnimations.AddRange(relatedAnimations);
                    }
                });
            }
        }

        public void SelectAnimation(int newSkinIndex, int newAnimationIndex)
        {
            var previousSkinIndex = selectedSkinIndex;
            selectedSkinIndex = newSkinIndex;
            selectedAnimationIndex = newAnimationIndex;
            OnSkinIndexSelectedChanged(previousSkinIndex, newSkinIndex);
        }

        public void AssignAnimationData(int index)
        {
            AssignAnimationData(selectedAnimationIndex, index, true);
        }

        public void AssignAnimationData(int containerAnimationIndex, int relatedAnimationIndex, bool userSelection = false)
        {
            var relatedAnimation = RelatedAnimations[relatedAnimationIndex];

            int animationHash = GetContainerAnimationHash(containerAnimationIndex);

            characterAnimationContainer.AssignAnimationData(selectedSkinIndex, containerAnimationIndex, animationHash, relatedAnimation);

            if (userSelection)
            {
                SetBindingState(containerAnimationIndex, AnimBindingState.Default);
            }
        }

        public int GetContainerAnimationHash(int animationIndex)
        {
            int animationHash = -1;

            if (animationCollectionContainer != null)
            {
                animationHash = animationCollectionContainer.GetAnimation(animationIndex).Hash;
            }

            return animationHash;
        }

        public int GetSavedAnimationHash(int skinIndex, int animationIndex, int lodLevel = 0)
        {
            var skinData = characterAnimationContainer.GetSkinData(skinIndex);
            var animations = skinData.GetAnimations(lodLevel);

            return animations[animationIndex].AnimationHash;
        }

        public string GetAnimationNameByIndex(int index)
        {
            var animData = animationCollectionContainer.GetAnimation(index);

            if (animData != null)
            {
                return animData.Name;
            }

            return string.Empty;
        }

        public AnimationCollectionContainer.AnimationData GetContainerAnimation(string guid) => animationCollectionContainer.GetAnimation(guid);

        public bool AnimationIsSelected(int skinIndex, int animationIndex) => SkinIsSelected(skinIndex) && selectedAnimationIndex == animationIndex;

        public bool SkinIsSelected(int skinIndex) => selectedSkinIndex == skinIndex;

        public int GetAnimationCount(int skinIndex, int lodLevel = 0) => characterAnimationContainer.GetSkinData(skinIndex).GetAnimationCount(lodLevel);

        public void ValidateAnimations(int lodLevel = 0)
        {
            if (animationCollectionContainer == null || characterAnimationContainer == null || characterAnimationContainer.Count == 0)
            {
                return;
            }

            var makeDirty = false;

            characterAnimationContainer.Validate();

            var animations = animationCollectionContainer.GetAnimations();

            for (int skinIndex = 0; skinIndex < characterAnimationContainer.Count; skinIndex++)
            {
                bool changed = false;

                var skinData = characterAnimationContainer.GetSkinData(skinIndex);
                var skinAnimations = skinData.GetAnimations(lodLevel);

                for (int i = 0; i < skinAnimations.Count; i++)
                {
                    if (i < animations.Count)
                    {
                        var sourceAnimData = animations[i];

                        if (skinAnimations[i].Guid == sourceAnimData.Guid)
                        {
                            continue;
                        }

                        changed = true;
                        break;
                    }
                    else
                    {
                        changed = true;
                        break;
                    }
                }

                if (changed)
                {
                    makeDirty = true;
                    List<AnimationData> animationDatas = new List<AnimationData>();
                    List<AnimationData> animationDatas2 = new List<AnimationData>();

                    for (int i = 0; i < animations.Count; i++)
                    {
                        var anim = animations[i];

                        var oldAnim = skinData.GetAnimationData(anim.Guid);

                        if (oldAnim == null && anim.AnimationType == AnimationUseType.Mandatory)
                        {
                            oldAnim = new AnimationData();
                            BindAnimation(oldAnim, anim);
                        }

                        if (oldAnim != null)
                        {
                            var list = anim.AnimationType == AnimationUseType.Mandatory ? animationDatas : animationDatas2;
                            list.Add(oldAnim);
                        }
                    }

                    animationDatas.AddRange(animationDatas2);

                    skinData.ClearAnimations();

                    foreach (var anim in animationDatas)
                    {
                        skinData.AddAnimationData(anim);
                    }
                }
            }

            if (makeDirty)
            {
                EditorSaver.SetObjectDirty(characterAnimationContainer);
            }
        }

        public bool CurrentSheetContainsMesh(int index, int lodLevel = 0)
        {
            if (SourceDataExist && characterAnimationContainer)
            {
                var skinData = characterAnimationContainer.GetSkinData(index);

                if (skinData != null && skinData.GetMesh(lodLevel))
                {
                    bool contains = false;

                    PerformDataAction(animationTextureDataContainer =>
                    {
                        var containsLocal = animationTextureDataContainer.ContainsMesh(skinData.GetMesh(lodLevel));

                        if (!contains)
                        {
                            contains = containsLocal;
                        }
                    });

                    return contains;
                }
            }

            return false;
        }

        public void GenerateMaterial(int index, Texture mainTexture, Texture animationTexture, Texture normalTexture, int lodLevel = 0)
        {
            if (animationMaterialBase == null)
            {
                throw new NullReferenceException("CrowdSkinFactory. Base material is null");
            }

#if UNITY_EDITOR

            var materialCopy = Instantiate(animationMaterialBase);
            materialCopy.SetTexture(Constans.MainTexture, mainTexture);
            materialCopy.SetTexture(Constans.AnimationTexture, animationTexture);
            materialCopy.SetTexture(Constans.NormalTexture, normalTexture);

            characterAnimationContainer.SetMaterial(index, materialCopy);

            string materialName = createdMaterialName;

            switch (materialTemplateType)
            {
                case MaterialTemplateType.TemplateOnly:
                    break;
                case MaterialTemplateType.EntryName:
                    {
                        materialName = characterAnimationContainer.GetKey(index);

                        if (addTemplateName)
                        {
                            materialName = $"{createdMaterialName}{materialName}";
                        }

                        break;
                    }
                case MaterialTemplateType.MeshName:
                    {
                        materialName = characterAnimationContainer.GetSkinData(index).GetMesh(lodLevel).name;

                        if (addTemplateName)
                        {
                            materialName = $"{createdMaterialName}{materialName}";
                        }

                        break;
                    }
            }

            var createPath = createMaterialPath + $"{materialName}.mat";
            var uniquePath = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(createPath);

            UnityEditor.AssetDatabase.CreateAsset(materialCopy, uniquePath);
            UnityEditor.AssetDatabase.Refresh();

            if (autoBindOnGeneration)
            {
                SelectAnimation(index, 0);
                AutoBindAnimation(true);
            }
#endif
        }

        public void SetMaterialGenerationPath(string newPath)
        {
            if (string.IsNullOrEmpty(newPath))
            {
                return;
            }

            if (createMaterialPath != newPath)
            {
                createMaterialPath = newPath;
                EditorSaver.SetObjectDirty(this);
            }
        }

        public AnimBindingState GetBindingState(int index)
        {
            if (animBindingStates.Count > index)
            {
                return animBindingStates[index];
            }

            return AnimBindingState.Default;
        }

        public void SetBindingState(int index, AnimBindingState state)
        {
            if (animBindingStates.Count > index)
            {
                animBindingStates[index] = state;
            }
        }

        public void AutoBindAnimation(bool force = false, int lodLevel = 0)
        {
            animBindingStates.Clear();

            if (!HasSelectedIndex)
            {
                return;
            }

            var skinData = characterAnimationContainer.GetSkinData(selectedSkinIndex);

            if (skinData == null)
            {
                return;
            }

            var animations = animationCollectionContainer.GetAnimations();
            var sourceAnimations = animations.Select(a => a.Name.Replace(" ", "").ToLower()).ToList();
            var relatedAnimations = RelatedAnimations;
            var targetAnimations = relatedAnimations.Select(a => a.AnimationName.Replace(" ", "").Replace("_", "").ToLower()).ToList();

            bool changed = false;

            for (int i = 0; i < animations.Count; i++)
            {
                var skinAnimations = skinData.GetAnimations(lodLevel);

                if (skinAnimations[i].FrameRate != 0 && skinAnimations[i].FrameCount != 0 && !force)
                {
                    animBindingStates.Add(AnimBindingState.Default);
                    continue;
                }

                bool assigned = false;

                int relatedAnimIndex = -1;
                float maxCurrentMatchedRate = float.MinValue;

                for (int j = 0; j < relatedAnimations.Count; j++)
                {
                    var guid = relatedAnimations[j].AnimationGUID;

                    if (string.IsNullOrEmpty(guid))
                    {
                        continue;
                    }

                    if (animations[i].Guid == guid)
                    {
                        assigned = true;
                        relatedAnimIndex = j;
                        maxCurrentMatchedRate = 1f;
                        break;
                    }
                }

                if (!assigned)
                {
                    string sourceAnimation = sourceAnimations[i];

                    for (int j = 0; j < targetAnimations.Count; j++)
                    {
                        var targetAnimation = targetAnimations[j];
                        if (targetAnimation.Contains(sourceAnimation))
                        {
                            var currentMatchRate = 0f;

                            if (sourceAnimation.Length < targetAnimation.Length)
                            {
                                currentMatchRate = (float)sourceAnimation.Length / targetAnimation.Length;
                            }
                            else
                            {
                                currentMatchRate = (float)targetAnimation.Length / sourceAnimation.Length;
                            }

                            if (currentMatchRate >= minAnimationTextMatchRate && currentMatchRate > maxCurrentMatchedRate)
                            {
                                assigned = true;
                                relatedAnimIndex = j;
                                maxCurrentMatchedRate = currentMatchRate;
                            }
                        }
                    }
                }

                if (assigned)
                {
                    changed = true;
                    AssignAnimationData(i, relatedAnimIndex);

                    var successState = maxCurrentMatchedRate > 0.99f ? AnimBindingState.Success : AnimBindingState.PartialSuccess;

                    animBindingStates.Add(successState);
                }
                else
                {
                    animBindingStates.Add(AnimBindingState.Failed);
                }
            }

            if (changed)
            {
                EditorSaver.SetObjectDirty(characterAnimationContainer);
            }
        }

        public bool AddAnimation(int skinIndex, string guid, int lodLevel = 0)
        {
            if (!HasAnimation(skinIndex, guid, lodLevel))
            {
                var skinData = GetSkinData(skinIndex);

                var newAnimation = new AnimationData()
                {
                    Guid = guid,
                };

                skinData.AddAnimationData(newAnimation, lodLevel);
                EditorSaver.SetObjectDirty(characterAnimationContainer);
                return true;
            }

            return false;
        }

        public bool RemoveAnimation(int skinIndex, string guid, int lodLevel = 0)
        {
            if (HasAnimation(skinIndex, guid, lodLevel))
            {
                var skinData = GetSkinData(skinIndex);
                skinData.RemoveAnimationData(guid, lodLevel);
                EditorSaver.SetObjectDirty(characterAnimationContainer);
                return true;
            }

            return false;
        }

        public bool HasAnimation(int skinIndex, string guid, int lodLevel = 0)
        {
            var skinData = GetSkinData(skinIndex);

            var data = skinData.GetAnimationData(guid, lodLevel);

            return data != null;
        }

        public bool ContainsMesh(Mesh mesh)
        {
            return characterAnimationContainer.ContainsMesh(mesh);
        }

        public void PerformDataAction(Action<AnimationTextureDataContainer> action)
        {
            if (!SourceDataExist)
                return;

            if (animationTextureDataContainer.CurrentContainerType == ContainerType.Single)
            {
                action(animationTextureDataContainer as AnimationTextureDataContainer);
            }
            else
            {
                var multi = animationTextureDataContainer as AnimationTextureDataMultiContainer;

                foreach (var container in multi.Containers)
                {
                    action(container);
                }
            }
        }

        public AnimationTextureDataContainer GetContainerBy(Func<AnimationTextureDataContainer, bool> action)
        {
            if (!SourceDataExist)
                return null;

            if (animationTextureDataContainer.CurrentContainerType == ContainerType.Single)
            {
                var container = animationTextureDataContainer as AnimationTextureDataContainer;

                if (action(container))
                    return container;
            }
            else
            {
                var multi = animationTextureDataContainer as AnimationTextureDataMultiContainer;

                foreach (var container in multi.Containers)
                {
                    if (container && action(container))
                        return container;
                }
            }

            return null;
        }

        #endregion

        #region Editor events

        public virtual void OnInspectorEnabled()
        {
            ValidateAnimations();
            InitAvailableMeshes();
        }

        public virtual void OnInspectorDisabled()
        {
            animBindingStates.Clear();
        }

        public void OnTextureDataChanged()
        {
            InitAvailableMeshes();

            displayAddTab = true;

            if (mesh)
            {
                mesh = null;
            }
        }

        public virtual void OnSkinIndexSelectedChanged(int previousIndex, int newIndex, bool force = false)
        {
            if (newIndex == previousIndex && !force)
            {
                return;
            }

            animBindingStates.Clear();

            if (CanFindAnimation)
            {
                FindAnimations();
            }
        }

        #endregion

        #region Private methods

        private bool HasAnimationContainer()
        {
            if (characterAnimationContainer == null)
            {
                UnityEngine.Debug.LogError($"Animation container not assigned");
                return false;
            }

            return true;
        }

        private int FindAnimationIndexInContainer(string guid, AnimationUseType animationType)
        {
            var animations = animationCollectionContainer.GetAnimations(animationType);

            for (int i = 0; i < animations.Count; i++)
            {
                if (animations[i].Guid == guid)
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindAnimationGuidIndexInSkinData(int skinIndex, string guid, int lodLevel = 0)
        {
            var skinData = characterAnimationContainer.GetSkinData(skinIndex);
            var animations = skinData.GetAnimations(lodLevel);

            for (int i = 0; i < animations.Count; i++)
            {
                if (animations[i].Guid == guid)
                {
                    return i;
                }
            }

            return -1;
        }

        private void AssignDefaultAnimations(SkinData skinData, int lodLevel = 0)
        {
            var animations = animationCollectionContainer.GetAnimations();
            var skinAnimations = skinData.GetAnimations(lodLevel);

            for (int i = 0; i < animations.Count; i++)
            {
                BindAnimation(skinAnimations[i], animations[i]);
            }

            EditorSaver.SetObjectDirty(characterAnimationContainer);
        }

        private void BindAnimation(AnimationData animationData, AnimationCollectionContainer.AnimationData containerAnimation)
        {
            animationData.Guid = containerAnimation.Guid;
            animationData.AnimationHash = containerAnimation.Hash;
        }

        private void InitAvailableMeshes()
        {
            availableContainerMeshes.Clear();

            if (!SourceDataExist)
                return;

            PerformDataAction(animationTextureDataContainer =>
            {
                for (int i = 0; i < animationTextureDataContainer.TextureDatas.Count; i++)
                {
                    var mesh = animationTextureDataContainer.TextureDatas[i].SourceMesh;
                    var sourceMaterial = animationTextureDataContainer.TextureDatas[i].SourceMaterial;

                    if (mesh && !availableContainerMeshes.Any(a => a.Mesh == mesh))
                    {
                        var meshData = new MeshData()
                        {
                            Mesh = mesh,
                            Material = sourceMaterial
                        };

                        availableContainerMeshes.Add(meshData);
                    }
                }
            });
        }

        #endregion
    }
}
