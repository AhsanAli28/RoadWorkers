using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Initialization;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic.Authoring
{
    public class TrafficCommonSettingsAuthoring : RuntimeConfigUpdater<TrafficCommonSettingsConfigBlobReference, TrafficCommonSettingsConfigBlob>
    {
        [ShowIfNull]
        [SerializeField] private TrafficSettings trafficSettings;

        protected override bool UpdateAvailableByDefault => false;
        protected override bool AutoSync => false;

        public override void Initialize(bool recreateOnStart)
        {
            base.Initialize(recreateOnStart);

#if UNITY_EDITOR
            var trafficSettingsConfig = trafficSettings.TrafficSettingsConfig;
            trafficSettingsConfig.OnTrafficSettingsChanged += TrafficSettingsConfig_OnTrafficSettingsChanged;
#endif
        }

        public override TrafficCommonSettingsConfigBlobReference CreateConfig(BlobAssetReference<TrafficCommonSettingsConfigBlob> blobRef)
        {
            return new TrafficCommonSettingsConfigBlobReference()
            {
                Reference = CreateBlobStatic(trafficSettings)
            };
        }

        protected override BlobAssetReference<TrafficCommonSettingsConfigBlob> CreateConfigBlob()
        {
            return CreateBlobStatic(trafficSettings);
        }

        public static TrafficCommonSettingsConfigBlobReference CreateConfigStatic(IBaker baker, TrafficSettings trafficSettings)
        {
            var blobRef = CreateBlobStatic(trafficSettings);

            baker.AddBlobAsset(ref blobRef, out var hash);

            return new TrafficCommonSettingsConfigBlobReference()
            {
                Reference = blobRef
            };
        }

        private static BlobAssetReference<TrafficCommonSettingsConfigBlob> CreateBlobStatic(TrafficSettings trafficSettings)
        {
            var trafficSettingsConfig = trafficSettings.TrafficSettingsConfig;

            var hasRaycast = TrafficInitializer.HasRaycast(trafficSettingsConfig);

            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<TrafficCommonSettingsConfigBlob>();


                root.EntityType = trafficSettings.EntityType;
                root.DetectObstacleMode = trafficSettingsConfig.DetectObstacleMode;
                root.DetectNpcMode = trafficSettingsConfig.DetectNpcMode;
                root.HasRaycast = hasRaycast;
                root.CullWheels = trafficSettingsConfig.CullWheels;
                root.CullWheelSupported = trafficSettingsConfig.CullWheelSupported;
                root.DefaultLaneSpeed = trafficSettingsConfig.DefaultLaneSpeedMs;
                root.MaxSpeedSteerDirectionAngle = trafficSettingsConfig.MaxSpeedSteerDirectionAngle / ProjectConstants.KmhToMs_RATE;
                root.PhysicsSimulation = trafficSettingsConfig.PhysicsSimulation;
                root.SimplePhysicsType = trafficSettingsConfig.SimplePhysics;
                root.CullPhysics = trafficSettingsConfig.CullPhysics;

                return builder.CreateBlobAssetReference<TrafficCommonSettingsConfigBlob>(Unity.Collections.Allocator.Persistent);
            }
        }

#if UNITY_EDITOR
        private void TrafficSettingsConfig_OnTrafficSettingsChanged()
        {
            ConfigUpdated = true;
        }
#endif
    }
}
