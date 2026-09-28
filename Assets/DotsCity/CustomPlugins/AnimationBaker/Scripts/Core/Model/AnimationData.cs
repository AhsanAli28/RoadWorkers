using System;
using UnityEngine;

namespace Spirit604.AnimationBaker
{
    [Serializable]
    public class AnimationData
    {
        public string Guid;
        public string ClipName;
        public int AnimationHash;
        public float ClipLength;
        public float OriginalFrameRate;
        public int VertexCount;
        public float FrameRate;
        public int FrameOffsetX;
        public int FrameOffsetY;
        public int FrameCount;
        public bool Interpolate;

        public float FrameStep => ClipLength / (FrameCount - 1);
        public float FrameStepInv => 1 / FrameStep;
        public Vector2 FrameOffset => new Vector2(FrameOffsetX, FrameOffsetY);
        public int InterpolateValue => Interpolate ? 1 : 0;
        public float CompressionValue => OriginalFrameRate != 0 ? 1 - FrameRate / OriginalFrameRate : 0;
        public float CompressionValuePerc => MathF.Round(CompressionValue * 100, 2);
    }
}