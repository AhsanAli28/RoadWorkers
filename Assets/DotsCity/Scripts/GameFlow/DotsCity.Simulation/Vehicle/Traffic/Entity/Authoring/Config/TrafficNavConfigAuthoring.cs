using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using System;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic.Authoring
{
    public class TrafficNavConfigAuthoring : RuntimeConfigUpdater<TrafficDestinationConfigReference, TrafficDestinationConfig>
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/commonConfigs.html#general-settings-config")]
        [SerializeField] private string link;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Min distance to target node")]
        [SerializeField][Range(0.1f, 8f)] private float minDistanceToTarget = 3.2f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Min distance to connected path point")]
        [SerializeField][Range(0.1f, 8f)] private float minDistanceToPathPointTarget = 2f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Minimum distance to the TrafficNode entity that contains the traffic light handler entity to assign it to the car entity (if the traffic node entity does not contain a traffic light entity, the index is -1)")]
        [SerializeField][Range(0.1f, 15f)] private float minDistanceToNewLight = 6.5f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Minimum distance from the TrafficNode entity that contains the traffic light handler entity to unassign it from the car entity (if the traffic node entity does not contain a traffic light entity, the index is -1)")]
        [SerializeField][Range(0.1f, 15f)] private float maxDistanceFromPreviousLight = 3.5f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Minimum distance to switch to the next waypoint of the path")]
        [SerializeField][Range(0.01f, 8f)] private float minDistanceToTargetRouteNode = 2f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Minimum distance to switch to the next waypoint of the path (trams only)")]
        [SerializeField][Range(0.01f, 4f)] private float minDistanceToTargetRailNode = 0.8f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Change the distance to a route node to be reached if the speed is greater than the default lane speed, a useful option on highways with high speed limits")]
        [SerializeField] private bool highSpeedRouteNodeCalc;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [ShowIf(nameof(highSpeedRouteNodeCalc))]
        [Tooltip("Final distance to route node is calculated by formula if current speed is greater than defaultLaneSpeed: minDistanceToTargetRouteNode = minDistanceToTargetRouteNode * (currentSpeed / defaultLaneSpeed * multiplier)")]
        [SerializeField][Range(1, 4f)] private float highSpeedRouteNodeMult = 2f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Resolving method in case the car is out of the path")]
        [SerializeField] private OutOfPathResolveMethod outOfPathMethod;

        [ShowIf(nameof(HasResolver))]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Minimum distance from the missed waypoint to the car")]
        [SerializeField][Range(0.01f, 10f)] private float minDistanceToOutOfPath = 2.5f;

        [ShowIf(nameof(HasResolver))]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Maximum distance from the missed waypoint to the car")]
        [SerializeField][Range(0.01f, 30f)] private float maxDistanceToOutOfPath = 7;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("The reaction if there is no further destination for the car")]
        [SerializeField] private NoDestinationReactType noDstReactType;

        private bool HasResolver => outOfPathMethod != OutOfPathResolveMethod.Disabled;

        protected override bool UpdateAvailableByDefault => false;

        public override TrafficDestinationConfigReference CreateConfig(BlobAssetReference<TrafficDestinationConfig> blobRef)
        {
            return new TrafficDestinationConfigReference() { Config = blobRef };
        }

        protected override BlobAssetReference<TrafficDestinationConfig> CreateConfigBlob()
        {
            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<TrafficDestinationConfig>();

                root.MinDistanceToTarget = minDistanceToTarget;
                root.MinDistanceToPathPointTarget = minDistanceToPathPointTarget;
                root.MinDistanceToNewLight = minDistanceToNewLight;
                root.MaxDistanceFromPreviousLightSQ = maxDistanceFromPreviousLight * maxDistanceFromPreviousLight;
                root.MinDistanceToTargetRouteNode = minDistanceToTargetRouteNode;
                root.MinDistanceToTargetRailRouteNode = minDistanceToTargetRailNode;
                root.HighSpeedRouteNodeCalc = highSpeedRouteNodeCalc;
                root.HighSpeedRouteNodeMult = highSpeedRouteNodeMult;
                root.OutOfPathMethod = outOfPathMethod;
                root.MinDistanceToOutOfPath = minDistanceToOutOfPath;
                root.MaxDistanceToOutOfPath = maxDistanceToOutOfPath;
                root.NoDestinationReact = noDstReactType;

                return builder.CreateBlobAssetReference<TrafficDestinationConfig>(Unity.Collections.Allocator.Persistent);
            }
        }

        class TrafficNavConfigAuthoringBaker : Baker<TrafficNavConfigAuthoring>
        {
            public override void Bake(TrafficNavConfigAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);

                AddComponent(entity, authoring.CreateConfig(this));
            }
        }
    }
}