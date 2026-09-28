using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.Extensions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic.Authoring
{
    public class TrafficSettingsAuthoring : RuntimeConfigUpdater<TrafficSettingsConfigBlobReference, TrafficSettingsConfigBlob>
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

        public override TrafficSettingsConfigBlobReference CreateConfig(BlobAssetReference<TrafficSettingsConfigBlob> blobRef)
        {
            return new TrafficSettingsConfigBlobReference()
            {
                Reference = CreateBlobStatic(trafficSettings)
            };
        }

        protected override BlobAssetReference<TrafficSettingsConfigBlob> CreateConfigBlob()
        {
            return CreateBlobStatic(trafficSettings);
        }

        public static TrafficSettingsConfigBlobReference CreateConfigStatic(IBaker baker, TrafficSettings trafficSettings)
        {
            var blobRef = CreateBlobStatic(trafficSettings);

            baker.AddBlobAsset(ref blobRef, out var hash);

            return new TrafficSettingsConfigBlobReference()
            {
                Reference = blobRef
            };
        }

        public static BlobAssetReference<TrafficSettingsConfigBlob> CreateBlobStatic(TrafficSettings trafficSettings)
        {
            var builder = new BlobBuilder(Allocator.Temp);

            ref var root = ref builder.ConstructRoot<TrafficSettingsConfigBlob>();

            root.MaxSpeed = trafficSettings.TrafficSettingsConfig.MaxCarSpeed;
            root.Acceleration = trafficSettings.TrafficSettingsConfig.Acceleration;
            root.BackwardAcceleration = trafficSettings.TrafficSettingsConfig.BackwardAcceleration;
            root.BrakePower = trafficSettings.TrafficSettingsConfig.BrakePower;
            root.BrakingRate = trafficSettings.TrafficSettingsConfig.BrakingRate;
            root.MaxSteerAngle = trafficSettings.TrafficSettingsConfig.MaxSteerAngle;
            root.MaxSteerDirectionAngle = trafficSettings.TrafficSettingsConfig.MaxSteerDirectionAngle;
            root.UseSteeringDamping = trafficSettings.TrafficSettingsConfig.UseSteeringDamping;
            root.SteeringDamping = trafficSettings.TrafficSettingsConfig.SteeringDamping;
            root.LimitSteeringPower = trafficSettings.TrafficSettingsConfig.LimitSteeringPower;

            ref var steerCurve = ref builder.Allocate<AnimationCurveBlob>(ref root.SteeringPowerCurve);

            var curve = new AnimationCurve();

            var sourceCurve = trafficSettings.TrafficSettingsConfig.SteeringPowerCurve;

            curve.CopyFrom(sourceCurve);
            curve.ClearKeys();

            for (int i = 0; i < sourceCurve.keys.Length; i++)
            {
                Keyframe key = sourceCurve.keys[i];

                key.time = key.time / ProjectConstants.KmhToMs_RATE;
                curve.AddKey(key);
            }

            AnimationCurveBlob.Build(ref builder, ref steerCurve, curve, 200);

            root.HasRotationLerp = trafficSettings.TrafficSettingsConfig.HasRotationLerp;
            root.HealthCount = trafficSettings.TrafficSettingsConfig.HealthCount;
            root.HasNavMeshObstacle = trafficSettings.TrafficSettingsConfig.HasNavObstacle;

            var blobRef = builder.CreateBlobAssetReference<TrafficSettingsConfigBlob>(Allocator.Persistent);

            builder.Dispose();

            return blobRef;
        }

#if UNITY_EDITOR
        private void TrafficSettingsConfig_OnTrafficSettingsChanged()
        {
            ConfigUpdated = true;
        }
#endif
    }
}