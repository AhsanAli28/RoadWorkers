using Spirit604.DotsCity.Simulation.VFX;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Level.Props.Authoring
{
    public class HydrantAuthoring : MonoBehaviour
    {
        [SerializeField] private VFXType vfxType;

        class HydrantAuthoringBaker : Baker<HydrantAuthoring>
        {
            public override void Bake(HydrantAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, typeof(HydrantTag));

                AddComponent(entity, new PropsVFXData()
                {
                    VFXType = authoring.vfxType
                });
            }
        }
    }
}