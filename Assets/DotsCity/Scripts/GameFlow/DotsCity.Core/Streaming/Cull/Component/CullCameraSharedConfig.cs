using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Core
{
    public struct CullCameraSharedConfig : ISharedComponentData, IEquatable<CullCameraSharedConfig>
    {
        public bool IgnoreY;
        public float MaxDistanceSQ;
        public float VisibleDistanceSQ;
        public float PreinitDistanceSQ;
        public float ViewPortOffset;

        public float BehindMaxDistanceSQ;
        public float BehindVisibleDistanceSQ;
        public float BehindPreinitDistanceSQ;

        public bool Equals(CullCameraSharedConfig other) =>
            this.MaxDistanceSQ == other.MaxDistanceSQ && this.VisibleDistanceSQ == other.VisibleDistanceSQ &&
            this.BehindMaxDistanceSQ == other.BehindMaxDistanceSQ && this.BehindVisibleDistanceSQ == other.BehindVisibleDistanceSQ;

        public override int GetHashCode()
        {
            unchecked
            {
                return (int)(math.round(this.MaxDistanceSQ)) + ((int)math.round(this.VisibleDistanceSQ) << 16);
            }
        }
    }
}
