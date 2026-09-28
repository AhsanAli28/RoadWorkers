using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Core
{
    public struct CullSharedConfig : ISharedComponentData, IEquatable<CullSharedConfig>
    {
        public bool IgnoreY;
        public float MaxDistanceSQ;
        public float VisibleDistanceSQ;
        public float PreinitDistanceSQ;

        public bool Equals(CullSharedConfig other) => this.MaxDistanceSQ == other.MaxDistanceSQ && this.VisibleDistanceSQ == other.VisibleDistanceSQ;

        public override int GetHashCode()
        {
            unchecked
            {
                return (int)(math.round(this.MaxDistanceSQ)) + ((int)math.round(this.VisibleDistanceSQ) << 16);
            }
        }
    }
}
