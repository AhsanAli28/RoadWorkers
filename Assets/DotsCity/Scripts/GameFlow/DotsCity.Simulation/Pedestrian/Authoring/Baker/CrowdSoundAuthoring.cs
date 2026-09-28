using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Core.Sound;
using Unity.Entities;
using UnityEngine;

#if !FMOD
using Spirit604.DotsCity.Hybrid.Core;
using Unity.Transforms;
#endif

namespace Spirit604.DotsCity.Simulation.Sound.Pedestrian.Authoring
{
    public class CrowdSoundAuthoring : RuntimeConfigAwaiter<CrowdSoundData>
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/commonConfigs.html#crowd-sound-config")]
        [SerializeField] private string link;

#pragma warning disable 0414

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField] private SoundLevelConfigHolder soundLevelConfigHolder;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [SerializeField] private SoundData crowdSoundData;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Maximum volume for a given number of pedestrians in the inner circle")]
        [SerializeField][Range(0, 400)] private int innerCrowdSoundCount = 50;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Maximum volume for a given number of pedestrians in the outer circle")]
        [SerializeField][Range(0, 400)] private int outerCrowdSoundCount = 150;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Minimum number of pedestrians to play the crowd sound")]
        [SerializeField][Range(0, 400)] private int minCrowdSoundCount = 5;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Maximum volume level for the crowd sound")]
        [SerializeField][Range(0, 1f)] private float maxVolume = 1f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Maximum volume in the outer circle")]
        [SerializeField][Range(0, 1f)] private float outerMaxVolume = 0.4f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Minimum volume level for the crowd sound")]
        [SerializeField][Range(0, 1f)] private float minVolume = 0.05f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Offset of neighbouring cells relative to current cell in hashmap in the inner circle")]
        [SerializeField][Range(0, HashMapHelper.DEFAULT_CELL_RADIUS)] private float innerCellOfset = 1f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Offset of neighbouring cells relative to current cell in hashmap in the outer circle")]
        [SerializeField][Range(0, HashMapHelper.DEFAULT_CELL_RADIUS)] private float outerCellOfset = 6f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Speed of sound volume change between current value and target value")]
        [SerializeField][Range(0, 20f)] private float lerpVolumeSpeed = 2f;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Height at which the sound begins to fade linearly")]
        [SerializeField][Range(0, 100f)] private float minHeightMuting = 15;

        [OnValueChanged(nameof(OnInspectorValueUpdated))]
        [Tooltip("Height above which the crowd sound is completely muted")]
        [SerializeField][Range(0, 100f)] private float maxHeight = 40;

#pragma warning restore 0414

        protected override bool HasCustomEntityArchetype => true;

        protected override bool UpdateAvailableByDefault => false;

        protected override bool IgnoreExist => true;

        protected override EntityArchetype GetEntityArchetype()
        {
            return EntityManager.CreateArchetype(typeof(CrowdSoundData), typeof(CrowdSoundVolume), typeof(SoundComponent), typeof(SoundVolume));
        }

        protected override void ConvertInternal(Entity entity, EntityManager dstManager)
        {
            dstManager.SetComponentData(entity, new CrowdSoundData()
            {
                InnerCrowdSoundCount = innerCrowdSoundCount,
                OuterCrowdSoundCount = outerCrowdSoundCount,
                MinCrowdSoundCount = minCrowdSoundCount,
                OuterMaxVolume = outerMaxVolume,
                MaxVolume = maxVolume,
                MinVolume = minVolume,
                InnerCellOffset = innerCellOfset,
                OuterCellOffset = outerCellOfset,
                LerpVolumeSpeed = lerpVolumeSpeed,
            });

            dstManager.SetComponentData(entity, new CrowdSoundVolume() { });

            dstManager.SetComponentData(entity, new SoundComponent()
            {
                Id = crowdSoundData.Id,
            });

            dstManager.SetComponentData(entity, new SoundVolume() { });
        }

        class CrowdSoundAuthoringBaker : Baker<CrowdSoundAuthoring>
        {
            public override void Bake(CrowdSoundAuthoring authoring)
            {
                var hasSounds = authoring.soundLevelConfigHolder?.SoundLevelConfig?.HasSounds ?? false;

                if (!hasSounds)
                    return;

#if FMOD
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);
#else
                var entity = CreateAdditionalEntity(TransformUsageFlags.ManualOverride);
#endif

                if (authoring.crowdSoundData != null && authoring.soundLevelConfigHolder.SoundLevelConfig.CrowdSound)
                {
                    AddComponent(entity, new CrowdSoundData()
                    {
                        InnerCrowdSoundCount = authoring.innerCrowdSoundCount,
                        OuterCrowdSoundCount = authoring.outerCrowdSoundCount,
                        MinCrowdSoundCount = authoring.minCrowdSoundCount,
                        OuterMaxVolume = authoring.outerMaxVolume,
                        MaxVolume = authoring.maxVolume,
                        MinVolume = authoring.minVolume,
                        InnerCellOffset = authoring.innerCellOfset,
                        OuterCellOffset = authoring.outerCellOfset,
                        LerpVolumeSpeed = authoring.lerpVolumeSpeed,
                        MinHeightMuting = authoring.minHeightMuting,
                        MaxHeight = authoring.maxHeight
                    });

                    AddComponent(entity, new CrowdSoundVolume() { });

                    AddComponent(entity, new SoundComponent()
                    {
                        Id = authoring.crowdSoundData.Id,
                    });

                    AddComponent(entity, new SoundVolume() { });
                    AddComponent(entity, new SoundCacheVolume() { });

#if !FMOD
                    AddComponent(entity, typeof(LocalToWorld));
                    AddComponent(entity, typeof(LocalTransform));
                    AddComponent(entity, typeof(CopyTransformToGameObject));
                    AddComponent(entity, typeof(PlayerTrackerTag));
#endif
                }
            }
        }
    }
}
