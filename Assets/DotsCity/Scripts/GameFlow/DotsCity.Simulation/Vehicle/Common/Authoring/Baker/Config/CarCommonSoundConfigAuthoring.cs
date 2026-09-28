using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Core.Sound;
using Spirit604.DotsCity.Simulation.Sound;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Car.Sound.Authoring
{
    public class CarCommonSoundConfigAuthoring : SyncConfigBase
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/carCommonConfigs.html#car-common-sound-config")]
        [SerializeField] private string link;

        [SerializeField] private SoundLevelConfigHolder soundLevelConfigHolder;

        [OnValueChanged(nameof(Sync))]
        [SerializeField] private SoundData collisionSound;

        [OnValueChanged(nameof(Sync))]
        [SerializeField] private SoundData carExplodeSound;

        [OnValueChanged(nameof(Sync))]
        [SerializeField] private SoundData bulletHitSound;

        [OnValueChanged(nameof(Sync))]
        [SerializeField] private SoundData npcHitSound;

        class CarCommonSoundConfigAuthoringBaker : Baker<CarCommonSoundConfigAuthoring>
        {
            public override void Bake(CarCommonSoundConfigAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);

                using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
                {
                    ref var root = ref builder.ConstructRoot<CarSoundCommonConfig>();

                    var hasSound = authoring.soundLevelConfigHolder?.SoundLevelConfig?.HasSounds ?? false;

                    if (hasSound)
                    {
                        root.CollisionSoundId = authoring.collisionSound?.Id ?? -1;
                        root.CarExplodeSoundId = authoring.carExplodeSound?.Id ?? -1;
                        root.BulletHitSoundId = authoring.bulletHitSound?.Id ?? -1;
                        root.NpcHitSoundId = authoring.npcHitSound?.Id ?? -1;
                    }
                    else
                    {
                        root.CollisionSoundId = -1;
                        root.CarExplodeSoundId = -1;
                        root.BulletHitSoundId = -1;
                        root.NpcHitSoundId = -1;
                    }

                    var blobRef = builder.CreateBlobAssetReference<CarSoundCommonConfig>(Unity.Collections.Allocator.Persistent);

                    AddBlobAsset(ref blobRef, out var hash);

                    AddComponent(entity, new CarSoundCommonConfigReference() { Config = blobRef });
                }
            }
        }
    }
}