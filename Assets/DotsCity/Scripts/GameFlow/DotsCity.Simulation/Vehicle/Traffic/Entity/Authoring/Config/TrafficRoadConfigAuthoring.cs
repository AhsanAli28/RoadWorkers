using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.Gameplay.Road;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Road.Authoring
{
    public class TrafficRoadConfigAuthoring : RuntimeConfigUpdater<TrafficRoadConfigReference, TrafficRoadConfig>
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/roadConfigs.html#traffic-road-config")]
        [SerializeField] private string link;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("List of allowed nodes to randomly spawn on the path between those nodes")]
        [SerializeField]
        private List<TrafficNodeType> routeRandomizeNodes = new List<TrafficNodeType>()
        {
            TrafficNodeType.Default,
            TrafficNodeType.DestroyVehicle,
            TrafficNodeType.Idle,
        };

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("List of allowed nodes where cars can spawn")]
        [SerializeField]
        private List<TrafficNodeType> availableForSpawnNodes = new List<TrafficNodeType>()
        {
            TrafficNodeType.Default,
            TrafficNodeType.Parking,
            TrafficNodeType.DestroyVehicle,
            TrafficNodeType.Idle,
            TrafficNodeType.TrafficArea
        };

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("List of allowed traffic nodes that can be selected for a target at spawn")]
        [SerializeField]
        private List<TrafficNodeType> availableForSpawnTargetNodes = new List<TrafficNodeType>()
        {
            TrafficNodeType.Default,
            TrafficNodeType.DestroyVehicle,
            TrafficNodeType.Idle,
            TrafficNodeType.TrafficArea,
        };

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Nodes that are linked to a specific traffic car which approaching a linked node")]
        [SerializeField]
        private List<TrafficNodeType> linkedNodes = new List<TrafficNodeType>()
        {
            TrafficNodeType.Parking,
            TrafficNodeType.TrafficPublicStop,
        };

        public override TrafficRoadConfigReference CreateConfig(BlobAssetReference<TrafficRoadConfig> blobRef)
        {
            return new TrafficRoadConfigReference() { Config = blobRef };
        }

        protected override BlobAssetReference<TrafficRoadConfig> CreateConfigBlob()
        {
            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<TrafficRoadConfig>();

                int isAvailableForRouteRandomizeSpawningFlags = 0;

                foreach (var nodeType in routeRandomizeNodes)
                {
                    isAvailableForRouteRandomizeSpawningFlags = isAvailableForRouteRandomizeSpawningFlags | 1 << (int)nodeType;
                }

                int isAvailableForSpawnFlags = 0;

                foreach (var nodeType in availableForSpawnNodes)
                {
                    isAvailableForSpawnFlags = isAvailableForSpawnFlags | 1 << (int)nodeType;
                }

                int isAvailableForSpawnTargetFlags = 0;

                foreach (var nodeType in availableForSpawnTargetNodes)
                {
                    isAvailableForSpawnTargetFlags = isAvailableForSpawnTargetFlags | 1 << (int)nodeType;
                }

                int linkedNodeFlags = 0;

                foreach (var nodeType in linkedNodes)
                {
                    linkedNodeFlags = linkedNodeFlags | 1 << (int)nodeType;
                }

                root.IsAvailableForRouteRandomizeSpawningFlags = isAvailableForRouteRandomizeSpawningFlags;
                root.IsAvailableForSpawnFlags = isAvailableForSpawnFlags;
                root.IsAvailableForSpawnTargetFlags = isAvailableForSpawnTargetFlags;
                root.LinkedNodeFlags = linkedNodeFlags;

                return builder.CreateBlobAssetReference<TrafficRoadConfig>(Unity.Collections.Allocator.Persistent);
            }
        }

        class TrafficRoadConfigAuthoringBaker : Baker<TrafficRoadConfigAuthoring>
        {
            public override void Bake(TrafficRoadConfigAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.CreateConfig(this));
            }
        }
    }
}