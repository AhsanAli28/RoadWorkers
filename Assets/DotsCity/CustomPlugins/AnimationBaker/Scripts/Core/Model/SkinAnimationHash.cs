using System;

namespace Spirit604.AnimationBaker
{
    public struct SkinAnimationHash : IEquatable<SkinAnimationHash>
    {
        public readonly int SkinIndex;
        public readonly int AnimationHash;

        public SkinAnimationHash(int skinIndex, int animationHash)
        {
            SkinIndex = skinIndex;
            AnimationHash = animationHash;
        }

        public bool Equals(SkinAnimationHash other) => SkinIndex.Equals(other.SkinIndex) && AnimationHash.Equals(other.AnimationHash);

        public override bool Equals(object obj) => obj is SkinAnimationHash && Equals((SkinAnimationHash)obj);

        public override int GetHashCode()
        {
            unchecked
            {
                return (SkinIndex * 397) ^ AnimationHash;
            }
        }
    }
}