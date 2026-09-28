using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Core.Authoring
{
    public class HealthAuthoring : MonoBehaviour
    {
        [SerializeField][Range(1, 1000)] private int maxHp = 4;

        class HealthAuthoringBaker : Baker<HealthAuthoring>
        {
            public override void Bake(HealthAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new HealthComponent(authoring.maxHp));
            }
        }
    }
}