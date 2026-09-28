using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Npc
{
    public struct NpcCombatStateComponent : IComponentData
    {
        public bool IsShooting;

        /// <summary> Movement speed reduction factor while shooting. </summary>
        public float ReducationFactor;
    }
}
