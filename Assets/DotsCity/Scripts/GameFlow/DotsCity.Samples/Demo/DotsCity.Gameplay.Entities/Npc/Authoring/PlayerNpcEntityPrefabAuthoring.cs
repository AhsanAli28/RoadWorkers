using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Npc.Authoring
{
    public class PlayerNpcEntityPrefabAuthoring : NpcEntityPrefabAuthoring
    {
        class PlayerNpcEntityPrefabBaker : Baker<PlayerNpcEntityPrefabAuthoring>
        {
            public override void Bake(PlayerNpcEntityPrefabAuthoring authoring)
            {
                DependsOn(authoring.physicsShapePrefab);
                NpcEntityPrefabAuthoring.Bake<PlayerNpcPrefabTag>(this, authoring);
            }
        }
    }
}
