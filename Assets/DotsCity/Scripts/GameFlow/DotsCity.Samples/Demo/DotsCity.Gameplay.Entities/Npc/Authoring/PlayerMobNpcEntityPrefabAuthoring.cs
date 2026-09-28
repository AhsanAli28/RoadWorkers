using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Npc.Authoring
{
    public class PlayerMobNpcEntityPrefabAuthoring : NpcEntityPrefabAuthoring
    {
        class PlayerMobNpcEntityPrefabBaker : Baker<PlayerMobNpcEntityPrefabAuthoring>
        {
            public override void Bake(PlayerMobNpcEntityPrefabAuthoring authoring)
            {
                DependsOn(authoring.physicsShapePrefab);
                NpcEntityPrefabAuthoring.Bake<PlayerMobNpcPrefabTag>(this, authoring);
            }
        }
    }
}
