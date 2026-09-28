using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Car;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic.Sound.Authoring
{
    public class TrafficCustomDestinationConfigAuthoring : SyncConfigBase
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/trafficCarConfigs.html#traffic-custom-destination-config")]
        [SerializeField] private string link;

        [OnValueChanged(nameof(Sync))]
        [Tooltip("Default speed limit for custom destination (if user doesn't set custom)")]
        [SerializeField][Range(0f, 60f)] private float defaultSpeedLimit = 15f;

        [OnValueChanged(nameof(Sync))]
        [Tooltip("Check that the destination is on the side of the car")]
        [SerializeField] private bool checkSidePoint = true;

        [OnValueChanged(nameof(Sync))]
        [ShowIf(nameof(checkSidePoint))]
        [Tooltip("Custom speed limit when destination is at side point")]
        [SerializeField][Range(0f, 60f)] private float sidePointSpeedLimit = 5f;

        [OnValueChanged(nameof(Sync))]
        [ShowIf(nameof(checkSidePoint))]
        [Tooltip("Distance to side point")]
        [SerializeField][Range(0f, 10f)] private float sidePointDistance = 3.5f;

        [OnValueChanged(nameof(Sync))]
        [Tooltip("Achieve distance of destination point")]
        [SerializeField][Range(0f, 5f)] private float defaultAchieveDistance = 1f;

        [OnValueChanged(nameof(Sync))]
        [Tooltip("Max duration of custom destination state enabled")]
        [SerializeField][Range(0f, 20f)] private float maxDuration = 5f;

        class TrafficCustomDestinationConfigAuthoringBaker : Baker<TrafficCustomDestinationConfigAuthoring>
        {
            public override void Bake(TrafficCustomDestinationConfigAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);

                using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
                {
                    ref var root = ref builder.ConstructRoot<TrafficCustomDestinationConfig>();

                    root.DefaultSpeedLimit = SpeedComponent.ToMeterPerSecond(authoring.defaultSpeedLimit);
                    root.CheckSidePoint = authoring.checkSidePoint;
                    root.SidePointSpeedLimit = SpeedComponent.ToMeterPerSecond(authoring.sidePointSpeedLimit);
                    root.SidePointDistance = authoring.sidePointDistance;
                    root.DefaultAchieveDistance = authoring.defaultAchieveDistance;
                    root.DefaultDuration = authoring.maxDuration;

                    var blobRef = builder.CreateBlobAssetReference<TrafficCustomDestinationConfig>(Unity.Collections.Allocator.Persistent);

                    AddBlobAsset(ref blobRef, out var hash);

                    AddComponent(entity, new TrafficCustomDestinationConfigReference() { Config = blobRef });
                }
            }
        }
    }
}