using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian.Authoring
{
    public class PedestrianMiscConversionSettingsAuthoring : RuntimeConfigUpdater<MiscConversionSettingsReference, MiscConversionSettings>
    {
        [ShowIf(nameof(SettingsIsNull))]
        [SerializeField]
        private PedestrianSpawnerConfigHolder pedestrianSpawnerConfigHolder;

        private bool SettingsIsNull => !pedestrianSpawnerConfigHolder;
        protected override bool AutoSync => false;

        public override MiscConversionSettingsReference CreateConfig(BlobAssetReference<MiscConversionSettings> blobRef)
        {
            return new MiscConversionSettingsReference() { Config = blobRef };
        }

        protected override BlobAssetReference<MiscConversionSettings> CreateConfigBlob()
        {
            return CreateBlobStatic(pedestrianSpawnerConfigHolder);
        }

        public static MiscConversionSettingsReference CreateConfigStatic(IBaker baker, PedestrianSpawnerConfigHolder pedestrianSpawnerConfigHolder)
        {
            var blobRef = CreateBlobStatic(pedestrianSpawnerConfigHolder);

            baker.AddBlobAsset(ref blobRef, out var hash);

            return new MiscConversionSettingsReference() { Config = blobRef };
        }

        private static BlobAssetReference<MiscConversionSettings> CreateBlobStatic(PedestrianSpawnerConfigHolder pedestrianSpawnerConfigHolder)
        {
            var config = pedestrianSpawnerConfigHolder.PedestrianSettingsConfig;

            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<MiscConversionSettings>();

                root.PedestrianSkinType = config.PedestrianSkinType;
                root.PedestrianColliderRadius = config.ColliderRadius;
                root.PedestrianNavigationType = config.PedestrianNavigationType;
                root.HasRig = config.HasRig;
                root.PedestrianRigType = config.PedestrianRigType;
                root.EntityType = config.PedestrianEntityType;
                root.ObstacleAvoidanceType = config.ObstacleAvoidanceType;
                root.CollisionType = config.PedestrianCollisionType;
                root.HasRagdoll = config.HasRagdoll;
                root.RagdollType = config.RagdollType;
                root.AutoAddAgentComponents = config.AutoAddAgentComponents;

                return builder.CreateBlobAssetReference<MiscConversionSettings>(Unity.Collections.Allocator.Persistent);
            }
        }
    }
}
