using System;
using UnityEngine;

namespace Spirit604.AnimationBaker
{
    [Serializable]
    public class AnimationTextureData : ICloneable
    {
        public Mesh SourceMesh;
        public Material SourceMaterial;
        public AnimationClip SourceClip;
        public string AnimationGUID;
        public string AnimationName;
        public float FrameRate;
        public Vector2Int TextureOffset;
        public int VertexCount;
        public int FrameCount;
        public Vector3 BakeOffset;
        public bool Interpolate;
        public int LodLevel;

        public AnimationClip ClipToReplace { get; set; }
        public float ClipLength => SourceClip?.length ?? 0;
        public float OriginalFrameRate => SourceClip?.frameRate ?? 0;
        public float FrameStep => ClipLength / (FrameCount - 1);
        public float FrameStepInverted => 1 / FrameStep;

        public object Clone() => this.MemberwiseClone();
    }
}