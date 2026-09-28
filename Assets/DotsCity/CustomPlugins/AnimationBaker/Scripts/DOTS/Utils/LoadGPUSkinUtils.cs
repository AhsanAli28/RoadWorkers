using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine.Rendering;

namespace Spirit604.AnimationBaker.Entities
{
    public static class LoadGPUSkinUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UpdateSkin(
            Random rndGen,
            float time,
            ref EntityCommandBuffer commandBuffer,
            Entity entity,
            ref SkinUpdateComponent skinUpdateComponent,
            ref SkinAnimatorData skinAnimatorData,
            ref ShaderPlaybackTime shaderPlaybackTime,
            ref ShaderTargetFrameOffsetData shaderTargetFrameOffsetData,
            ref ShaderTargetFrameStepInvData shaderTargetFrameStepInvData,
            ref MaterialMeshInfo materialMeshInfo,
            ref EnabledRefRW<UpdateSkinTag> updateSkinTagRW,
            in AnimationBlobReference animationBlobRef,
            ref ComponentLookup<TakenAnimationDataComponent> takenAnimationDataLookup,
            ref NativeHashSet<int> takenIndexes,
            in NativeHashMap<SkinAnimationHash, HashToIndexData> hashToLocalData,
            in NativeHashSet<int> allowDuplicateHashes,
            in NativeHashMap<int, BatchMaterialID> materialMapping,
            in NativeHashMap<int, BatchMeshID> meshMapping)
        {
            var animationHash = skinUpdateComponent.NewAnimationHash;

            var newAnimationIndex = GetLocalAnimationIndex(in hashToLocalData, skinAnimatorData.SkinIndex, animationHash);

            if (newAnimationIndex != -1)
            {
            }
            else
            {
#if UNITY_EDITOR
                UnityEngine.Debug.LogError($"UpdateSkin. No animation hash found. Entity {entity.Index} AnimationHash {animationHash}");
#endif
            }

            skinAnimatorData.StartAnimationTime = time;
            skinAnimatorData.CurrentAnimationHash = animationHash;

            if (!skinUpdateComponent.UniqueAnimation)
            {
                shaderPlaybackTime.Value = -1;
                shaderTargetFrameOffsetData.Value = -1;
                shaderTargetFrameStepInvData.Value = -1;
            }

            bool uniqueMaterialFound;

            int newMaterialIndex;
            int newMeshIndex;
            bool meshIsTaken;
            BatchMaterialID newMaterialBatchId;
            BatchMeshID newMeshBatchId;

            GetMeshData(
                skinAnimatorData.SkinIndex,
                skinUpdateComponent.NewAnimationHash,
                rndGen,
                in animationBlobRef,
                ref takenIndexes,
                in allowDuplicateHashes,
                in hashToLocalData,
                in materialMapping,
                in meshMapping,
                out newMaterialIndex,
                out newMeshIndex,
                out newMaterialBatchId,
                out newMeshBatchId,
                out uniqueMaterialFound,
                out meshIsTaken);

            skinAnimatorData.UniqueAnimation = meshIsTaken;

            bool checkForReleaseAnimation = true;

            if (newMeshIndex >= 0 && (materialMeshInfo.MeshID != newMeshBatchId || materialMeshInfo.MaterialID != newMaterialBatchId))
            {
                materialMeshInfo.MeshID = newMeshBatchId;
                materialMeshInfo.MaterialID = newMaterialBatchId;

                if (uniqueMaterialFound)
                {
                    checkForReleaseAnimation = false;

                    if (!takenAnimationDataLookup.HasComponent(entity))
                    {
                        if (meshIsTaken)
                        {
                            commandBuffer.AddComponent(entity, new TakenAnimationDataComponent()
                            {
                                SkinIndex = skinAnimatorData.SkinIndex,
                                AnimationHash = skinUpdateComponent.NewAnimationHash,
                                TakenMeshIndex = newMeshIndex
                            });
                        }
                    }
                    else
                    {
                        if (meshIsTaken)
                        {
                            var existTakenAnimData = takenAnimationDataLookup[entity];

                            if (takenIndexes.Contains(existTakenAnimData.TakenMeshIndex))
                            {
                                takenIndexes.Remove(existTakenAnimData.TakenMeshIndex);
                            }

                            existTakenAnimData.SkinIndex = skinAnimatorData.SkinIndex;
                            existTakenAnimData.AnimationHash = skinUpdateComponent.NewAnimationHash;
                            existTakenAnimData.TakenMeshIndex = newMeshIndex;
                            takenAnimationDataLookup[entity] = existTakenAnimData;
                        }
                        else
                        {
                            checkForReleaseAnimation = true;
                        }
                    }
                }
            }

            if (checkForReleaseAnimation)
            {
                if (takenAnimationDataLookup.HasComponent(entity))
                {
                    var existTakenAnimData = takenAnimationDataLookup[entity];

                    if (takenIndexes.Contains(existTakenAnimData.TakenMeshIndex))
                    {
                        takenIndexes.Remove(existTakenAnimData.TakenMeshIndex);
                    }

                    commandBuffer.RemoveComponent<TakenAnimationDataComponent>(entity);
                }
            }

            skinUpdateComponent = new SkinUpdateComponent();

            updateSkinTagRW.ValueRW = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetMeshData(
            int skinIndex,
            int animationHash,
            Random rndGen,
            in AnimationBlobReference animationBlobRef,
            ref NativeHashSet<int> takenIndexes,
            in NativeHashSet<int> allowDuplicateHashes,
            in NativeHashMap<SkinAnimationHash, HashToIndexData> hashToLocalData,
            in NativeHashMap<int, BatchMaterialID> materialMapping,
            in NativeHashMap<int, BatchMeshID> meshMapping,
            out int materialIndex,
            out int meshIndex,
            out BatchMaterialID batchMaterialID,
            out BatchMeshID batchMeshID,
            out bool uniqueMaterial,
            out bool meshIsTaken)
        {
            materialIndex = -1;
            meshIndex = -1;

            batchMaterialID = default;
            batchMeshID = default;

            uniqueMaterial = false;
            meshIsTaken = false;

            if (animationHash == 0)
            {
                return;
            }

            var crowdAnimIndex = GetCrowdAnimationIndex(in hashToLocalData, skinIndex, animationHash);

            if (crowdAnimIndex == -1)
            {
                return;
            }

            int minMeshIndex;
            int maxMeshIndex;

            uniqueMaterial = animationBlobRef.IsUniqueSkin(crowdAnimIndex, out minMeshIndex, out maxMeshIndex);

            for (int currentMeshIndex = minMeshIndex; currentMeshIndex < maxMeshIndex; currentMeshIndex++)
            {
                if (!uniqueMaterial)
                {
                    meshIndex = currentMeshIndex;
                    materialIndex = currentMeshIndex;
                    break;
                }
                else
                {
                    if (allowDuplicateHashes.Contains(animationHash))
                    {
                        meshIndex = rndGen.NextInt(minMeshIndex, maxMeshIndex);
                        materialIndex = meshIndex;
                        break;
                    }
                    else
                    {
                        if (!takenIndexes.Contains(currentMeshIndex))
                        {
                            meshIndex = currentMeshIndex;
                            materialIndex = currentMeshIndex;
                            meshIsTaken = true;
                            break;
                        }
                    }
                }
            }

            if (meshIndex == -1)
            {
                var localAnimIndex = GetLocalAnimationIndex(in hashToLocalData, skinIndex, animationHash);

                if (takenIndexes.Contains(minMeshIndex))
                {
                    meshIndex = rndGen.NextInt(minMeshIndex, maxMeshIndex);
                    materialIndex = meshIndex;

                    uniqueMaterial = true;
                    meshIsTaken = true;

                    if (!allowDuplicateHashes.Contains(animationHash))
                    {
#if UNITY_EDITOR
                        UnityEngine.Debug.Log($"Mesh pool seems exceeded. SkinIndex {skinIndex} AnimationHash {animationHash} LocalAnimationIndex {localAnimIndex}");
#endif
                    }
                }
                else
                {
#if UNITY_EDITOR
                    UnityEngine.Debug.Log($"Mesh not found. SkinIndex {skinIndex} AnimationHash {animationHash} LocalAnimationIndex {localAnimIndex}");
#endif
                }
            }

            if (meshIndex != -1)
            {
                var materialId = animationBlobRef.GetMaterialInstanceByIndex(materialIndex);
                var meshId = animationBlobRef.GetMeshInstanceByIndex(meshIndex);

                batchMaterialID = materialMapping[materialId];
                batchMeshID = meshMapping[meshId];

                if (meshIsTaken)
                {
                    if (!takenIndexes.Contains(meshIndex))
                    {
                        takenIndexes.Add(meshIndex);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetLocalAnimationIndex(in NativeHashMap<SkinAnimationHash, HashToIndexData> hashToLocalData, int skinIndex, int hash)
        {
            var skinHash = new SkinAnimationHash(skinIndex, hash);

            if (hashToLocalData.ContainsKey(skinHash))
            {
                return hashToLocalData[skinHash].LocalAnimationIndex;
            }
            else
            {
#if UNITY_EDITOR
                UnityEngine.Debug.Log($"Animation skinIndex {skinIndex} hash {hash} not found");
#endif
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCrowdAnimationIndex(in NativeHashMap<SkinAnimationHash, HashToIndexData> hashToLocalData, int skinIndex, int hash)
        {
            var skinHash = new SkinAnimationHash(skinIndex, hash);

            if (hashToLocalData.ContainsKey(skinHash))
            {
                return hashToLocalData[skinHash].CrowdAnimationIndex;
            }
            else
            {
#if UNITY_EDITOR
                UnityEngine.Debug.Log($"Animation skinIndex {skinIndex} hash {hash} not found");
#endif
            }

            return -1;
        }
    }
}