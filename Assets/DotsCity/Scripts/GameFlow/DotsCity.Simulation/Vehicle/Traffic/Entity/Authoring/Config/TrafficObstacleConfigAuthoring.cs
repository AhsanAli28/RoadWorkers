using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.Extensions;
using System;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic.Authoring
{
    public class TrafficObstacleConfigAuthoring : RuntimeConfigUpdater<TrafficObstacleConfigReference, TrafficObstacleConfig>
    {
        private const string SettingsGroup = "Settings";

        private const float DefaultCarSize = 4f;

        private const float MaxDistanceToObstacle = 9f;
        private const float MinDistanceToStartApproach = 16f;
        private const float MinDistanceToStartApproachSoft = 32f;
        private const float MinDistanceToCheckNextPath = 20f;
        private const float ShortPathLength = 12f;
        private const float CalculateDistanceToIntesectPoint = 22f;
        private const float SizeOffsetToIntersectPoint = 1f;
        private const float StopDistanceBeforeIntersection = 10f;
        private const float StopDistanceForSameTargetNode = 14f;
        private const float CloseDistanceToChangeLanePoint = 7f;
        private const float MaxDistanceToObstacleChangeLane = 5f;
        private const float InFrontOfViewDot = 0.4f;
        private const float NeighboringDistance = 5f;

        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/trafficCarConfigs.html#traffic-obstacle-config")]
        [SerializeField] private string link;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Minimum distance to an obstacle")]
        [SerializeField][Range(0, 35f)] private float maxDistanceToObstacle = MaxDistanceToObstacle;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Minimum distance to the last car in the current lane to start approaching (stay at the same speed as the target car)")]
        [SerializeField][Range(0, 60f)] private float minDistanceToStartApproach = MinDistanceToStartApproach;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Minimum distance to the last car in the current lane to start approaching (long distance approach at a 'soft' speed)")]
        [SerializeField][Range(0, 60f)] private float minDistanceToStartApproachSoft = MinDistanceToStartApproachSoft;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Minimum distance to check the next path for obstacles")]
        [SerializeField][Range(0, 35f)] private float minDistanceToCheckNextPath = MinDistanceToCheckNextPath;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("If the next path is too short, start checking the next connected paths for obstacles")]
        [SerializeField][Range(0, 35f)] private float shortPathLength = ShortPathLength;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Distance to intersected paths when they are checked for obstacles")]
        [SerializeField][Range(0, 45f)] private float calculateDistanceToIntesectPoint = CalculateDistanceToIntesectPoint;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Method of calculating the intersection of the vehicle and the intersect point")]
        [SerializeField] private IntersectCalculationMethod intersectCalculation = IntersectCalculationMethod.Distance;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Additional offset to the length of the car bounds to check the closeness to the intersect point")]
        [SerializeField][Range(0, 10f)] private float sizeOffsetToIntersectPoint = SizeOffsetToIntersectPoint;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Car is close enough to stop in front of the intersect point if necessary")]
        [SerializeField][Range(0, 35f)] private float stopDistanceBeforeIntersection = StopDistanceBeforeIntersection;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Current car is close enough to stop in front if another car approaches the same target node but with a higher priority")]
        [SerializeField][Range(0, 35f)] private float stopDistanceForSameTargetNode = StopDistanceForSameTargetNode;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Car that is too close to the lane change point is always an obstacle")]
        [SerializeField][Range(0, 35f)] private float closeDistanceToChangeLanePoint = CloseDistanceToChangeLanePoint;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Maximum distance to the obstacle in the target change lane")]
        [SerializeField][Range(0, 35f)] private float maxDistanceToObstacleChangeLane = MaxDistanceToObstacleChangeLane;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Direction of the vehicle to check for obstacles in neighboring paths")]
        [SerializeField][Range(-1, 1f)] private float inFrontOfViewDot = InFrontOfViewDot;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Distance between the front point of the car and the waypoint of the current car path to detect neighbouring cars")]
        [SerializeField][Range(0.1f, 10f)] private float neighboringDistance = NeighboringDistance;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Car doesn't enter an crossroad if it cannot pass it without jamming")]
        [SerializeField] private bool avoidCrossroadJam = true;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("If the cars have the same priority, the car from the right has priority, otherwise, if this option is disabled, the car closer to the intersection has priority")]
        [SerializeField] private bool priorityToRight = true;

        [OnValueChanged(nameof(PrototypeCarMeshOnValueChanged))]
        [SerializeField] private MeshRenderer prototypeCarMesh;

        [ShowIf(nameof(HasPrototypeCarMesh))]
        [Range(0, 10f)]
        [SerializeField]
        private float carSize;

        [ShowIf(nameof(HasPrototypeCarMesh))]
        [Range(0, 10f)]
        [SerializeField]
        private float sizeMultiplier = 1.2f;

        private float sizeDifference;

        private bool HasPrototypeCarMesh => prototypeCarMesh;

        protected override bool UpdateAvailableByDefault => false;

        public override TrafficObstacleConfigReference CreateConfig(BlobAssetReference<TrafficObstacleConfig> blobRef)
        {
            return new TrafficObstacleConfigReference() { Config = blobRef };
        }

        protected override BlobAssetReference<TrafficObstacleConfig> CreateConfigBlob()
        {
            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<TrafficObstacleConfig>();

                root.MaxDistanceToObstacle = maxDistanceToObstacle;
                root.MinDistanceToStartApproach = minDistanceToStartApproach;
                root.MinDistanceToStartApproachSoft = minDistanceToStartApproachSoft;
                root.MinDistanceToCheckNextPath = minDistanceToCheckNextPath;
                root.ShortPathLength = shortPathLength;
                root.IntersectCalculation = intersectCalculation;
                root.CalculateDistanceToIntersectPoint = calculateDistanceToIntesectPoint;
                root.SizeOffsetToIntersectPoint = sizeOffsetToIntersectPoint;
                root.StopDistanceBeforeIntersection = stopDistanceBeforeIntersection;
                root.StopDistanceForSameTargetNode = stopDistanceForSameTargetNode;
                root.CloseDistanceToChangeLanePoint = closeDistanceToChangeLanePoint;
                root.MaxDistanceToObstacleChangeLane = maxDistanceToObstacleChangeLane;
                root.InFrontOfViewDot = inFrontOfViewDot;
                root.NeighboringDistanceSQ = neighboringDistance * neighboringDistance;
                root.AvoidCrossroadJam = avoidCrossroadJam;
                root.PriorityToRight = priorityToRight;

                return builder.CreateBlobAssetReference<TrafficObstacleConfig>(Unity.Collections.Allocator.Persistent);
            }
        }

        class TrafficObstacleConfigAuthoringBaker : Baker<TrafficObstacleConfigAuthoring>
        {
            public override void Bake(TrafficObstacleConfigAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.CreateConfig(this));
            }
        }

        private float GetNewValue(float sourceValue)
        {
            return (float)System.Math.Round(sourceValue + sizeDifference, 1);
        }

        private float GetNewValue2(float sourceValue, float multiplier)
        {
            return (float)System.Math.Round(sourceValue * multiplier, 1);
        }

        private void PrototypeCarMeshOnValueChanged()
        {
            if (prototypeCarMesh != null)
            {
                carSize = prototypeCarMesh.bounds.size.z;
            }
        }

        [Button]
        private void Recalculate()
        {
            if (prototypeCarMesh == null)
            {
                UnityEngine.Debug.Log("Select TargetCarMesh");
                return;
            }

#if UNITY_EDITOR
            Undo.RegisterCompleteObjectUndo(this, "Undo settings");
#endif

            var multiplier = carSize * sizeMultiplier / DefaultCarSize;
            sizeDifference = (carSize - DefaultCarSize) * sizeMultiplier;

            maxDistanceToObstacle = GetNewValue(MaxDistanceToObstacle);
            minDistanceToStartApproach = GetNewValue(MinDistanceToStartApproach);
            minDistanceToStartApproachSoft = GetNewValue(MinDistanceToStartApproachSoft);
            minDistanceToCheckNextPath = GetNewValue(MinDistanceToCheckNextPath);
            shortPathLength = GetNewValue(ShortPathLength);
            calculateDistanceToIntesectPoint = GetNewValue(CalculateDistanceToIntesectPoint);
            sizeOffsetToIntersectPoint = GetNewValue2(SizeOffsetToIntersectPoint, multiplier);
            stopDistanceBeforeIntersection = GetNewValue(StopDistanceBeforeIntersection);
            stopDistanceForSameTargetNode = GetNewValue(StopDistanceForSameTargetNode);
            closeDistanceToChangeLanePoint = GetNewValue(CloseDistanceToChangeLanePoint);
            neighboringDistance = GetNewValue(NeighboringDistance);
            ConfigUpdated = true;
            EditorSaver.SetObjectDirty(this);
        }

        [Button]
        private void ResetToDefault()
        {
            maxDistanceToObstacle = MaxDistanceToObstacle;
            minDistanceToStartApproach = MinDistanceToStartApproach;
            minDistanceToCheckNextPath = MinDistanceToCheckNextPath;
            calculateDistanceToIntesectPoint = CalculateDistanceToIntesectPoint;
            sizeOffsetToIntersectPoint = SizeOffsetToIntersectPoint;
            stopDistanceBeforeIntersection = StopDistanceBeforeIntersection;
            stopDistanceForSameTargetNode = StopDistanceForSameTargetNode;
            closeDistanceToChangeLanePoint = CloseDistanceToChangeLanePoint;
            inFrontOfViewDot = InFrontOfViewDot;
            ConfigUpdated = true;
            EditorSaver.SetObjectDirty(this);
        }
    }
}