using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using System;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic.Authoring
{
    public class TrafficApproachConfigAuthoring : RuntimeConfigUpdater<TrafficApproachConfigReference, TrafficApproachConfig>
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/trafficCarConfigs.html#traffic-approach-config")]
        [SerializeField] private string link;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Min approach speed")]
        [SerializeField][Range(0.1f, 60f)] private float minApproachSpeed = 18f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Min approach speed soft at the long distance to obstacle")]
        [SerializeField][Range(0.1f, 60f)] private float minApproachSpeedSoft = 40f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Slowing down the speed of the car when approaching a red light (if the segment speed limit is lower or the speed of the obstacles is lower, the lowest speed of all the conditions will be selected)")]
        [SerializeField][Range(0.1f, 60f)] private float onComingToRedLightSpeed = 18f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Distance at which the car slows down")]
        [SerializeField][Range(0.1f, 45f)] private float stoppingDistanceToLight = 15f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("The car automatically brakes to the new speed limit at the selected distance")]
        [SerializeField] private bool autoBrakeBeforeSpeedLimit = true;

        [ShowIf(nameof(autoBrakeBeforeSpeedLimit))]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Soft braking distance to new speed limit")]
        [SerializeField][Range(0f, 45f)] private float softBrakingDistance = 20f;

        [ShowIf(nameof(autoBrakeBeforeSpeedLimit))]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Soft braking rate when the car is at a soft distance (if it is closer soft distance than the default braking rate)s")]
        [SerializeField][Range(0f, 1f)] private float softBrakingRate = 0.6f;

        [ShowIf(nameof(autoBrakeBeforeSpeedLimit))]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Braking distance to new speed limit")]
        [SerializeField][Range(1f, 45f)] private float brakingDistance = 10f;

        [ShowIf(nameof(autoBrakeBeforeSpeedLimit))]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("If the next path is too short, the next one is selected")]
        [SerializeField][Range(0.1f, 15f)] private float skipBrakingPathLength = 5f;

        protected override bool UpdateAvailableByDefault => false;

        public override TrafficApproachConfigReference CreateConfig(BlobAssetReference<TrafficApproachConfig> blobRef)
        {
            return new TrafficApproachConfigReference() { Config = blobRef };
        }

        protected override BlobAssetReference<TrafficApproachConfig> CreateConfigBlob()
        {
            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<TrafficApproachConfig>();

                root.MinApproachSpeed = minApproachSpeed / ProjectConstants.KmhToMs_RATE;
                root.MinApproachSpeedSoft = minApproachSpeedSoft / ProjectConstants.KmhToMs_RATE;
                root.OnComingToRedLightSpeed = onComingToRedLightSpeed / ProjectConstants.KmhToMs_RATE;
                root.StoppingDistanceToLight = stoppingDistanceToLight;
                root.AutoBrakeBeforeSpeedLimit = autoBrakeBeforeSpeedLimit;
                root.SoftBrakingDistance = softBrakingDistance;
                root.SoftBrakingRate = softBrakingRate;
                root.BrakingDistance = brakingDistance;
                root.SkipBrakingPathLength = skipBrakingPathLength;

                return builder.CreateBlobAssetReference<TrafficApproachConfig>(Unity.Collections.Allocator.Persistent);
            }
        }

        class TrafficApproachConfigAuthoringBaker : Baker<TrafficApproachConfigAuthoring>
        {
            public override void Bake(TrafficApproachConfigAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.CreateConfig(this));
            }
        }
    }
}