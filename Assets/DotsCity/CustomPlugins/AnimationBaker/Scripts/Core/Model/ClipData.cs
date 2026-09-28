using System;
using UnityEngine;

namespace Spirit604.AnimationBaker
{
    [Serializable]
    public class ClipData : ICloneable
    {
        public AnimationClip Clip;
        public bool HasCustomFrameRate;

        [Range(0, 60)] public int CustomFrameRate = 24;

        public bool Interpolate;
        public Vector3 Offset;
        public string CustomAnimationName;
        public string Guid;

        public string AnimationName => string.IsNullOrEmpty(CustomAnimationName) && GetClip() != null ? GetClip().name : CustomAnimationName;

        public int FrameCount { get; set; }

        public int InterpolateValue => Interpolate ? 1 : 0;

        public float CompressionValue => GetClip() != null ? 1 - CustomFrameRate / GetClip().frameRate : 0;

        public float CompressionValuePerc => MathF.Round(CompressionValue * 100, 2);

        public float GetCompression(float frameRate)
        {
            if (HasCustomFrameRate)
            {
                return CompressionValue;
            }

            if (GetClip())
            {
                return 1 - frameRate / GetClip().frameRate;
            }

            return 0;
        }

        public AnimationClip GetClip()
        {
            // Can be a typemismatch error, where the null check doesn't work.
            try
            {
                if (Clip != null && Clip.length >= 0)
                {
                    return Clip;
                }
            }
            catch { }

            return null;
        }

        public float GetCompressionPerc(float frameRate) => MathF.Round(GetCompression(frameRate) * 100, 2);

        public object Clone() => this.MemberwiseClone();
    }
}