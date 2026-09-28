using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Spirit604.AnimationBaker.Entities
{
    [DisableAutoTypeRegistration]
    [DisableAutoCreation]
    public partial class CrowdSkinProviderSystem : SystemBase
    {
        private const int TakenIndexesCapacity = 1000;
        private const float ScaleBoundsSize = 80f;
        private const float HugeBoundsMultiplier = 0.01f;
        private const int LodCount = 1;

        private CrowdSkinFactory crowdSkinFactory;
        private EntitiesGraphicsSystem entitiesGraphicsSystem;

        private NativeHashMap<SkinAnimationHash, HashToIndexData> hashToLocalData;
        private NativeHashSet<int> allowDuplicateHashes;
        private NativeHashSet<int> takenIndexes;
        private NativeHashMap<int, BatchMeshID> m_MeshMapping;
        private NativeHashMap<int, BatchMaterialID> m_MaterialMapping;
        private NativeArray<RenderBounds> skinBounds;

        private List<int> meshIndexToInstance;
        private List<int> materialIndexToInstance;

        private class TempSkinMeshAnimationData
        {
            public int MinMaterialIndex;
            public int MaxMaterialIndex;
            public int MinMeshIndex;
            public int MaxMeshIndex;
        }

        private List<TempSkinMeshAnimationData> tempSkinAnimationMeshData = new List<TempSkinMeshAnimationData>();
        private BlobAssetReference<AnimationBlob> animationBlob;

        public NativeHashMap<SkinAnimationHash, HashToIndexData> HashToLocalData => hashToLocalData;
        public NativeHashSet<int> TakenIndexes => takenIndexes;
        public NativeHashMap<int, BatchMeshID> MeshMapping => m_MeshMapping;
        public NativeHashMap<int, BatchMaterialID> MaterialMapping => m_MaterialMapping;
        public NativeHashSet<int> AllowDuplicateHashes { get => allowDuplicateHashes; }
        public RenderMeshArray TotalRenderMeshData { get; private set; }

        public static NativeHashMap<SkinAnimationHash, HashToIndexData> HashToLocalDataStaticRef { get; private set; }
        public static NativeHashSet<int> TakenIndexesStaticRef { get; private set; }
        public static NativeHashMap<int, BatchMeshID> MeshMappingStaticRef { get; private set; }
        public static NativeHashMap<int, BatchMaterialID> MaterialMappingStaticRef { get; private set; }
        public static NativeHashSet<int> AllowDuplicateHashesStaticRef { get; private set; }

        public event Action OnInitialized = delegate { };

        protected override void OnCreate()
        {
            base.OnCreate();
            entitiesGraphicsSystem = World.GetOrCreateSystemManaged<EntitiesGraphicsSystem>();
            Enabled = false;
        }

        protected override void OnDestroy()
        {
            if (hashToLocalData.IsCreated)
            {
                hashToLocalData.Dispose();
                HashToLocalDataStaticRef = default;
            }

            if (allowDuplicateHashes.IsCreated)
            {
                allowDuplicateHashes.Dispose();
                AllowDuplicateHashesStaticRef = default;
            }

            if (m_MeshMapping.IsCreated)
            {
                m_MeshMapping.Dispose();
                MeshMappingStaticRef = default;
            }

            if (m_MaterialMapping.IsCreated)
            {
                m_MaterialMapping.Dispose();
                MaterialMappingStaticRef = default;
            }

            if (skinBounds.IsCreated)
            {
                skinBounds.Dispose();
            }

            if (takenIndexes.IsCreated)
            {
                takenIndexes.Dispose();
                TakenIndexesStaticRef = default;
            }

            if (animationBlob.IsCreated)
            {
                animationBlob.Dispose();
            }
        }

        protected override void OnUpdate() { }

        public void Initialize(CrowdSkinFactory crowdSkinFactory)
        {
            this.crowdSkinFactory = crowdSkinFactory;
        }

        public BatchMeshID GetDefaultMeshBatchId(int skinIndex)
        {
            return GetMeshBatchId(0, 0);
        }

        public BatchMaterialID GetDefaultMaterialBatchId(int skinIndex)
        {
            return GetMaterialBatchId(0, 0);
        }

        public BatchMeshID GetMeshBatchId(int skinIndex, int meshIndex)
        {
            if (meshIndex < 0)
            {
                return default;
            }

            var meshId = meshIndexToInstance[meshIndex];
            return m_MeshMapping[meshId];
        }

        public BatchMaterialID GetMaterialBatchId(int skinIndex, int materialIndex)
        {
            if (materialIndex < 0)
            {
                return default;
            }

            var materialId = materialIndexToInstance[materialIndex];
            return m_MaterialMapping[materialId];
        }

        public Entity CreateBlobEntity()
        {
            InitHash();
            Init();

            animationBlob = CreateAnimationBlobData();

            var npcAnimationBlobReference = new AnimationBlobReference()
            {
                BlobRef = animationBlob
            };

            var entity = EntityManager.CreateEntity(typeof(AnimationBlobReference));

            EntityManager.SetComponentData(entity, npcAnimationBlobReference);

            var singleton = new Singleton()
            {
                hashToLocalData = this.hashToLocalData,
                skinBounds = this.skinBounds,
                allowDuplicateHashes = this.allowDuplicateHashes,
                animationBlob = this.animationBlob,
                m_MaterialMapping = this.m_MaterialMapping,
                m_MeshMapping = this.m_MeshMapping,
                takenIndexes = this.takenIndexes,
            };

            EntityManager.AddComponentData(SystemHandle, singleton);

            OnInitialized();

            return entity;
        }

        private void Init()
        {
            var characterAnimationContainer = crowdSkinFactory.CharacterAnimationContainer;
            var animationsData = crowdSkinFactory.AnimationsData;

            takenIndexes = new NativeHashSet<int>(TakenIndexesCapacity, Allocator.Persistent);
            TakenIndexesStaticRef = takenIndexes;

            int minMaterialIndex = 0;
            int maxMaterialIndex = 0;
            int minMeshIndex = 0;
            int maxMeshIndex = 0;

            List<Material> materials = new List<Material>();
            List<Mesh> meshes = new List<Mesh>();

            var m_MeshMappingTemp = new Dictionary<int, BatchMeshID>();
            var m_MaterialMappingTemp = new Dictionary<int, BatchMaterialID>();

            meshIndexToInstance = new List<int>();
            materialIndexToInstance = new List<int>();
            int index = 0;

            NativeList<RenderBounds> skinBoundsTempLocal = new NativeList<RenderBounds>(Allocator.TempJob);

            for (int skinIndex = 0; skinIndex < characterAnimationContainer.Count; skinIndex++)
            {
                for (int lodLevel = 0; lodLevel < LodCount; lodLevel++)
                {
                    var currentSkinData = characterAnimationContainer.GetSkinData(skinIndex);

                    var mesh = currentSkinData.GetMesh(lodLevel);

                    var center = mesh.bounds.center;
                    var extents = mesh.bounds.extents;

                    if (extents.x > ScaleBoundsSize)
                    {
                        center *= HugeBoundsMultiplier;
                        extents *= HugeBoundsMultiplier;
                    }

                    var renderBounds = new RenderBounds()
                    {
                        Value = new AABB()
                        {
                            Center = center,
                            Extents = extents
                        }
                    };

                    skinBoundsTempLocal.Add(renderBounds);

                    var animations = currentSkinData.GetAnimations(lodLevel);

                    for (int animIndex = 0; animIndex < animations.Count; animIndex++)
                    {
                        var animData = animations[animIndex];

                        var instanceCount = animationsData[animIndex].GetInstanceCount();

                        for (int i = 0; i < instanceCount; i++)
                        {
                            var newMaterial = GameObject.Instantiate(currentSkinData.GetMaterial(lodLevel));
                            var newMesh = GameObject.Instantiate(currentSkinData.GetMesh(lodLevel));

                            crowdSkinFactory.InitMaterial(newMaterial, animData);

                            RegisterMesh(
                                newMaterial,
                                newMesh,
                                materials,
                                meshes,
                                m_MeshMappingTemp,
                                m_MaterialMappingTemp);

                            maxMaterialIndex++;
                            maxMeshIndex++;
                            index++;
                        }

                        tempSkinAnimationMeshData.Add(new TempSkinMeshAnimationData()
                        {
                            MinMaterialIndex = minMaterialIndex,
                            MaxMaterialIndex = maxMaterialIndex,
                            MinMeshIndex = minMeshIndex,
                            MaxMeshIndex = maxMeshIndex
                        });

                        minMaterialIndex = maxMaterialIndex;
                        minMeshIndex = maxMeshIndex;
                    }
                }
            }

            TotalRenderMeshData = new RenderMeshArray(materials.ToArray(), meshes.ToArray());

            m_MeshMapping = new NativeHashMap<int, BatchMeshID>(m_MeshMappingTemp.Count, Allocator.Persistent);
            m_MaterialMapping = new NativeHashMap<int, BatchMaterialID>(m_MaterialMappingTemp.Count, Allocator.Persistent);

            MeshMappingStaticRef = m_MeshMapping;
            MaterialMappingStaticRef = m_MaterialMapping;

            foreach (var item in m_MeshMappingTemp)
            {
                m_MeshMapping.Add(item.Key, item.Value);
            }

            foreach (var item in m_MaterialMappingTemp)
            {
                m_MaterialMapping.Add(item.Key, item.Value);
            }

            skinBounds = skinBoundsTempLocal.ToArray(Allocator.Persistent);
            skinBoundsTempLocal.Dispose();

            m_MeshMappingTemp.Clear();
            m_MeshMappingTemp = null;

            m_MaterialMappingTemp.Clear();
            m_MaterialMappingTemp = null;
        }

        private void RegisterMesh(Material newMaterial, Mesh newMesh, List<Material> materials, List<Mesh> meshes, Dictionary<int, BatchMeshID> m_MeshMappingTemp, Dictionary<int, BatchMaterialID> m_MaterialMappingTemp)
        {
            var materialBatchId = entitiesGraphicsSystem.RegisterMaterial(newMaterial);
            var meshBatchId = entitiesGraphicsSystem.RegisterMesh(newMesh);

            var newMaterialHash = newMaterial.GetHashCode();
            var newMeshHash = newMesh.GetHashCode();

            m_MaterialMappingTemp.Add(newMaterialHash, materialBatchId);
            m_MeshMappingTemp.Add(newMeshHash, meshBatchId);

            materialIndexToInstance.Add(newMaterialHash);
            meshIndexToInstance.Add(newMeshHash);

            materials.Add(newMaterial);
            meshes.Add(newMesh);
        }

        private void InitHash()
        {
            var animationsData = crowdSkinFactory.AnimationsData;
            var characterAnimationContainer = crowdSkinFactory.CharacterAnimationContainer;

            int animationIndex = 0;

            var allowDuplicateHashesTemp = new HashSet<int>();

            var tempHashDict = new Dictionary<SkinAnimationHash, HashToIndexData>();

            for (int skinIndex = 0; skinIndex < characterAnimationContainer.Count; skinIndex++)
            {
                var skinData = characterAnimationContainer.GetSkinData(skinIndex);

                for (int lodLevel = 0; lodLevel < LodCount; lodLevel++)
                {
                    var skinAnimations = skinData.GetAnimations(lodLevel);
                    var skinAnimationCount = skinAnimations.Count;

                    for (int j = 0; j < skinAnimationCount; j++)
                    {
                        var animationHash = skinAnimations[j].AnimationHash;
                        var skinHash = new SkinAnimationHash(skinIndex, animationHash);

                        if (!tempHashDict.ContainsKey(skinHash))
                        {
                            tempHashDict.Add(skinHash, new HashToIndexData(j, animationIndex));

                            if (animationsData[j].AllowDuplicate)
                            {
                                allowDuplicateHashesTemp.Add(animationHash);
                            }
                        }

                        animationIndex++;
                    }
                }
            }

            hashToLocalData = new NativeHashMap<SkinAnimationHash, HashToIndexData>(tempHashDict.Count, Allocator.Persistent);
            HashToLocalDataStaticRef = hashToLocalData;

            foreach (var item in tempHashDict)
            {
                hashToLocalData.Add(item.Key, item.Value);
            }

            allowDuplicateHashes = new NativeHashSet<int>(allowDuplicateHashesTemp.Count, Allocator.Persistent);
            AllowDuplicateHashesStaticRef = allowDuplicateHashes;

            foreach (var hash in allowDuplicateHashesTemp)
            {
                allowDuplicateHashes.Add(hash);
            }

            allowDuplicateHashesTemp.Clear();
            allowDuplicateHashesTemp = null;
        }

        private BlobAssetReference<AnimationBlob> CreateAnimationBlobData()
        {
            var characterBakedAnimationContainer = crowdSkinFactory.CharacterAnimationContainer;

            using (var builder = new BlobBuilder(Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<AnimationBlob>();
                var skinDataArray = builder.Allocate(ref root.SkinDataArray, characterBakedAnimationContainer.Count);
                var skinMeshDataArray = builder.Allocate(ref root.SkinMeshDataArray, tempSkinAnimationMeshData.Count);
                var meshIndexToInstanceArray = builder.Allocate(ref root.MeshIndexToInstance, meshIndexToInstance.Count);
                var materialIndexToInstanceArray = builder.Allocate(ref root.MaterialIndexToInstance, materialIndexToInstance.Count);

                for (int skinIndex = 0; skinIndex < characterBakedAnimationContainer.Count; skinIndex++)
                {
                    var skinData = characterBakedAnimationContainer.GetSkinData(skinIndex);

                    var lodArray = builder.Allocate(ref skinDataArray[skinIndex].Lods, LodCount);

                    for (int lodLevel = 0; lodLevel < LodCount; lodLevel++)
                    {
                        var animations = skinData.GetAnimations(lodLevel);
                        var skinAnimationCount = animations.Count;

                        var animationArray = builder.Allocate(ref lodArray[lodLevel].Clips, skinAnimationCount);

                        for (int animationIndex = 0; animationIndex < skinAnimationCount; animationIndex++)
                        {
                            animationArray[animationIndex].ClipLength = animations[animationIndex].ClipLength;
                            animationArray[animationIndex].FrameRate = animations[animationIndex].FrameRate;
                            animationArray[animationIndex].FrameOffset = animations[animationIndex].FrameOffset;
                            animationArray[animationIndex].FrameCount = animations[animationIndex].FrameCount;
                            animationArray[animationIndex].FrameStepInv = animations[animationIndex].FrameStepInv;
                        }
                    }
                }

                for (int i = 0; i < tempSkinAnimationMeshData.Count; i++)
                {
                    skinMeshDataArray[i] = new SkinMeshBlobData()
                    {
                        MinMaterialIndex = tempSkinAnimationMeshData[i].MinMaterialIndex,
                        MaxMaterialIndex = tempSkinAnimationMeshData[i].MaxMaterialIndex,
                        MinMeshIndex = tempSkinAnimationMeshData[i].MinMeshIndex,
                        MaxMeshIndex = tempSkinAnimationMeshData[i].MaxMeshIndex,
                    };
                }

                for (int i = 0; i < meshIndexToInstance.Count; i++)
                {
                    meshIndexToInstanceArray[i] = meshIndexToInstance[i];
                }

                for (int i = 0; i < materialIndexToInstance.Count; i++)
                {
                    materialIndexToInstanceArray[i] = materialIndexToInstance[i];
                }

                return builder.CreateBlobAssetReference<AnimationBlob>(Allocator.Persistent);
            }
        }

        private bool HasDuplicateHash(int animationHash) => allowDuplicateHashes.Contains(animationHash);
    }
}