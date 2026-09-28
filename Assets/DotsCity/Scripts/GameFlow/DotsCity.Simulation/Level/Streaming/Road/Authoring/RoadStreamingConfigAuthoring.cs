using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using System;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Level.Streaming.Authoring
{
    public class RoadStreamingConfigAuthoring : RuntimeConfigUpdater<RoadStreamingConfigReference, RoadStreamingConfig>
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/streaming.html#road-streaming-config")]
        [SerializeField] private string link;

        [Tooltip("On/off road section streaming")]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField] private bool streamingIsEnabled = true;

        [Tooltip("Ignore calculation of distance to road section for Y axis")]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField] private bool ignoreY;

        [Tooltip("Distance at what the road section is loaded")]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][Range(0, 1000)] private float distanceForStreamingIn = 100f;

        [Tooltip("Distance at what the road section is unloaded")]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][Range(0, 1000)] private float distanceForStreamingOut = 120f;

        [Tooltip("Cell size of the road section")]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][Range(0, 1000)] private float sectionCellSize = 50f;

        [Tooltip("Node size for TrafficNode and PedestrianNode in order to compute a unique position hash for them")]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][Range(0, 1000)] private float nodeCellSize = 0.25f;

        public bool StreamingIsEnabled => streamingIsEnabled;

        public override RoadStreamingConfigReference CreateConfig(BlobAssetReference<RoadStreamingConfig> blobRef)
        {
            return new RoadStreamingConfigReference()
            {
                Config = blobRef
            };
        }

        protected override BlobAssetReference<RoadStreamingConfig> CreateConfigBlob()
        {
            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<RoadStreamingConfig>();

                root.StreamingIsEnabled = streamingIsEnabled;
                root.IgnoreY = ignoreY;
                root.DistanceForStreamingInSQ = distanceForStreamingIn * distanceForStreamingIn;
                root.DistanceForStreamingOutSQ = distanceForStreamingOut * distanceForStreamingOut;
                root.SectionCellSize = sectionCellSize;
                root.NodeCellSize = nodeCellSize;

                return builder.CreateBlobAssetReference<RoadStreamingConfig>(Unity.Collections.Allocator.Persistent);
            }
        }

        class RoadStreamingConfigBaker : Baker<RoadStreamingConfigAuthoring>
        {
            public override void Bake(RoadStreamingConfigAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);

                AddComponent(entity, authoring.CreateConfig(this));
            }
        }
    }
}