using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.AnimationBaker.Entities
{
    public struct ClipBlobData
    {
        public float ClipLength;
        public float FrameRate;
        public float2 FrameOffset;
        public int FrameCount;
        public float FrameStepInv;
    }

    public struct SkinMeshBlobData
    {
        public int MinMaterialIndex;
        public int MaxMaterialIndex;
        public int MinMeshIndex;
        public int MaxMeshIndex;
    }

    public struct SkinLoadData
    {
        public BlobArray<ClipBlobData> Clips;
    }

    public struct SkinAnimationData
    {
        public BlobArray<SkinLoadData> Lods;
    }

    public struct AnimationBlob
    {
        public BlobArray<SkinAnimationData> SkinDataArray;
        public BlobArray<SkinMeshBlobData> SkinMeshDataArray;
        public BlobArray<int> MeshIndexToInstance;
        public BlobArray<int> MaterialIndexToInstance;
    }

    public struct AnimationBlobReference : IComponentData
    {
        public BlobAssetReference<AnimationBlob> BlobRef;
    }
}
