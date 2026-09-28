using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Weapon
{
    public struct CrossHairComponent : IComponentData
    {
        public float TargetScale;
        public float CurrentScale;
    }

    public struct CrossHairUpdateScaleTag : IComponentData, IEnableableComponent
    {
    }
}