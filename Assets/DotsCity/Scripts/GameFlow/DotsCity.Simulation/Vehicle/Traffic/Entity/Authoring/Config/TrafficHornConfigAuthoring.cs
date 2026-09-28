using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic.Sound.Authoring
{
    public class TrafficHornConfigAuthoring : SyncConfigBase
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/trafficCarConfigs.html#traffic-horn-config")]
        [SerializeField] private string link;

        [OnValueChanged(nameof(Sync))]
        [Tooltip("Chance to start the horn (1 == 100%, 0 = 0%)")]
        [SerializeField][Range(0f, 1f)] private float chanceToStart = 1f;

        [OnValueChanged(nameof(Sync))]
        [Tooltip("Idle time to start the horn")]
        [SerializeField][Range(0f, 20f)] private float idleTimeToStart = 4f;

        [OnValueChanged(nameof(Sync))]
        [Tooltip("Delay between horns")]
        [SerializeField][MinMaxSlider(0.0f, 20f)] private Vector2 delay = new Vector2(5f, 10f);

        [OnValueChanged(nameof(Sync))]
        [Tooltip("Horn duration")]
        [SerializeField][MinMaxSlider(0.0f, 10f)] private Vector2 hornDuration = new Vector2(1.5f, 3f);

        class TrafficHornConfigAuthoringBaker : Baker<TrafficHornConfigAuthoring>
        {
            public override void Bake(TrafficHornConfigAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);

                using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
                {
                    ref var root = ref builder.ConstructRoot<TrafficHornConfig>();

                    root.ChanceToStart = authoring.chanceToStart;
                    root.IdleTimeToStart = authoring.idleTimeToStart;
                    root.MaxDelay = authoring.delay.y;
                    root.MinDelay = authoring.delay.x;
                    root.MaxHornDuration = authoring.hornDuration.y;
                    root.MinHornDuration = authoring.hornDuration.x;

                    var blobRef = builder.CreateBlobAssetReference<TrafficHornConfig>(Unity.Collections.Allocator.Persistent);

                    AddBlobAsset(ref blobRef, out var hash);

                    AddComponent(entity, new TrafficHornConfigReference() { Config = blobRef });
                }
            }
        }
    }
}