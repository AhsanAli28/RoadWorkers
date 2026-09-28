using Spirit604.DotsCity.Simulation.VFX;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Level.Props
{
    public struct PropsComponent : IComponentData
    {
        public float3 InitialPosition;
        public float3 InitialForward;
    }

    public struct PropsProcessDamageTag : IComponentData, IEnableableComponent
    {
    }

    public struct PropsDamagedTag : IComponentData, IEnableableComponent
    {
    }

    public struct PropsVFXData : IComponentData
    {
        public Entity RelatedEntity;
        public VFXType VFXType;
    }
}
