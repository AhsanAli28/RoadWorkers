using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using System;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car.Authoring
{
    public class CarStoppingEngineConfigAuthoring : RuntimeConfigUpdater<CarStopEngineConfigReference, CarStopEngineConfig>
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/carCommonConfigs.html#car-stopping-engine-config")]
        [SerializeField] private string link;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField] private bool hasStopEngine = true;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][Range(0.1f, 25f)] private float stoppingDuration = 1f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][Range(0, 25f)] private float idleAfterStopping = 0.5f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][Range(0f, 1f)] private float targetMinPitch = 0.1f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][Range(0f, 1f)] private float targetMinVolume = 0f;

        protected override bool UpdateAvailableByDefault => false;

        public override CarStopEngineConfigReference CreateConfig(BlobAssetReference<CarStopEngineConfig> blobRef)
        {
            return new CarStopEngineConfigReference() { Config = blobRef };
        }

        protected override BlobAssetReference<CarStopEngineConfig> CreateConfigBlob()
        {
            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<CarStopEngineConfig>();

                root.HasStopEngine = hasStopEngine;
                root.StoppingDuration = stoppingDuration;
                root.IdleAfterStopping = idleAfterStopping;
                root.TargetMinPitch = targetMinPitch;
                root.TargetMinVolume = targetMinVolume;

                return builder.CreateBlobAssetReference<CarStopEngineConfig>(Unity.Collections.Allocator.Persistent);
            }
        }

        class CarStoppingEngineConfigAuthoringBaker : Baker<CarStoppingEngineConfigAuthoring>
        {
            public override void Bake(CarStoppingEngineConfigAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.CreateConfig(this));
            }
        }
    }
}