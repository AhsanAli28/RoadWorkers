using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.Extensions;
using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian.Authoring
{
    public class PedestrianTriggerConfigAuthoring : RuntimeConfigUpdater<TriggerConfigReference, TriggerConfig>
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/pedestrianInit.html#trigger-config")]
        [SerializeField] private string link;

        [GeneralOption("pedestrianTriggerSystemSupport")]
        [SerializeField][Range(0, 1000)] private int triggerHashMapCapacity = 20;

        [SerializeField][Range(0, 200)] private float triggerHashMapCellSize = 15f;

        [HideInInspector]
        [SerializeField] private List<TriggerDataConfig> triggerDataConfigs = new List<TriggerDataConfig>();

        public override TriggerConfigReference CreateConfig(BlobAssetReference<TriggerConfig> blobRef)
        {
            return new TriggerConfigReference() { Config = blobRef };
        }

        protected override BlobAssetReference<TriggerConfig> CreateConfigBlob()
        {
            using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<TriggerConfig>();

                root.TriggerHashMapCapacity = triggerHashMapCapacity;
                root.TriggerHashMapCellSize = triggerHashMapCellSize;

                ValidateTriggerArray();

                var blolbTriggerDataConfigs = builder.Allocate(ref root.TriggerDataConfigs, triggerDataConfigs.Count);

                for (int i = 0; i < triggerDataConfigs.Count; i++)
                {
                    blolbTriggerDataConfigs[i] = triggerDataConfigs[i];
                }

                return builder.CreateBlobAssetReference<TriggerConfig>(Unity.Collections.Allocator.Persistent);
            }
        }

        class PedestrianTriggerConfigAuthoringBaker : Baker<PedestrianTriggerConfigAuthoring>
        {
            public override void Bake(PedestrianTriggerConfigAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.CreateConfig(this));
            }
        }

        public int EnumCount()
        {
            return Enum.GetValues(typeof(TriggerAreaType)).Length;
        }

        public void ValidateTriggerArray()
        {
            var currentElementCount = triggerDataConfigs.Count;
            var targetElementCount = EnumCount();

            if (currentElementCount != targetElementCount)
            {
                List<TriggerDataConfig> newTriggerDataConfigs = null;

                if (targetElementCount < currentElementCount)
                {
                    newTriggerDataConfigs = triggerDataConfigs.GetRange(0, targetElementCount);
                }
                else
                {
                    newTriggerDataConfigs = new List<TriggerDataConfig>(triggerDataConfigs);
                    var addCount = targetElementCount - currentElementCount;

                    for (int i = 0; i < addCount; i++)
                    {
                        newTriggerDataConfigs.Add(new TriggerDataConfig());
                    }
                }

                triggerDataConfigs = newTriggerDataConfigs;
                EditorSaver.SetObjectDirty(this);
            }
        }

    }
}