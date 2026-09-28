using System;
using Unity.Entities;

namespace Spirit604.DotsCity.Hybrid.Core
{
    public interface IHybridEntityRef
    {
        public Entity RelatedEntity { get; }
        public bool HasEntity { get; }

        event Action<Entity> OnEntityInitialized;

        void DestroyEntity();
    }
}
