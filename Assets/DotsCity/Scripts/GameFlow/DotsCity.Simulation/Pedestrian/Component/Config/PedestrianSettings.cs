using Spirit604.DotsCity.Simulation.Npc.Navigation;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct PedestrianSettings
    {
        public float MinWalkingSpeed;
        public float MaxWalkingSpeed;
        public float MinRunningSpeed;
        public float MaxRunningSpeed;
        public float RotationSpeed;
        public float ColliderRadius;
        public int Health;
        public NpcNavigationType NavigationType;
        public NpcSkinType SkinType;
        public EntityType EntityType;
        public NpcRigType RigType;
        public bool HasRig;
        public bool LerpRotation;
        public bool LerpRotationInView;
        public int MaxSkinIndex;
        public bool AlignYAxis;
        public float YAxisLerpSpeed;

        public bool HasLegacySkin => RigType == NpcRigType.HybridLegacy || RigType == NpcRigType.HybridAndGPU || RigType == NpcRigType.HybridOnRequestAndGPU;
        public bool HasGPUSkin => RigType == NpcRigType.PureGPU || RigType == NpcRigType.HybridAndGPU || RigType == NpcRigType.HybridOnRequestAndGPU || RigType == NpcRigType.HybridShapeGPU;
        public bool Physics => EntityType == EntityType.Physics;

        public float GetRandomWalkingSpeed(Random rnd) => rnd.NextFloat(MinWalkingSpeed, MaxWalkingSpeed);
        public float GetRandomRunningSpeed(Random rnd) => rnd.NextFloat(MinRunningSpeed, MaxRunningSpeed);
    }

    public struct PedestrianSettingsReference : IComponentData
    {
        public BlobAssetReference<PedestrianSettings> Config;
    }
}
