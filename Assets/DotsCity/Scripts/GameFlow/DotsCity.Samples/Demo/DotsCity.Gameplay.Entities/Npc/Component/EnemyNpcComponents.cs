using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Npc
{
    public struct EnemyNpcTag : IComponentData { }

    public struct EnemyNpcTargetComponent : IComponentData
    {
        public Entity Target;
    }

    public struct EnemyNpcCombatStateTag : IComponentData { }
}
