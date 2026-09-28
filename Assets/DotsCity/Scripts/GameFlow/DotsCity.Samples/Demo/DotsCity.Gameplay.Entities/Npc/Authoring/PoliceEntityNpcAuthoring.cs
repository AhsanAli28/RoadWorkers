using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Npc.Authoring
{
    public class PoliceEntityNpcAuthoring : MonoBehaviour
    {
        public class PoliceEntityNpcBaker : Baker<PoliceEntityNpcAuthoring>
        {
            public override void Bake(PoliceEntityNpcAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, typeof(EnemyNpcTag));
                AddComponent(entity, typeof(EnemyNpcTargetComponent));
            }
        }
    }
}