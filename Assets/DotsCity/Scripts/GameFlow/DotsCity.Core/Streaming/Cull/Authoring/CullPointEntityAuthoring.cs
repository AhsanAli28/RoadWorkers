using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Core.Authoring
{
    public class CullPointEntityAuthoring : MonoBehaviour
    {
        public class CullPointEntityAuthoringBaker : Baker<CullPointEntityAuthoring>
        {
            public override void Bake(CullPointEntityAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, typeof(CullPointTag));
            }
        }
    }
}
