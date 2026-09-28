using Spirit604.DotsCity.Core;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Common.Authoring
{
    public class VfxAuthoring : MonoBehaviour
    {
        [SerializeField] private ParticleSystem particleEffect;
        [SerializeField] private ParticleSystemRenderer particleSystemRenderer;

        class VfxAuthoringBaker : Baker<VfxAuthoring>
        {
            public override void Bake(VfxAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                PoolEntityUtils.AddPoolComponents(this, entity, EntityWorldType.PureEntity);
            }
        }
    }
}