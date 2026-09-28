using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic.Authoring
{
    public class TrafficRotationCurveAuthoring : RuntimeConfigUpdater<RotationCurveReference, CurveData>
    {
        private const int STEP_COUNT = 200;

        [ShowIf(nameof(TrafficSettingsIsNull))]
        [SerializeField] private TrafficSettings trafficSettings;

        private bool TrafficSettingsIsNull => !trafficSettings;
        protected override bool UpdateAvailableByDefault => false;

        public override void Initialize(bool recreateOnStart)
        {
            base.Initialize(recreateOnStart);

#if UNITY_EDITOR
            var trafficSettingsConfig = trafficSettings.TrafficSettingsConfig;
            trafficSettingsConfig.OnTrafficSettingsChanged += TrafficSettingsConfig_OnTrafficSettingsChanged;
#endif
        }

        public override RotationCurveReference CreateConfig(BlobAssetReference<CurveData> blobRef)
        {
            return new RotationCurveReference()
            {
                Curve = CreateBlobStatic(trafficSettings)
            };
        }

        protected override BlobAssetReference<CurveData> CreateConfigBlob()
        {
            return CreateBlobStatic(trafficSettings);
        }

        public static RotationCurveReference CreateConfigStatic(IBaker baker, TrafficSettings trafficSettings)
        {
            var blobRef = CreateBlobStatic(trafficSettings);

            baker.AddBlobAsset(ref blobRef, out var hash);

            return new RotationCurveReference()
            {
                Curve = blobRef
            };
        }

        private static BlobAssetReference<CurveData> CreateBlobStatic(TrafficSettings trafficSettings)
        {
            var curve = trafficSettings.TrafficSettingsConfig.RotationSpeedCurve;
            var array = BlobCurveUtils.GenerateCurveArray(curve, STEP_COUNT);

            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<CurveData>();

                root.RotationSpeed = trafficSettings.TrafficSettingsConfig.RotationSpeed;
                var valueArray = builder.Allocate(ref root.Values, array.Length);

                for (int i = 0; i < array.Length; i++)
                {
                    valueArray[i] = array[i];
                }

                return builder.CreateBlobAssetReference<CurveData>(Unity.Collections.Allocator.Persistent);
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