using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Binding.Authoring
{
    public class EntityBindingConfigAuthoring : SyncConfigBase
    {
        [OnValueChanged(nameof(Sync))]
        [SerializeField] private bool bindingAvailable;

        public bool BindingAvailable => bindingAvailable;

        private class EntityBindingConfigAuthoringBaker : Baker<EntityBindingConfigAuthoring>
        {
            public override void Bake(EntityBindingConfigAuthoring authoring)
            {
                var entity = CreateAdditionalEntity(TransformUsageFlags.None);

                using (var builder = new BlobBuilder(Unity.Collections.Allocator.Temp))
                {
                    ref var root = ref builder.ConstructRoot<EntityBindingConfig>();

                    root.IsAvailable = authoring.bindingAvailable;

                    var blobRef = builder.CreateBlobAssetReference<EntityBindingConfig>(Unity.Collections.Allocator.Persistent);

                    AddBlobAsset(ref blobRef, out var hash);

                    AddComponent(entity, new EntityBindingConfigReference() { Config = blobRef });
                }
            }
        }
    }
}