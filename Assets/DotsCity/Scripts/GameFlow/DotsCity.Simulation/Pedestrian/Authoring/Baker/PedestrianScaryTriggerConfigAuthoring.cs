using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Core.Sound;
using System;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian.Authoring
{
    public class PedestrianScaryTriggerConfigAuthoring : RuntimeConfigUpdater<ScaryTriggerConfigReference, ScaryTriggerConfig>
    {
        private const string SettingsGroupName = "Trigger Settings";
        private const string SoundGroupName = "Sound Settings";

        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/pedestrianInit.html#scary-trigger-config")]
        [SerializeField] private string link;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][Range(0, 200)] private float deathTriggerSquaredDistance = 25f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][Range(0, 20)] private float deathTriggerDuration = 1f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField] private bool hasScreamSound = true;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][Range(0, 20)] private int screamEntityLimit = 4;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][Range(0, 1f)] private float chanceToScream = 0.2f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField][MinMaxSlider(0, 5f)] private Vector2 screamDelay = new Vector2(0, 1f);

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField] private SoundData screamSoundData;

        public override ScaryTriggerConfigReference CreateConfig(BlobAssetReference<ScaryTriggerConfig> blobRef)
        {
            return new ScaryTriggerConfigReference() { Config = blobRef };
        }

        protected override BlobAssetReference<ScaryTriggerConfig> CreateConfigBlob()
        {
            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<ScaryTriggerConfig>();

                root.DeathTriggerSqDistance = deathTriggerSquaredDistance;
                root.DeathTriggerDuration = deathTriggerDuration;
                root.HasScreamSound = hasScreamSound;
                root.ScreamEntityLimit = screamEntityLimit;
                root.ChanceToScream = chanceToScream;
                root.MinScreamDelay = screamDelay.x;
                root.MaxScreamDelay = screamDelay.y;

                int soundId = -1;

                if (screamSoundData != null)
                {
                    soundId = screamSoundData.Id;
                }

                root.ScreamSoundId = soundId;

                return builder.CreateBlobAssetReference<ScaryTriggerConfig>(Unity.Collections.Allocator.Persistent);
            }
        }

        class PedestrianScaryTriggerConfigAuthoringBaker : Baker<PedestrianScaryTriggerConfigAuthoring>
        {
            public override void Bake(PedestrianScaryTriggerConfigAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.CreateConfig(this));
            }
        }
    }
}