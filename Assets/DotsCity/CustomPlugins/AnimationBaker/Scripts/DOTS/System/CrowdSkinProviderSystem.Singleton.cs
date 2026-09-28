using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine.Rendering;

namespace Spirit604.AnimationBaker.Entities
{
    public partial class CrowdSkinProviderSystem : SystemBase
    {
        public struct Singleton : IComponentData
        {
            internal NativeHashMap<SkinAnimationHash, HashToIndexData> hashToLocalData;
            internal NativeHashSet<int> allowDuplicateHashes;
            internal NativeHashSet<int> takenIndexes;
            internal NativeHashMap<int, BatchMeshID> m_MeshMapping;
            internal NativeHashMap<int, BatchMaterialID> m_MaterialMapping;
            internal NativeArray<RenderBounds> skinBounds;
            internal BlobAssetReference<AnimationBlob> animationBlob;

            public ClipBlobData GetClipData(int skinIndex, int animationHash, int lodLevel = 0)
            {
                var localAnimationIndex = LoadGPUSkinUtils.GetLocalAnimationIndex(in hashToLocalData, skinIndex, animationHash);

                ref var clipData = ref animationBlob.GetAnimationData(skinIndex, localAnimationIndex, lodLevel);
                return clipData;
            }

            public RenderBounds GetSkinBounds(int skinIndex)
            {
                return skinBounds[skinIndex];
            }

            public bool UpdateAnimation(ref SkinUpdateComponent skinUpdateComponent, ref EnabledRefRW<UpdateSkinTag> updateSkinTagRW, int skinIndex, int animHash)
            {
                var animationIndex = LoadGPUSkinUtils.GetLocalAnimationIndex(in hashToLocalData, skinIndex, animHash);

                if (animationIndex != -1)
                {
                    AnimEntitiesUtils.UpdateAnimation(ref skinUpdateComponent, ref updateSkinTagRW, animHash);
                    return true;
                }

                return false;
            }

            public bool TryToRemoveIndex(int takenMeshIndex)
            {
                if (takenIndexes.Contains(takenMeshIndex))
                {
                    takenIndexes.Remove(takenMeshIndex);
                    return true;
                }

                return false;
            }
        }
    }
}