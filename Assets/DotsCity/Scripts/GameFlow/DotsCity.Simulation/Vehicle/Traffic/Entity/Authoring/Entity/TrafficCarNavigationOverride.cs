using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic.Authoring
{
    [TemporaryBakingType]
    public struct TrafficDestinationBakingSharedConfig : IComponentData
    {
        public float MinDistanceToTarget;
        public float MinDistanceToPathPointTarget;
        public float MinDistanceToNewLight;
        public float MaxDistanceFromPreviousLightSQ;
        public float MinDistanceToTargetRouteNode;
        public float MinDistanceToTargetRailRouteNode;
    }

    public class TrafficCarNavigationOverride : MonoBehaviour
    {
        [Tooltip("Min distance to target node")]
        [SerializeField][Range(0.1f, 8f)] private float minDistanceToTarget = 3.2f;

        [Tooltip("Min distance to connected path point")]
        [SerializeField][Range(0.1f, 8f)] private float minDistanceToPathPointTarget = 2f;

        [Tooltip("Minimum distance to the TrafficNode entity that contains the traffic light handler entity to assign it to the car entity (if the traffic node entity does not contain a traffic light entity, the index is -1)")]
        [SerializeField][Range(0.1f, 15f)] private float minDistanceToNewLight = 6.5f;

        [Tooltip("Minimum distance from the TrafficNode entity that contains the traffic light handler entity to unassign it from the car entity (if the traffic node entity does not contain a traffic light entity, the index is -1)")]
        [SerializeField][Range(0.1f, 15f)] private float maxDistanceFromPreviousLight = 3.5f;

        [Tooltip("Minimum distance to switch to the next waypoint of the path")]
        [SerializeField][Range(0.01f, 8f)] private float minDistanceToTargetRouteNode = 2f;

        [Tooltip("Minimum distance to switch to the next waypoint of the path (for rail movement)")]
        [SerializeField][Range(0.01f, 4f)] private float minDistanceToTargetRailNode = 0.8f;

        class TrafficCarNavigationOverrideBaker : Baker<TrafficCarNavigationOverride>
        {
            public override void Bake(TrafficCarNavigationOverride authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new TrafficDestinationBakingSharedConfig()
                {
                    MinDistanceToTarget = authoring.minDistanceToTarget,
                    MinDistanceToPathPointTarget = authoring.minDistanceToPathPointTarget,
                    MinDistanceToNewLight = authoring.minDistanceToNewLight,
                    MaxDistanceFromPreviousLightSQ = authoring.maxDistanceFromPreviousLight * authoring.maxDistanceFromPreviousLight,
                    MinDistanceToTargetRouteNode = authoring.minDistanceToTargetRouteNode,
                    MinDistanceToTargetRailRouteNode = authoring.minDistanceToTargetRailNode,
                });
            }
        }
    }
}