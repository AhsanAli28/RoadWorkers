using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Config
{
    public class GeneralSettingsCommonRuntimeAuthoring : RuntimeConfigUpdater<CommonGeneralSettingsReference, CommonGeneralSettingsData>
    {
        [ShowIfNull]
        [SerializeField] private CitySettingsInitializerBase settingsInitializer;

        protected override bool Updatable => false;

        public override CommonGeneralSettingsReference CreateConfig(BlobAssetReference<CommonGeneralSettingsData> blobRef)
        {
            return new CommonGeneralSettingsReference() { Config = blobRef };
        }

        protected override BlobAssetReference<CommonGeneralSettingsData> CreateConfigBlob()
        {
            return CreateBlobConfigStatic(settingsInitializer.GetSettings<GeneralSettingDataSimulation>());
        }

        public override CommonGeneralSettingsReference CreateConfig()
        {
            return CreateConfigStatic(settingsInitializer.GetSettings<GeneralSettingDataSimulation>());
        }

        public static BlobAssetReference<CommonGeneralSettingsData> CreateBlobConfigStatic(GeneralSettingDataSimulation settings)
        {
            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<CommonGeneralSettingsData>();

                root.BulletSupport = settings.BulletSupport;
                root.PropsPhysics = settings.PropsPhysics;
                root.HealthSupport = settings.HealthSystemSupport;
                root.PropsDamageSupport = settings.PropsDamageSystemSupport;

                var blobRef = builder.CreateBlobAssetReference<CommonGeneralSettingsData>(Unity.Collections.Allocator.Persistent);

                return blobRef;
            }
        }

        public static CommonGeneralSettingsReference CreateConfigStatic(IBaker baker, GeneralSettingDataSimulation settings)
        {
            var blobRef = CreateBlobConfigStatic(settings);

            baker.AddBlobAsset(ref blobRef, out var hash);

            return new CommonGeneralSettingsReference() { Config = blobRef };
        }

        public static CommonGeneralSettingsReference CreateConfigStatic(GeneralSettingDataSimulation settings)
        {
            var blobRef = CreateBlobConfigStatic(settings);

            return new CommonGeneralSettingsReference() { Config = blobRef };
        }
    }
}
