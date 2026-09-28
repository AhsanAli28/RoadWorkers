using Spirit604.Attributes;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Core
{
    public class GeneralCoreSettingsRuntimeAuthoring : RuntimeConfigUpdater<GeneralCoreSettingsDataReference, GeneralCoreSettingsData>
    {
        [ShowIfNull]
        [SerializeField] private CitySettingsInitializerBase settingsInitializer;

        protected override bool Updatable => false;
        protected override bool AutoSync => false;

        public override GeneralCoreSettingsDataReference CreateConfig(BlobAssetReference<GeneralCoreSettingsData> blobRef)
        {
            return new GeneralCoreSettingsDataReference() { Config = blobRef };
        }

        protected override BlobAssetReference<GeneralCoreSettingsData> CreateConfigBlob()
        {
            return CreateBlobConfigStatic(settingsInitializer.GetSettings<GeneralSettingDataCore>());
        }

        public override GeneralCoreSettingsDataReference CreateConfig()
        {
            return CreateConfigStatic(settingsInitializer.GetSettings<GeneralSettingDataCore>());
        }

        public static BlobAssetReference<GeneralCoreSettingsData> CreateBlobConfigStatic(GeneralSettingDataCore settings)
        {
            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<GeneralCoreSettingsData>();

                root.WorldSimulationType = settings.WorldSimulationType;
                root.SimulationType = settings.SimulationType;
                root.CullPhysics = settings.CullPhysics;
                root.CullStaticPhysics = settings.CullStaticPhysics;

                var blobRef = builder.CreateBlobAssetReference<GeneralCoreSettingsData>(Unity.Collections.Allocator.Persistent);

                return blobRef;
            }
        }

        public static GeneralCoreSettingsDataReference CreateConfigStatic(IBaker baker, GeneralSettingDataCore settings)
        {
            var blobRef = CreateBlobConfigStatic(settings);

            baker.AddBlobAsset(ref blobRef, out var hash);

            return new GeneralCoreSettingsDataReference() { Config = blobRef };
        }

        public static GeneralCoreSettingsDataReference CreateConfigStatic(GeneralSettingDataCore settings)
        {
            var blobRef = CreateBlobConfigStatic(settings);

            return new GeneralCoreSettingsDataReference() { Config = blobRef };
        }
    }
}
