using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using System;
using Unity.Entities;
using Unity.Physics.Authoring;
using UnityEngine;
using static Spirit604.ProjectConstants;

namespace Spirit604.DotsCity.Simulation.Traffic.Authoring
{
    public class TrafficRaycastConfigAuthoring : RuntimeConfigUpdater<RaycastConfigReference, RaycastConfig>
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/trafficCarConfigs.html#traffic-raycast-config")]
        [SerializeField] private string link;

        [ShowIf(nameof(ConfigIsNull))]
        [SerializeField] private CitySettingsInitializerBase citySettingsInitializer;

        [ShowIf(nameof(ConfigIsNull))]
        [SerializeField] private TrafficSettings trafficSettings;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Width of raycast box")]
        [SerializeField][Range(0.1f, 2f)] private float sideOffset = 0.6f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Max lenght of raycast box")]
        [SerializeField][Range(3f, 25f)] private float maxRayLength = 15f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Min lenght of raycast box")]
        [SerializeField][Range(3f, 25f)] private float minRayLength = 4f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Height raycast box")]
        [SerializeField][Range(0.01f, 10f)] private float boxCastHeight = 1f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Y-offset position box")]
        [SerializeField][Range(0.1f, 2f)] private float rayYaxisOffset = 0.5f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("If the raycast is set to Hybrid mode than only those targets that are in front of the car with the set dot parameter will be raycasted")]
        [SerializeField][Range(0.01f, 1f)] private float dotDirection = 0.35f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Value by which the bounds is multiplied")]
        [SerializeField][Range(0.5f, 1.2f)] private float boundsMultiplier = 0.6f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("How often the raycast is performed")]
        [MinMaxSlider(0f, 2f)]
        [SerializeField][Range(0, 2f)] private Vector2 raycastFrequency = new Vector2(0.15f, 0.3f);

        [ShowIf(nameof(DOTSSimulation))]
        [EnableIf(nameof(HybridMode))]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Physics filter for hybrid mode")]
        [SerializeField]
        private PhysicsCategoryTags hybridModeFilter = new PhysicsCategoryTags()
        {
            Value = 1u << PLAYER_LAYER_VALUE | 1u << POLICE_LAYER_VALUE | 1u << DEFAULT_TRAFFIC_LAYER_VALUE
        };

        [ShowIf(nameof(DOTSSimulation))]
        [EnableIf(nameof(RaycastOnlyMode))]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Physics filter for raycast only mode")]
        [SerializeField]
        private PhysicsCategoryTags raycastOnlyFilter = new PhysicsCategoryTags()
        {
            Value = 1u << PLAYER_LAYER_VALUE | 1u << POLICE_LAYER_VALUE | 1u << DEFAULT_TRAFFIC_LAYER_VALUE,
        };

        [ShowIf(nameof(DOTSSimulation))]
        [EnableIf(nameof(RaycastNPCMode))]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Physics filter to raycast npc")]
        [SerializeField]
        private PhysicsCategoryTags raycastNPCFilter = new PhysicsCategoryTags()
        {
            Value = 1u << NPC_LAYER_VALUE,
        };

        [HideIf(nameof(DOTSSimulation))]
        [EnableIf(nameof(HybridMode))]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Physics filter for hybrid mode")]
        [SerializeField]
        private LayerMask hybridModeLayer;

        [HideIf(nameof(DOTSSimulation))]
        [EnableIf(nameof(RaycastOnlyMode))]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Physics filter for raycast only mode")]
        [SerializeField]
        private LayerMask raycastOnlyLayer;

        [HideIf(nameof(DOTSSimulation))]
        [EnableIf(nameof(RaycastNPCMode))]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Physics filter to raycast npc")]
        [SerializeField]
        private LayerMask raycastNpcLayer;

        protected override bool UpdateAvailableByDefault => false;

        private bool ConfigIsNull => !trafficSettings || !citySettingsInitializer;
        private bool HybridMode => trafficSettings && trafficSettings.TrafficSpawnerConfig && trafficSettings.TrafficSettingsConfig.DetectObstacleMode == DetectObstacleMode.Hybrid;
        private bool RaycastOnlyMode => trafficSettings && trafficSettings.TrafficSpawnerConfig && trafficSettings.TrafficSettingsConfig.DetectObstacleMode == DetectObstacleMode.RaycastOnly;
        private bool RaycastNPCMode => trafficSettings && trafficSettings.TrafficSpawnerConfig && trafficSettings.TrafficSettingsConfig.DetectNpcMode == DetectNpcMode.Raycast;
        private bool DOTSSimulation => citySettingsInitializer != null ? citySettingsInitializer.DOTSSimulation : true;

        public override RaycastConfigReference CreateConfig(BlobAssetReference<RaycastConfig> blobRef)
        {
            return new RaycastConfigReference() { Config = blobRef };
        }

        protected override BlobAssetReference<RaycastConfig> CreateConfigBlob()
        {
            uint raycastFilter = ~0u;

            if (trafficSettings && trafficSettings.TrafficSettingsConfig)
            {
                var trafficSettingsConfig = trafficSettings.TrafficSettingsConfig;
                var trafficDetectObstacleMode = trafficSettingsConfig.DetectObstacleMode;
                var trafficDetectNpcMode = trafficSettingsConfig.DetectNpcMode;

                switch (trafficDetectObstacleMode)
                {
                    case DetectObstacleMode.Hybrid:
                        {
                            if (DOTSSimulation)
                            {
                                raycastFilter = hybridModeFilter.Value;
                            }
                            else
                            {
                                raycastFilter = (uint)hybridModeLayer.value;
                            }

                            break;
                        }
                    case DetectObstacleMode.RaycastOnly:
                        {
                            if (DOTSSimulation)
                            {
                                raycastFilter = raycastOnlyFilter.Value;
                            }
                            else
                            {
                                raycastFilter = (uint)raycastOnlyLayer.value;
                            }

                            break;
                        }
                }

                if (trafficDetectNpcMode == DetectNpcMode.Raycast)
                {
                    if (DOTSSimulation)
                    {
                        raycastFilter = raycastFilter | raycastNPCFilter.Value;
                    }
                    else
                    {
                        raycastFilter = raycastFilter | (uint)raycastNpcLayer.value;
                    }
                }
            }

            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<RaycastConfig>();

                root.SideOffset = sideOffset;
                root.MaxRayLength = maxRayLength;
                root.MaxRayLengthSQ = maxRayLength * maxRayLength;
                root.MinRayLength = minRayLength;
                root.BoxCastHeight = boxCastHeight;
                root.RayYaxisOffset = rayYaxisOffset;
                root.DotDirection = dotDirection;
                root.BoundsMultiplier = boundsMultiplier;
                root.CastFrequency = raycastFrequency;
                root.RaycastFilter = raycastFilter;

                return builder.CreateBlobAssetReference<RaycastConfig>(Unity.Collections.Allocator.Persistent);
            }
        }

        class TrafficRaycastConfigAuthoringBaker : Baker<TrafficRaycastConfigAuthoring>
        {
            public override void Bake(TrafficRaycastConfigAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.CreateConfig(this));
            }
        }
    }
}