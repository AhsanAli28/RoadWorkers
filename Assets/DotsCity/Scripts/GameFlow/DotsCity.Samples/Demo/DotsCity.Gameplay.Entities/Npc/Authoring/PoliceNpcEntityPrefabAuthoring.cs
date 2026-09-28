using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Npc.Authoring
{
    public class PoliceNpcEntityPrefabAuthoring : NpcEntityPrefabAuthoring
    {
        class PlayerNpcEntityPrefabBaker : Baker<PoliceNpcEntityPrefabAuthoring>
        {
            public override void Bake(PoliceNpcEntityPrefabAuthoring authoring)
            {
                DependsOn(authoring.physicsShapePrefab);
                NpcEntityPrefabAuthoring.Bake<PoliceNpcPrefabTag>(this, authoring);
            }
        }
    }
}