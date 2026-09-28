using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Sound
{
    public class SoundLevelConfigHolder : SyncConfigBase
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/commonConfigs.html#common-sound-config")]
        [SerializeField] private string link;

        [OnValueChanged(nameof(Sync))]
        [Expandable]
        [SerializeField] private SoundLevelConfig soundLevelConfig;

        public SoundLevelConfig SoundLevelConfig => soundLevelConfig;

        public class SoundLevelConfigBaker : Baker<SoundLevelConfigHolder>
        {
            public override void Bake(SoundLevelConfigHolder authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);

                var config = authoring.soundLevelConfig;

                using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
                {
                    ref var root = ref builder.ConstructRoot<SoundLevelConfigData>();

                    if (config)
                    {
                        root.HasSounds = config.HasSounds;
                        root.TrafficHasSounds = config.HasSounds && !config.ForceCustomTrafficSound;
                        root.RandomHornsSound = config.RandomHornsSound;
                    }
                    else
                    {
                        root.HasSounds = false;
                        root.RandomHornsSound = false;
                    }

                    var blobRef = builder.CreateBlobAssetReference<SoundLevelConfigData>(Unity.Collections.Allocator.Persistent);

                    AddBlobAsset(ref blobRef, out var hash);

                    AddComponent(entity, new SoundLevelConfigReference() { Config = blobRef });
                }
            }
        }
    }
}