using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Npc
{
    public struct NpcStateComponent : IComponentData
    {
        public bool IsGrounded;
        public bool IsLanded;
        public bool IsFalling;
    }
}