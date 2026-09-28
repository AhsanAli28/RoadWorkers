using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Npc
{
    public struct AnimatorMovementComponent : IComponentData
    {
        public float CurrentSideLerp;
        public float CurrentForwardLerp;
        public float TargetForwardLerp;
    }
}
