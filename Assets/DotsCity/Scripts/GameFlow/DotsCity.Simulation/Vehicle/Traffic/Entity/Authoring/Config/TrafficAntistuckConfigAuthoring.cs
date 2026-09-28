using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using System;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic.Authoring
{
    public class TrafficAntistuckConfigAuthoring : RuntimeConfigUpdater<TrafficAntistuckConfigReference, TrafficAntistuckConfig>
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/trafficCarConfigs.html#traffic-antistuck-config")]
        [SerializeField] private string link;

        [GeneralOption("antiStuckSupport")]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Duration of sighting of the obstacle after which the car will be culled")]
        [SerializeField][Range(0.1f, 180f)] private float obstacleStuckTime = 15f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("If the car moved more than the parameter distance the `Obstacle stuck time` is reset")]
        [SerializeField][Range(0.1f, 5f)] private float stuckDistanceDiff = 0.3f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Car will be culled only if it is out of the camera's range of vision.")]
        [SerializeField] private bool cullOutOfTheCameraOnly = true;

        protected override bool UpdateAvailableByDefault => false;

        public override TrafficAntistuckConfigReference CreateConfig(BlobAssetReference<TrafficAntistuckConfig> blobRef)
        {
            return new TrafficAntistuckConfigReference() { Config = blobRef };
        }

        protected override BlobAssetReference<TrafficAntistuckConfig> CreateConfigBlob()
        {
            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<TrafficAntistuckConfig>();

                root.ObstacleStuckTime = obstacleStuckTime;
                root.StuckDistanceDiff = stuckDistanceDiff;
                root.CullOutOfTheCameraOnly = cullOutOfTheCameraOnly;

                return builder.CreateBlobAssetReference<TrafficAntistuckConfig>(Unity.Collections.Allocator.Persistent);
            }
        }

        class TrafficAntistuckConfigAuthoringBaker : Baker<TrafficAntistuckConfigAuthoring>
        {
            public override void Bake(TrafficAntistuckConfigAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.CreateConfig(this));
            }
        }
    }
}