using Spirit604.DotsCity.Simulation.Npc;
using Spirit604.DotsCity.Simulation.Npc.Navigation;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian.Authoring
{
    public class PedestrianSettingsBaker : Baker<PedestrianSpawnerConfigHolder>
    {
        public override void Bake(PedestrianSpawnerConfigHolder authoring)
        {
            var entity = CreateAdditionalEntity(TransformUsageFlags.None);

            AddComponent(entity, PedestrianMiscConversionSettingsAuthoring.CreateConfigStatic(this, authoring));
        }
    }

    public struct MiscConversionSettings
    {
        public NpcSkinType PedestrianSkinType;
        public float PedestrianColliderRadius;
        public bool HasRig;
        public NpcRigType PedestrianRigType;
        public EntityType EntityType;
        public NpcNavigationType PedestrianNavigationType;
        public ObstacleAvoidanceType ObstacleAvoidanceType;
        public CollisionType CollisionType;
        public bool HasRagdoll;
        public RagdollType RagdollType;
        public bool AutoAddAgentComponents;

        public bool DefaultRagdollSystem => RagdollType == RagdollType.Default;
        public bool HybridSkin => PedestrianRigType == NpcRigType.HybridLegacy || PedestrianRigType == NpcRigType.HybridAndGPU || PedestrianRigType == NpcRigType.HybridOnRequestAndGPU;
    }

    public struct MiscConversionSettingsReference : IComponentData
    {
        public BlobAssetReference<MiscConversionSettings> Config;
    }
}
