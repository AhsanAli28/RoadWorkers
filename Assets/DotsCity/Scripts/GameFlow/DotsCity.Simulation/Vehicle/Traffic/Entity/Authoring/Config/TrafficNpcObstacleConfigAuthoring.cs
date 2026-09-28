using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Pedestrian;
using System;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic.Authoring
{
    public class TrafficNpcObstacleConfigAuthoring : RuntimeConfigUpdater<TrafficNpcObstacleConfigReference, TrafficNpcObstacleConfig>
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/trafficCarConfigs.html#traffic-npc-obstacle-config")]
        [SerializeField] private string link;

        [Tooltip("The car only reacts to pedestrians with the selected ActionState")]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField] private ActionState obstaclePedestrianActionState = (ActionState)~0;

        [Tooltip("Obstacle calculation length")]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][Range(0.1f, 20f)] private float checkDistance = 9f;

        [Tooltip("Length of the obstacle calculation square")]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][Range(0.1f, 25f)] private float squareLength = 8f;

        [Tooltip("Width of the obstacle calculation square")]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][Range(0.1f, 25f)] private float sideOffsetX = 1.4f;

        [Tooltip("Z-axis offset relative to car body extents")]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][Range(-2f, 2f)] private float rateOffsetZ = -0.8f;

        [Tooltip("Maximum difference in Y-axis position between the car and the npc")]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][Range(0.01f, 10f)] private float maxYDiff = 2f;

        public override TrafficNpcObstacleConfigReference CreateConfig(BlobAssetReference<TrafficNpcObstacleConfig> blobRef)
        {
            return new TrafficNpcObstacleConfigReference() { Config = blobRef };
        }

        protected override BlobAssetReference<TrafficNpcObstacleConfig> CreateConfigBlob()
        {
            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<TrafficNpcObstacleConfig>();

                root.ObstacleActionStates = obstaclePedestrianActionState;
                root.CheckDistanceSQ = checkDistance * checkDistance;
                root.SquareLength = squareLength;
                root.SideOffsetX = sideOffsetX;
                root.RateOffsetZ = rateOffsetZ;
                root.MaxYDiff = maxYDiff;

                return builder.CreateBlobAssetReference<TrafficNpcObstacleConfig>(Unity.Collections.Allocator.Persistent);
            }
        }

        class TrafficNpcObstacleConfigAuthoringBaker : Baker<TrafficNpcObstacleConfigAuthoring>
        {
            public override void Bake(TrafficNpcObstacleConfigAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.CreateConfig(this));
            }
        }
    }
}