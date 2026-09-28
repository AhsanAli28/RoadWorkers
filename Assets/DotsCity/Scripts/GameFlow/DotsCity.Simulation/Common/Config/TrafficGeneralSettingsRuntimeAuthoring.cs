using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Config;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public class TrafficGeneralSettingsRuntimeAuthoring : RuntimeConfigUpdater<TrafficGeneralSettingsReference, TrafficGeneralSettingsData>
    {
        [ShowIfNull]
        [SerializeField] private CitySettingsInitializerBase settingsInitializer;

        protected override bool Updatable => false;
        protected override bool AutoSync => false;

        public override TrafficGeneralSettingsReference CreateConfig(BlobAssetReference<TrafficGeneralSettingsData> blobRef)
        {
            return new TrafficGeneralSettingsReference() { Config = blobRef };
        }

        protected override BlobAssetReference<TrafficGeneralSettingsData> CreateConfigBlob()
        {
            return CreateBlobConfigStatic(settingsInitializer.GetSettings<GeneralSettingDataSimulation>());
        }

        public static TrafficGeneralSettingsReference CreateConfigStatic(IBaker baker, GeneralSettingDataSimulation settings)
        {
            var blobRef = CreateBlobConfigStatic(settings);

            baker.AddBlobAsset(ref blobRef, out var hash);

            return new TrafficGeneralSettingsReference() { Config = blobRef };
        }

        private static BlobAssetReference<TrafficGeneralSettingsData> CreateBlobConfigStatic(GeneralSettingDataSimulation settings)
        {
            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<TrafficGeneralSettingsData>();

                root.EntityBakingType = settings.TrafficBakingType;
                root.HasTraffic = settings.HasTraffic;
                root.ChangeLaneSupport = settings.ChangeLaneSupport;
                root.AntiStuckSupport = settings.AntiStuckSupport;
                root.AvoidanceSupport = settings.AvoidanceSupport;
                root.RailSupport = settings.RailMovementSupport;
                root.CarVisualDamageSystemSupport = settings.CarVisualDamageSystemSupport;
                root.WheelSystemSupport = settings.WheelSystemSupport;
                root.VehicleLightSupport = settings.VehicleLightSupport;

                var blobRef = builder.CreateBlobAssetReference<TrafficGeneralSettingsData>(Unity.Collections.Allocator.Persistent);

                return blobRef;
            }
        }
    }
}
