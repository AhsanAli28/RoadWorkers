using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Unity.Entities;
using UnityEngine;

#if UNITY_EDITOR
#endif

namespace Spirit604.DotsCity.Simulation.Pedestrian.Authoring
{
    public class PedestrianDestinationConfigAuthoring : RuntimeConfigUpdater<DestinationConfigReference, DestinationConfig>
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/pedestrianInit.html#configs")]
        [SerializeField] private string link;

        [SerializeField] private PedestrianSpawnerConfigHolder pedestrianSpawnerConfigHolder;

        [Tooltip("Ignore previous destination")]
        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField] private bool ignorePreviousDst = true;

        public void ConfigChanged()
        {
            OnInspectorValueUpdated();
        }

        public override DestinationConfigReference CreateConfig(BlobAssetReference<DestinationConfig> blobRef)
        {
            return new DestinationConfigReference() { Config = blobRef };
        }

        protected override BlobAssetReference<DestinationConfig> CreateConfigBlob()
        {
            float achieveDistance = 1f;

            if (pedestrianSpawnerConfigHolder && pedestrianSpawnerConfigHolder.PedestrianSettingsConfig)
            {
                achieveDistance = pedestrianSpawnerConfigHolder.PedestrianSettingsConfig.AchieveDistance;
            }

            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<DestinationConfig>();

                root.AchieveDistanceSQ = achieveDistance * achieveDistance;
                root.IgnorePreviousDst = ignorePreviousDst;

                return builder.CreateBlobAssetReference<DestinationConfig>(Unity.Collections.Allocator.Persistent);
            }
        }

        class PedestrianDestinationConfigAuthoringBaker : Baker<PedestrianDestinationConfigAuthoring>
        {
            public override void Bake(PedestrianDestinationConfigAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.CreateConfig(this));
            }
        }
    }
}