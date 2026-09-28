using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic.Authoring
{
    public class TrafficAvoidanceConfigAuthoring : SyncConfigBase
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/trafficCarConfigs.html#traffic-avoidance-config")]
        [SerializeField] private string link;

        [ShowIfNull]
        [SerializeField]
        private CitySettingsInitializerBase citySettingsInitializer;

        [GeneralOption("avoidanceSupport")]
        [OnValueChanged(nameof(Sync))]
        [Tooltip("Custom achieve distance of avoidance point")]
        [SerializeField][Range(0f, 5f)] private float customAchieveDistance = 0.5f;

        [OnValueChanged(nameof(Sync))]
        [Tooltip("Overcome the cyclical obstacle of cars getting stuck in each other")]
        [SerializeField] private bool resolveCyclicObstacle = true;

        class TrafficAvoidanceConfigAuthoringBaker : Baker<TrafficAvoidanceConfigAuthoring>
        {
            public override void Bake(TrafficAvoidanceConfigAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);

                using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
                {
                    ref var root = ref builder.ConstructRoot<TrafficAvoidanceConfig>();

                    root.CustomAchieveDistance = authoring.customAchieveDistance;
                    root.ResolveCyclicObstacle = authoring.resolveCyclicObstacle;

                    var blobRef = builder.CreateBlobAssetReference<TrafficAvoidanceConfig>(Unity.Collections.Allocator.Persistent);

                    AddBlobAsset(ref blobRef, out var hash);

                    AddComponent(entity, new TrafficAvoidanceConfigReference() { Config = blobRef });
                }
            }
        }
    }
}