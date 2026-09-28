using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Config;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public class PedestrianGeneralSettingsRuntimeAuthoring : RuntimeConfigUpdater<PedestrianGeneralSettingsReference, PedestrianGeneralSettingsData>
    {
        [ShowIfNull]
        [SerializeField] private CitySettingsInitializerBase settingsInitializer;

        protected override bool Updatable => false;
        protected override bool AutoSync => false;

        public override PedestrianGeneralSettingsReference CreateConfig(BlobAssetReference<PedestrianGeneralSettingsData> blobRef)
        {
            return new PedestrianGeneralSettingsReference() { Config = blobRef };
        }

        protected override BlobAssetReference<PedestrianGeneralSettingsData> CreateConfigBlob()
        {
            return CreateBlobStatic(settingsInitializer.GetSettings<GeneralSettingDataSimulation>());
        }

        public static PedestrianGeneralSettingsReference CreateConfigStatic(IBaker baker, GeneralSettingDataSimulation settings)
        {
            var blobRef = CreateBlobStatic(settings);

            baker.AddBlobAsset(ref blobRef, out var hash);

            return new PedestrianGeneralSettingsReference() { Config = blobRef };
        }

        private static BlobAssetReference<PedestrianGeneralSettingsData> CreateBlobStatic(GeneralSettingDataSimulation settings)
        {
            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<PedestrianGeneralSettingsData>();

                root.EntityBakingType = settings.PedestrianBakingType;
                root.HasPedestrian = settings.HasPedestrian;
                root.NavigationSupport = settings.NavigationSupport;
                root.ParkingSupport = settings.TrafficParkingSupport;
                root.TrafficPublicSupport = settings.TrafficPublicSupport;
                root.TalkingSupport = settings.TalkingSupport;
                root.BenchSystemSupport = settings.BenchSystemSupport;
                root.TriggerSupport = settings.PedestrianTriggerSystemSupport;

                return builder.CreateBlobAssetReference<PedestrianGeneralSettingsData>(Unity.Collections.Allocator.Persistent);
            }
        }
    }
}
