using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Core.Authoring
{
    [TemporaryBakingType]
    public struct BakingStaticPhysicsData : IComponentData
    {
        public bool PreinitEnabling;
    }

    public class CullPhysicsEntityAuthoring : MonoBehaviour
    {
        [SerializeField] private bool preinitEnabling;

        public bool PreinitEnabling { get => preinitEnabling; set => preinitEnabling = value; }

        public class CullPhysicsEntityAuthoringBaker : Baker<CullPhysicsEntityAuthoring>
        {
            public override void Bake(CullPhysicsEntityAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent<CullPhysicsTag>(entity);
                AddComponent(entity, new BakingStaticPhysicsData()
                {
                    PreinitEnabling = authoring.preinitEnabling
                });
            }
        }
    }
}
