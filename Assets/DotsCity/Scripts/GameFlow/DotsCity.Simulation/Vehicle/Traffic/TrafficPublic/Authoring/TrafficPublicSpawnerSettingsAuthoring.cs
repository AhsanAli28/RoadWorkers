using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.TrafficPublic.Authoring
{
    public class TrafficPublicSpawnerSettingsAuthoring : MonoBehaviour
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/trafficPublicConfigs.html#traffic-public-spawner-settings")]
        [SerializeField] private string link;

        [GeneralOption("trafficPublicSupport")]
        [Tooltip("Spawn frequency of public transport")]
        [SerializeField][Range(0, 60f)] private float spawnFrequency = 2f;

        [Tooltip("Hash map size for public routes")]
        [SerializeField][Range(0, 500)] private int routeHashMapCapacity = 50;

        class TrafficPublicSpawnerSettingsAuthoringBaker : Baker<TrafficPublicSpawnerSettingsAuthoring>
        {
            public override void Bake(TrafficPublicSpawnerSettingsAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);

                using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
                {
                    ref var root = ref builder.ConstructRoot<TrafficPublicSpawnerSettings>();

                    root.SpawnFrequency = authoring.spawnFrequency;
                    root.RouteHashMapCapacity = authoring.routeHashMapCapacity;

                    var blobRef = builder.CreateBlobAssetReference<TrafficPublicSpawnerSettings>(Unity.Collections.Allocator.Persistent);

                    AddBlobAsset(ref blobRef, out var hash);

                    AddComponent(entity, new TrafficPublicSpawnerSettingsReference() { Config = blobRef });
                }
            }
        }
    }
}