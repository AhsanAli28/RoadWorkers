using System;
using Unity.Entities;

namespace Spirit604.DotsCity.Core
{
    public struct WorldEntitySharedType : ISharedComponentData, IEquatable<WorldEntitySharedType>
    {
        public EntityWorldType EntityWorldType;

        public WorldEntitySharedType(EntityWorldType entityWorldType)
        {
            EntityWorldType = entityWorldType;
        }

        public bool Equals(WorldEntitySharedType other) => this.EntityWorldType == other.EntityWorldType;

        public override int GetHashCode() => (int)(EntityWorldType);
    }
}
