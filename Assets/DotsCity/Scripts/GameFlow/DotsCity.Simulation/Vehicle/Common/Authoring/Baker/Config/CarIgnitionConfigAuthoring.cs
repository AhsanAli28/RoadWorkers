using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using System;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car.Authoring
{
    public class CarIgnitionConfigAuthoring : RuntimeConfigUpdater<CarIgnitionConfigReference, CarIgnitionConfig>
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/carCommonConfigs.html#car-ignition-config")]
        [SerializeField] private string link;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("On/off ignition state of the car when the NPCs enters the car")]
        [SerializeField] private bool hasIgnition = true;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Idle before starting ignition")]
        [SerializeField][Range(0.0f, 25f)] private float idleBeforeStart = 2f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Ignition duration")]
        [SerializeField][Range(0.1f, 25f)] private float ignitionDuration = 4f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Time to end of ignition state after which the engine start sound is emitted (if value = 0, engine start sound is not emitted)")]
        [SerializeField][Range(0, 25f)] private float engineStartedTimeDuration = 1.2f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Max pitch of the ignition sound")]
        [SerializeField][Range(0.1f, 5f)] private float maxPitch = 1.2f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Max volume of ignition sound")]
        [SerializeField][Range(0.1f, 5f)] private float maxVolume = 1.5f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Count of steps in the blob curve")]
        [SerializeField][Range(10, 400)] int curveBlobStepsCount = 200;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Pitch ignition curve. Y - pitch value. X - normalized duration time")]
        [SerializeField] private AnimationCurve pitchAnimationCurve;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Volume ignition curve. Y - volume value. X - normalized duration time")]
        [SerializeField] private AnimationCurve volumeAnimationCurve;

        protected override bool UpdateAvailableByDefault => false;

        public override CarIgnitionConfigReference CreateConfig(BlobAssetReference<CarIgnitionConfig> blobRef)
        {
            return new CarIgnitionConfigReference() { Config = blobRef };
        }

        protected override BlobAssetReference<CarIgnitionConfig> CreateConfigBlob()
        {
            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<CarIgnitionConfig>();

                root.HasIgnition = hasIgnition;
                root.IdleBeforeStart = idleBeforeStart;
                root.IgnitionDuration = ignitionDuration;
                root.StartedTimeDuration = engineStartedTimeDuration;
                root.MaxPitch = maxPitch;
                root.MaxVolume = maxVolume;

                var sourcePitchArray = BlobCurveUtils.GenerateCurveArray(pitchAnimationCurve, curveBlobStepsCount);
                var sourceVolumeArray = BlobCurveUtils.GenerateCurveArray(volumeAnimationCurve, curveBlobStepsCount);

                var pitchArray = builder.Allocate(ref root.EngineStartedPitchCurve, sourcePitchArray.Length);

                for (int i = 0; i < sourcePitchArray.Length; i++)
                {
                    pitchArray[i] = sourcePitchArray[i];
                }

                var volumeArray = builder.Allocate(ref root.EngineStartedVolumeCurve, sourceVolumeArray.Length);

                for (int i = 0; i < sourceVolumeArray.Length; i++)
                {
                    volumeArray[i] = sourceVolumeArray[i];
                }

                return builder.CreateBlobAssetReference<CarIgnitionConfig>(Unity.Collections.Allocator.Persistent);
            }
        }

        class CarIgnitionConfigAuthoringBaker : Baker<CarIgnitionConfigAuthoring>
        {
            public override void Bake(CarIgnitionConfigAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.CreateConfig(this));
            }
        }
    }
}