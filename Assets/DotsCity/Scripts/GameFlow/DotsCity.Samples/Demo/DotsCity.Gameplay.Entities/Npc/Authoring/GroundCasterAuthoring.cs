using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Npc.Authoring
{
    public class GroundCasterAuthoring : MonoBehaviour
    {
        public class GroundCasterAuthoringBaker : Baker<GroundCasterAuthoring>
        {
            public override void Bake(GroundCasterAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

                AddComponent(entity, new GroundCasterComponent()
                {
                });
            }
        }
    }
}