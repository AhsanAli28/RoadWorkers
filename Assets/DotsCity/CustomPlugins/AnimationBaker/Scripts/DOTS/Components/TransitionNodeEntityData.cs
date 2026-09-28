using Unity.Entities;

namespace Spirit604.AnimationBaker.Entities
{
    public struct TransitionNodeEntityData : IComponentData
    {
        public float TransitionDuration;
        public AnimationTransitionType AnimationTransitionType;
    }
}
