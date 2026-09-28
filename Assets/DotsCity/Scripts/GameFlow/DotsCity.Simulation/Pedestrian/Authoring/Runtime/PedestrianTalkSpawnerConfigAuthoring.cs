using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian.Authoring
{
    public class PedestrianTalkSpawnerConfigAuthoring : RuntimeEntityConfigBase
    {
        [Tooltip("How often the talking animation switches to the next")]
        [SerializeField][MinMaxSlider(0, 10f)] private Vector2 switchAnimationDuration = new Vector2(3, 5);

        [SerializeField]
        private List<AnimationState> talkingStates = new List<AnimationState>()
        {
            AnimationState.Talking1,
            AnimationState.Talking2,
            AnimationState.Talking3,
        };

        private PedestrianSpawnerConfigHolder pedestrianSpawnerConfigHolder;
        private BlobAssetReference<TalkSpawnSettings> blobRef;

        [InjectWrapper]
        public void Construct(PedestrianSpawnerConfigHolder pedestrianSpawnerConfigHolder)
        {
            this.pedestrianSpawnerConfigHolder = pedestrianSpawnerConfigHolder;
        }

        protected override bool AutoSync => false;

        public override void Dispose()
        {
            base.Dispose();

            if (blobRef.IsCreated)
            {
                blobRef.Dispose();
            }
        }

        protected override void ConvertInternal(Entity entity, EntityManager dstManager)
        {
            var config = pedestrianSpawnerConfigHolder.PedestrianSettingsConfig;

            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<TalkSpawnSettings>();

                root.TalkingPedestrianSpawnChance = config.TalkingPedestrianSpawnChance;
                root.MinTalkTime = config.MinMaxTalkTime.x;
                root.MaxTalkTime = config.MinMaxTalkTime.y;
                root.MinMaxSwitchDuration = switchAnimationDuration;

                var talkingArray = builder.Allocate(ref root.TalkingStates, talkingStates.Count);

                for (int i = 0; i < talkingStates.Count; i++)
                {
                    talkingArray[i] = talkingStates[i];
                }

                blobRef = builder.CreateBlobAssetReference<TalkSpawnSettings>(Unity.Collections.Allocator.Persistent);

                dstManager.AddComponentData(entity, new TalkSpawnSettingsReference() { Config = blobRef });
            }
        }

        protected override void OnConfigUpdatedInternal()
        {
            base.OnConfigUpdatedInternal();
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PedestrianEntitySpawnerSystem>().UpdateTalkSpawnConfig();
        }
    }
}
