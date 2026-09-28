using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Gameplay.Npc
{
    public struct NpcTargetComponent : IComponentData
    {
        public bool HasMovementTarget;
        public float3 MovementTargetPosition;
        public float MovementTargetDistance;

        public float3 ShootingTargetPosition;
        public float ShootingTargetDistance;
        public bool HasShootingTarget;
    }
}