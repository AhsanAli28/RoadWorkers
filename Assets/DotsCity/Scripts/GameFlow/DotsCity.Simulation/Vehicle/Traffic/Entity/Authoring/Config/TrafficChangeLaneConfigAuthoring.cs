using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using System;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic.Authoring
{
    public class TrafficChangeLaneConfigAuthoring : RuntimeConfigUpdater<TrafficChangeLaneConfigReference, TrafficChangeLaneConfig>
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/trafficCarConfigs.html#traffic-change-lane-config")]
        [SerializeField] private string link;

        [Header("Common")]

        [GeneralOption("changeLaneSupport")]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Minimum/maximum offset on the target lane depending on vehicle speed. Max value if vehicle has DefaultLaneSpeed (default 60 km/h), if higher than default speed then more than max lane offset value will be interpolated")]
        [SerializeField][MinMaxSlider(0.0f, 30.0f)] private Vector2 minMaxChangeLaneOffset = new Vector2(8, 14f);

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Maximum distance before the end of a current path at which car can change lanes")]
        [SerializeField][Range(0, 25f)] private float maxDistanceToEndOfPath = 6f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Minimum distance to the last car in the current lane")]
        [SerializeField][Range(0, 35f)] private float minDistanceToLastCarInLane = 18f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Distance to the car in the target lane, the distance is chosen based on the current speed of the calculated car (lerp between 0 speed and max speed of the car (60 km/h by default))")]
        [SerializeField][MinMaxSlider(0.0f, 30.0f)] private Vector2 targetLaneCarDistance = new Vector2(10f, 20f);

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("If the car is too close to the crossing, the ability to change lanes is disabled")]
        [SerializeField] private bool checkTheIntersectedPaths = true;

        [ShowIf(nameof(checkTheIntersectedPaths))]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("If the intersecting path doesn't have a car, the intersecting path is ignored")]
        [SerializeField] private bool ignoreEmptyIntersects = true;

        [ShowIf(nameof(checkTheIntersectedPaths))]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Distance to the crossing, if the car is close to the crossing, the ability to change lanes is disabled")]
        [SerializeField][Range(0, 25f)] private float maxDistanceToIntersectedPath = 8f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Frequency of lane change calculation")]
        [SerializeField][MinMaxSlider(0, 25f)] private Vector2 checkFrequency = new Vector2(0.5f, 1.5f);

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Blocking the ability to change lanes after a lane change has been performed")]
        [SerializeField][Range(0, 25f)] private float blockDurationAfterChangeLane = 10f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Distance to achieve the target lane point")]
        [SerializeField][Range(0, 25f)] private float achieveDistance = 2f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Initial capacity hashmap containing data about cars that change lanes")]
        [SerializeField][Range(0, 20000)] private int changeLaneHashMapCapacity = 20;

        [Header("Lane Diff")]

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Minimum number of cars in the current lane to change lanes")]
        [SerializeField][Range(0, 10)] private int minCarsToChangeLane = 1;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Minimum car difference in the nearest lane to change lanes")]
        [SerializeField][Range(0, 10)] private int minCarDiffToChangeLane = 1;

        [Header("Speed Diff")]

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("If the speed of the front car is less than the set value (km/h), the current car will change lanes. If value is zero, value is not used")]
        [SerializeField][Range(0f, 50f)] private float lastCarSpeedDiff = 10;

        [Header("Random")]

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("How often try to invoke random event of lane change if chance is more than 0")]
        [SerializeField][Range(1f, 60f)] private float randomChangeLaneFrequency = 10f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("When a random event has occurred at the set frequency for a lane change. If the value is zero, the random lane change is disabled")]
        [SerializeField][Range(0f, 1f)] private float randomChangeLaneChance = 0;

        protected override bool UpdateAvailableByDefault => false;

        public override TrafficChangeLaneConfigReference CreateConfig(BlobAssetReference<TrafficChangeLaneConfig> blobRef)
        {
            return new TrafficChangeLaneConfigReference() { Config = blobRef };
        }

        protected override BlobAssetReference<TrafficChangeLaneConfig> CreateConfigBlob()
        {
            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<TrafficChangeLaneConfig>();

                root.MinChangeLaneOffset = minMaxChangeLaneOffset.x;
                root.MaxChangeLaneOffset = minMaxChangeLaneOffset.y;
                root.MaxDistanceToEndOfPath = maxDistanceToEndOfPath;
                root.MinDistanceToLastCarInLane = minDistanceToLastCarInLane;
                root.MinTargetLaneCarDistance = targetLaneCarDistance.x;
                root.MaxTargetLaneCarDistance = targetLaneCarDistance.y;
                root.CheckTheIntersectedPaths = checkTheIntersectedPaths;
                root.IgnoreEmptyIntersects = ignoreEmptyIntersects;
                root.MaxDistanceToIntersectedPathSQ = maxDistanceToIntersectedPath * maxDistanceToIntersectedPath;
                root.MinCheckFrequency = checkFrequency.x;
                root.MaxCheckFrequency = checkFrequency.y;
                root.BlockDurationAfterChangeLane = blockDurationAfterChangeLane;
                root.AchieveDistanceSQ = achieveDistance * achieveDistance;
                root.MinCarsToChangeLane = minCarsToChangeLane;
                root.MinCarDiffToChangeLane = minCarDiffToChangeLane;
                root.ChangeLaneHashMapCapacity = changeLaneHashMapCapacity;
                root.MaxSpeedDiff = lastCarSpeedDiff / ProjectConstants.KmhToMs_RATE;
                root.RandomChangeLaneChance = randomChangeLaneChance;
                root.RandomChangeLaneFrequency = randomChangeLaneFrequency;

                return builder.CreateBlobAssetReference<TrafficChangeLaneConfig>(Unity.Collections.Allocator.Persistent);
            }
        }

        class TrafficChangeLaneConfigAuthoringBaker : Baker<TrafficChangeLaneConfigAuthoring>
        {
            public override void Bake(TrafficChangeLaneConfigAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.CreateConfig(this));
            }
        }
    }
}