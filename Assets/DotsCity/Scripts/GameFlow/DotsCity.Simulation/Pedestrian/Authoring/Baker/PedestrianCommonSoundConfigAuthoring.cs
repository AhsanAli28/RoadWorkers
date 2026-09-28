using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Core.Sound;
using Spirit604.DotsCity.Simulation.Sound;
using Spirit604.DotsCity.Simulation.Sound.Pedestrian;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian.Authoring
{
    public class PedestrianCommonSoundConfigAuthoring : RuntimeConfigUpdater<SoundConfigReference, SoundConfig>
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/pedestrianInit.html#common-sound-config")]
        [SerializeField] private string link;

        [SerializeField] private SoundLevelConfigHolder soundLevelConfigHolder;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField] private SoundData soundDeath;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField] private SoundData enterTramSound;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField] private SoundData exitTramSound;

        protected override bool UpdateAvailableByDefault => false;

        public override SoundConfigReference CreateConfig(BlobAssetReference<SoundConfig> blobRef)
        {
            return new SoundConfigReference() { Config = blobRef };
        }

        protected override BlobAssetReference<SoundConfig> CreateConfigBlob()
        {
            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<SoundConfig>();

                var hasSound = soundLevelConfigHolder?.SoundLevelConfig?.HasSounds ?? false;

                if (hasSound)
                {
                    root.DeathSoundId = soundDeath?.Id ?? -1;
                    root.EnterTramSoundId = enterTramSound?.Id ?? -1;
                    root.ExitTramSoundId = exitTramSound?.Id ?? -1;
                }
                else
                {
                    root.DeathSoundId = -1;
                    root.EnterTramSoundId = -1;
                    root.ExitTramSoundId = -1;
                }

                return builder.CreateBlobAssetReference<SoundConfig>(Unity.Collections.Allocator.Persistent);
            }
        }

        class PedestrianCommonSoundConfigAuthoringBaker : Baker<PedestrianCommonSoundConfigAuthoring>
        {
            public override void Bake(PedestrianCommonSoundConfigAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.CreateConfig(this));
            }
        }
    }
}