using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Core.Authoring;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Road.Authoring
{
    public class CitySpawnConfigAuthoring : SyncConfigBase
    {
        [OnValueChanged(nameof(Sync))]
        [SerializeField] private CullStateList trafficNodeStateList = CullStateList.Default;

        [OnValueChanged(nameof(Sync))]
        [Tooltip("The cull state of the traffic node in which the traffic will spawn.")]
        [CullStateListSource(nameof(trafficNodeStateList))]
        [SerializeField] private CullState trafficSpawnStateNode = CullState.CloseToCamera;

        [OnValueChanged(nameof(Sync))]
        [SerializeField] private CullStateList pedestrianNodeStateList = CullStateList.Default;

        [OnValueChanged(nameof(Sync))]
        [Tooltip("The cull state of the pedestrian node in which the pedestrian will spawn.")]
        [CullStateListSource(nameof(pedestrianNodeStateList))]
        [SerializeField] private CullState pedestrianSpawnStateNode = CullState.CloseToCamera;

        private class CitySpawnConfigAuthoringBaker : Baker<CitySpawnConfigAuthoring>
        {
            public override void Bake(CitySpawnConfigAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);

                using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
                {
                    ref var root = ref builder.ConstructRoot<CitySpawnConfig>();

                    root.TrafficNodeStateList = authoring.trafficNodeStateList;
                    root.TrafficSpawnStateNode = authoring.trafficSpawnStateNode;
                    root.PedestrianNodeStateList = authoring.pedestrianNodeStateList;
                    root.PedestrianSpawnStateNode = authoring.pedestrianSpawnStateNode;

                    var blobRef = builder.CreateBlobAssetReference<CitySpawnConfig>(Unity.Collections.Allocator.Persistent);

                    AddBlobAsset(ref blobRef, out var hash);

                    AddComponent(entity, new CitySpawnConfigReference() { Config = blobRef });
                }
            }
        }
    }
}