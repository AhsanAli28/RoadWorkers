using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Core.Authoring
{
    [DisallowMultipleComponent]
    public class EntityTrackerAuthoring : MonoBehaviour
    {
        [SerializeField]
        private bool addPoolableComponent;

        class EntityFollowerAuthoringBaker : Baker<EntityTrackerAuthoring>
        {
            public override void Bake(EntityTrackerAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);

                AddComponent(entity, typeof(EntityTrackerComponent));

                if (authoring.addPoolableComponent)
                {
                    PoolEntityUtils.AddPoolComponents(this, entity);
                }
            }
        }
    }
}