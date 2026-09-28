using Spirit604.AnimationBaker;
using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Pedestrian.Authoring;
using Spirit604.DotsCity.Simulation.Root.Authoring;
using Spirit604.Extensions;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian.State
{
    public class PedestrianAnimationStateAuthoring : SyncConfigBase, ISyncableConfig
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/pedestrianInit.html#state-authoring")]
        [SerializeField] private string link;

        [SerializeField]
        private PedestrianSpawnerConfigHolder pedestrianSpawnerConfigHolder;

        [OnValueChanged(nameof(Sync))]
        [SerializeField]
        private AnimationCollectionContainer animationCollectionContainer;

        [OnValueChanged(nameof(Sync))]
        [Expandable]
        [SerializeField]
        private AnimatorDataPreset pedestrianAnimatorDataPreset;

        public class PedestrianAnimationStateAuthoringBaker : Baker<PedestrianAnimationStateAuthoring>
        {
            public override void Bake(PedestrianAnimationStateAuthoring authoring)
            {
                var animationCollectionContainer = authoring.animationCollectionContainer;

                if (!animationCollectionContainer)
                    return;

                var entity = CreateAdditionalEntity(TransformUsageFlags.None);
                var legacyAnimationData = authoring.pedestrianAnimatorDataPreset.LegacyAnimationData;
                var pureAnimationData = authoring.pedestrianAnimatorDataPreset.PureAnimationData;

                using (var builder = new BlobBuilder(Allocator.Temp))
                {
                    ref var root = ref builder.ConstructRoot<AnimationDataBlob>();

                    var legacyKeys = builder.Allocate(ref root.LegacyKeys, legacyAnimationData.Keys.Count);
                    var legacyData = builder.Allocate(ref root.LegacyData, legacyAnimationData.Keys.Count);

                    var keyList = legacyAnimationData.Keys.ToList();
                    var valueList = legacyAnimationData.Values.ToList();

                    NpcRigType rigType = default;

                    if (authoring.pedestrianSpawnerConfigHolder)
                    {
                        rigType = authoring.pedestrianSpawnerConfigHolder.PedestrianSettingsConfig.PedestrianRigType;
                    }
                    else
                    {
                        Debug.LogError("PedestrianAnimationStateAuthoring. PedestrianSpawnerConfigHolder is null");
                    }

                    var hashProvider = GetHashProvider(rigType);

                    for (int i = 0; i < keyList.Count; i++)
                    {
                        legacyKeys[i] = keyList[i];
                        legacyData[i] = new LegacyAnimationDataComponent(valueList[i], hashProvider);
                    }

                    var gpuKeys = builder.Allocate(ref root.GPUKeys, pureAnimationData.Keys.Count);
                    var gpuData = builder.Allocate(ref root.GPUData, pureAnimationData.Keys.Count);

                    var keyList2 = pureAnimationData.Keys.ToList();
                    var valueList2 = pureAnimationData.Values.ToList();

                    for (int i = 0; i < keyList2.Count; i++)
                    {
                        gpuKeys[i] = keyList2[i];

                        var animData = animationCollectionContainer.GetAnimation(valueList2[i].AnimationGUID);

                        if (animData == null)
                        {
                            Debug.LogError($"PedestrianAnimationStateAuthoring. Animation data index {i} guid {valueList2[i].AnimationGUID} is null");
                            continue;
                        }

                        var animHash = animData.Hash;

                        gpuData[i] = new GPUAnimationDataComponent()
                        {
                            AnimationHash = animHash
                        };
                    }

                    var movementAnimationBinding = authoring.pedestrianAnimatorDataPreset.MovementAnimationBinding;

                    var movementKeys = builder.Allocate(ref root.MovementKeys, movementAnimationBinding.Keys.Count);
                    var movementValues = builder.Allocate(ref root.MovementValues, movementAnimationBinding.Keys.Count);

                    var movementKeyList = movementAnimationBinding.Keys.ToList();
                    var movementValueList = movementAnimationBinding.Values.ToList();

                    for (int i = 0; i < movementKeyList.Count; i++)
                    {
                        movementKeys[i] = movementKeyList[i];
                        movementValues[i] = movementValueList[i];
                    }

                    var blobRef = builder.CreateBlobAssetReference<AnimationDataBlob>(Allocator.Persistent);

                    AddBlobAsset(ref blobRef, out var hash);

                    AddComponent(entity, new AnimationDataBlobReference()
                    {
                        Config = blobRef
                    });
                }
            }

            private static IAnimationHashProvider GetHashProvider(NpcRigType rigType)
            {
                IAnimationHashProvider hashProvider;

                switch (rigType)
                {
                    case NpcRigType.Rukhanka:
                        hashProvider = new RukhankaHashProvider();
                        break;
                    case NpcRigType.RukhankaHybrid:
                        hashProvider = new RukhankaHashProvider();
                        break;
                    default:
                        hashProvider = new DefaultHashProvider();
                        break;
                }

                return hashProvider;
            }
        }

        [Button]
        public void SyncConfig()
        {
            var crowdSkinFactory = ObjectUtils.FindObjectOfType<CrowdSkinFactory>();

            if (crowdSkinFactory && crowdSkinFactory.AnimationCollectionContainer != animationCollectionContainer)
            {
                animationCollectionContainer = crowdSkinFactory.AnimationCollectionContainer;
                EditorSaver.SetObjectDirty(this);
            }
        }
    }
}