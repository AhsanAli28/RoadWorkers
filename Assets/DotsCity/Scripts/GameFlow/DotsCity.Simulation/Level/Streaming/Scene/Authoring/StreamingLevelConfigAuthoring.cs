using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using System;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Level.Streaming
{
    public class StreamingLevelConfigAuthoring : RuntimeConfigAwaiter<StreamingLevelConfig>
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/streaming.html#streaming-level-config")]
        [SerializeField] private string link;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField] private bool streamingIsEnabled = true;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][Range(0, 1000)] private float distanceForStreamingIn = 100f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][Range(0, 1000)] private float distanceForStreamingOut = 120f;

        protected override void ConvertInternal(Entity entity, EntityManager dstManager)
        {
            dstManager.AddComponentData(entity, GetConfig());
        }

        public StreamingLevelConfig GetConfig()
        {
            return new StreamingLevelConfig
            {
                DistanceForStreamingInSQ = distanceForStreamingIn * distanceForStreamingIn,
                DistanceForStreamingOutSQ = distanceForStreamingOut * distanceForStreamingOut,
                StreamingIsEnabled = streamingIsEnabled
            };
        }

        class StreamingLevelConfigBaker : Baker<StreamingLevelConfigAuthoring>
        {
            public override void Bake(StreamingLevelConfigAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);

                AddComponent(entity, authoring.GetConfig());
            }
        }
    }
}