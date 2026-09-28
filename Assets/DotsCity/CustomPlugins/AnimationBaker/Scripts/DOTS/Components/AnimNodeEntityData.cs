using Unity.Entities;

namespace Spirit604.AnimationBaker.Entities
{
    public struct AnimNodeEntityData : IComponentData
    {
        public int AnimHash;
        public bool UniqueAnimation;
    }
}
