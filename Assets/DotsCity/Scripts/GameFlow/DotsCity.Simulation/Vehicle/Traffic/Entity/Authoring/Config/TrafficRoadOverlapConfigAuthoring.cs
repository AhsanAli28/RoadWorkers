using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using System;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Road.Authoring
{
    public class TrafficRoadOverlapConfigAuthoring : RuntimeConfigUpdater<TrafficRoadOverlapConfigReference, TrafficRoadOverlapConfig>
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/roadConfigs.html#overlap-config")]
        [SerializeField] private string link;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("" +
            "<b>Low accuracy</b> : low accuracy of calculation based on distance, but higher performance\r\n\r\n" +
            "<b>High accuracy</b> : higher accuracy of calculation based on bounds, but little bit lower performance")]
        [SerializeField] private TrafficNodeCalculateOverlapSystem.CalculateMethod calculateMethod = TrafficNodeCalculateOverlapSystem.CalculateMethod.HighAccuracy;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Size multiplier of the traffic car")]
        [SerializeField][Range(0f, 25f)] private float sizeMultiplier = 1.15f;

        public override TrafficRoadOverlapConfigReference CreateConfig(BlobAssetReference<TrafficRoadOverlapConfig> blobRef)
        {
            return new TrafficRoadOverlapConfigReference() { Config = blobRef };
        }

        protected override BlobAssetReference<TrafficRoadOverlapConfig> CreateConfigBlob()
        {
            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<TrafficRoadOverlapConfig>();

                root.CalculateMethod = calculateMethod;
                root.SizeMultiplier = sizeMultiplier;

                return builder.CreateBlobAssetReference<TrafficRoadOverlapConfig>(Unity.Collections.Allocator.Persistent);
            }
        }

        class TrafficRoadOverlapConfigAuthoringBaker : Baker<TrafficRoadOverlapConfigAuthoring>
        {
            public override void Bake(TrafficRoadOverlapConfigAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.CreateConfig(this));
            }
        }
    }
}