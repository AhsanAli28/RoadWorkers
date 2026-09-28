using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Npc
{
    public struct AnimatorStateComponent : IComponentData
    {
        public bool IsFalling;
        public bool ShortFalling;
        public bool StartedLanding;
        public bool IsLanded;
    }
}
