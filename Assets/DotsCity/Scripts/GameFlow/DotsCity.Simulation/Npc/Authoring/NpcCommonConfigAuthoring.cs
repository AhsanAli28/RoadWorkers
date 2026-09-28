using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Npc.Authoring
{
    public class NpcCommonConfigAuthoring : SyncConfigBase
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/npc.html#npc-common-config")]
        [SerializeField] private string link;

        [OnValueChanged(nameof(Sync))]
        [Expandable]
        [SerializeField] private NpcCommonSettingsConfig npcCommonSettingsConfig;

        class NpcCommonConfigAuthoringBaker : Baker<NpcCommonConfigAuthoring>
        {
            public override void Bake(NpcCommonConfigAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);

                using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
                {
                    ref var root = ref builder.ConstructRoot<NpcCommonConfig>();

                    var config = authoring.npcCommonSettingsConfig;

                    if (config != null && config.RegisterHashmap)
                    {
                        root.HashEnabled = true;
                        root.NpcHashMapCapacity = config.NpcHashMapCapacity;
                    }
                    else
                    {
                        root.HashEnabled = false;
                        root.NpcHashMapCapacity = 0;
                    }

                    var blobRef = builder.CreateBlobAssetReference<NpcCommonConfig>(Unity.Collections.Allocator.Persistent);

                    AddBlobAsset(ref blobRef, out var hash);

                    AddComponent(entity, new NpcCommonConfigReference() { Config = blobRef });
                }
            }
        }
    }
}
