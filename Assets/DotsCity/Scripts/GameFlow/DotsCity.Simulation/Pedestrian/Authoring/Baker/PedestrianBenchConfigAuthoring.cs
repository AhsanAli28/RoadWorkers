using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian.Authoring
{
    public class PedestrianBenchConfigAuthoring : RuntimeConfigUpdater<BenchConfigReference, BenchConfig>
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/pedestrianInit.html#bench-config")]
        [SerializeField] private string link;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][MinMaxSlider(0, 200f)] private Vector2 minMaxIdleTime = new Vector2(15f, 20f);

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Distance to reach the entry point of the bench")]
        [SerializeField][Range(0, 5)] private float entryDistance = 0.2f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Idle after reaching the bench exit point")]
        [SerializeField][Range(0, 2f)] private float exitIdleDuration = 0.4f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][Range(0, 5f)] private float sittingMovementSpeed = 1f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][Range(0, 5f)] private float sittingRotationSpeed = 2f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Distance to reach the bench seat point")]
        [SerializeField][Range(0, 5f)] private float sitPointDistance = 0.15f;

        protected override bool UpdateAvailableByDefault => false;

        public override BenchConfigReference CreateConfig(BlobAssetReference<BenchConfig> blobRef)
        {
            return new BenchConfigReference() { Config = blobRef };
        }

        protected override BlobAssetReference<BenchConfig> CreateConfigBlob()
        {
            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<BenchConfig>();

                root.MinIdleTime = minMaxIdleTime.x;
                root.MaxIdleTime = minMaxIdleTime.y;
                root.EntryDistance = entryDistance;
                root.ExitIdleDuration = exitIdleDuration;
                root.SittingMovementSpeed = sittingMovementSpeed;
                root.SittingRotationSpeed = sittingRotationSpeed;
                root.SitPointDistanceSQ = sitPointDistance * sitPointDistance;

                return builder.CreateBlobAssetReference<BenchConfig>(Unity.Collections.Allocator.Persistent);
            }
        }

        class PedestrianBenchConfigAuthoringBaker : Baker<PedestrianBenchConfigAuthoring>
        {
            public override void Bake(PedestrianBenchConfigAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.CreateConfig(this));
            }
        }
    }
}