using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Events.Authoring
{
    public class TriggerNodeConfigAuthoring : SyncConfigBase
    {
        [OnValueChanged(nameof(Sync))]
        [Tooltip("Activate this config & set the trigger type to the desired traffic or pedestrian node. Event can be retrieved from <b>TriggerNodeService</b>")]
        [SerializeField] private bool isActivated;

        public bool IsActivated => isActivated;

        private class TriggerNodeConfigAuthoringBaker : Baker<TriggerNodeConfigAuthoring>
        {
            public override void Bake(TriggerNodeConfigAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);

                using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
                {
                    ref var root = ref builder.ConstructRoot<TriggerNodeConfig>();

                    root.IsActivated = authoring.isActivated;

                    var blobRef = builder.CreateBlobAssetReference<TriggerNodeConfig>(Unity.Collections.Allocator.Persistent);

                    AddBlobAsset(ref blobRef, out var hash);

                    AddComponent(entity, new TriggerNodeConfigReference() { Config = blobRef });
                }
            }
        }
    }
}