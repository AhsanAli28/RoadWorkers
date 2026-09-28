using System.Runtime.CompilerServices;
using Unity.Entities;

namespace Spirit604.AnimationBaker.Entities
{
    public static class AnimationBlobUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref ClipBlobData GetAnimationData(ref this AnimationBlobReference npcAnimationRef, int skinIndex, int localAnimationIndex, int lodLevel = 0)
        {
            ref var animationBlob = ref npcAnimationRef.BlobRef;

            return ref GetAnimationData(ref animationBlob, skinIndex, localAnimationIndex, lodLevel);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref ClipBlobData GetAnimationData(ref this BlobAssetReference<AnimationBlob> animationBlobRef, int skinIndex, int localAnimationIndex, int lodLevel = 0)
        {
            ref var targetAnimData = ref animationBlobRef.Value.SkinDataArray[skinIndex].Lods[lodLevel].Clips[localAnimationIndex];

            return ref targetAnimData;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetMeshInstanceByIndex(this in AnimationBlobReference animationBlobRef, int meshIndex)
        {
            return animationBlobRef.BlobRef.Value.MeshIndexToInstance[meshIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetMaterialInstanceByIndex(this in AnimationBlobReference animationBlobRef, int materialIndex)
        {
            return animationBlobRef.BlobRef.Value.MaterialIndexToInstance[materialIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref SkinMeshBlobData GetMeshData(this in AnimationBlobReference animationBlobRef, int crowdAnimIndex)
        {
            return ref animationBlobRef.BlobRef.Value.SkinMeshDataArray[crowdAnimIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsUniqueSkin(this in AnimationBlobReference animationBlobRef, int crowdAnimIndex, out int minMeshIndex, out int maxMeshIndex)
        {
            minMeshIndex = animationBlobRef.BlobRef.Value.SkinMeshDataArray[crowdAnimIndex].MinMeshIndex;
            maxMeshIndex = animationBlobRef.BlobRef.Value.SkinMeshDataArray[crowdAnimIndex].MaxMeshIndex;

            var uniqueMaterial = (maxMeshIndex - minMeshIndex) > 1;

            return uniqueMaterial;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsUniqueSkin(this in AnimationBlobReference animationBlobRef, int crowdAnimIndex)
        {
            return IsUniqueSkin(in animationBlobRef, crowdAnimIndex, out var minMeshIndex, out var maxMeshIndex);
        }
    }
}
