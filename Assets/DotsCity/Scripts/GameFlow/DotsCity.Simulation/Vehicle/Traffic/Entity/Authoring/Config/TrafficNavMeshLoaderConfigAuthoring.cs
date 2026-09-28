using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public class TrafficNavMeshLoaderConfigAuthoring : SyncConfigBase
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/trafficCarConfigs.html#traffic-navmesh-loader-config")]
        [SerializeField] private string link;

        [OnValueChanged(nameof(Sync))]
        [Tooltip("Size offset of loaded NavMeshObstacle")]
        [SerializeField][Range(0f, 5f)] private float sizeOffset = 0.5f;

        [OnValueChanged(nameof(Sync))]
        [Tooltip("Load NavMeshObstacle in view of camera only")]
        [SerializeField] private bool loadOnlyInView = true;

        [OnValueChanged(nameof(Sync))]
        [ShowIf(nameof(loadOnlyInView))]
        [Tooltip("Load frequency of NavMeshObstacle for the car")]
        [SerializeField][Range(0f, 5f)] private float loadFrequency = 2f;

        class TrafficHornConfigAuthoringBaker : Baker<TrafficNavMeshLoaderConfigAuthoring>
        {
            public override void Bake(TrafficNavMeshLoaderConfigAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);

                using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
                {
                    ref var root = ref builder.ConstructRoot<TrafficNavMeshLoaderConfig>();

                    root.SizeOffset = authoring.sizeOffset;
                    root.LoadOnlyInView = authoring.loadOnlyInView;
                    root.LoadFrequency = authoring.loadFrequency;

                    var blobRef = builder.CreateBlobAssetReference<TrafficNavMeshLoaderConfig>(Unity.Collections.Allocator.Persistent);

                    AddBlobAsset(ref blobRef, out var hash);

                    AddComponent(entity, new TrafficNavMeshLoaderConfigReference() { Config = blobRef });
                }
            }
        }
    }
}