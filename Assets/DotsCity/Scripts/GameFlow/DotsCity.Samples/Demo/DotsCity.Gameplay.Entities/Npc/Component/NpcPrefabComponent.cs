using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Npc
{
    public struct NpcPrefabComponent : IComponentData
    {
        public Entity PrefabEntity;
    }

    public struct PlayerNpcPrefabTag : IComponentData { }
    public struct PlayerMobNpcPrefabTag : IComponentData { }
    public struct PoliceNpcPrefabTag : IComponentData { }
}