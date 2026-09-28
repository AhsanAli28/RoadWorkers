using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Gameplay.Initialization;
using Spirit604.Gameplay.InputService;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player
{
    public class PlayerTargetSettingsRuntimeAuthoring : RuntimeConfigUpdater<PlayerTargetSettingsReference, PlayerTargetSettings>
    {
        [ShowIfNull]
        [SerializeField] private CitySettingsInitializer settingsInitializer;

        private IInputSettings inputSettings;

        [InjectWrapper]
        public void Construct(IInputSettings inputSettings)
        {
            this.inputSettings = inputSettings;
        }

        protected override bool Updatable => false;
        protected override bool AutoSync => false;

        public override PlayerTargetSettingsReference CreateConfig(BlobAssetReference<PlayerTargetSettings> blobRef)
        {
            return new PlayerTargetSettingsReference() { Config = CreateBlobStatic(settingsInitializer.Settings.GetPlayerTargetSettings(inputSettings)) };
        }

        protected override BlobAssetReference<PlayerTargetSettings> CreateConfigBlob()
        {
            return CreateBlobStatic(settingsInitializer.Settings.GetPlayerTargetSettings(inputSettings));
        }

        public static PlayerTargetSettingsReference CreateConfigStatic(IBaker baker, PlayerTargetSettings settings)
        {
            var blobRef = CreateBlobStatic(settings);

            baker.AddBlobAsset(ref blobRef, out var hash);

            return new PlayerTargetSettingsReference() { Config = blobRef };
        }

        private static BlobAssetReference<PlayerTargetSettings> CreateBlobStatic(PlayerTargetSettings settings)
        {
            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<PlayerTargetSettings>();

                root.PlayerShootDirectionSource = settings.PlayerShootDirectionSource;
                root.MaxTargetDistanceSQ = settings.MaxTargetDistanceSQ;
                root.MaxCaptureAngle = settings.MaxCaptureAngle;
                root.DefaultAimPointDistance = settings.DefaultAimPointDistance;
                root.DefaultAimPointYPosition = settings.DefaultAimPointYPosition;

                return builder.CreateBlobAssetReference<PlayerTargetSettings>(Unity.Collections.Allocator.Persistent);
            }
        }
    }
}
