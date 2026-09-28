using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.TestScene
{
    public class FinishPointAuthoring : MonoBehaviour
    {
        public class FinishPointAuthoringBaker : Baker<FinishPointAuthoring>
        {
            public override void Bake(FinishPointAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                AddComponent(entity, new FinishPointTag());
            }
        }
    }
}
