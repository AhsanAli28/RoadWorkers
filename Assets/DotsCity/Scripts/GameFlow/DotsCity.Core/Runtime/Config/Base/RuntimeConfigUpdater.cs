using Unity.Entities;

namespace Spirit604.DotsCity.Core
{
    public abstract class RuntimeConfigUpdater<T> : RuntimeConfigAwaiter<T> where T : unmanaged, IComponentData
    {
        protected override void ConvertInternal(Entity entity, EntityManager dstManager)
        {
            dstManager.AddComponentData(entity, CreateConfig());
        }

        public abstract T CreateConfig();
    }

    public abstract class RuntimeConfigUpdater<T, TBlobRef> : RuntimeConfigAwaiter<T>
        where T : unmanaged, IComponentData
        where TBlobRef : unmanaged
    {
        private BlobAssetReference<TBlobRef> blobRef;

        public override void Dispose()
        {
            base.Dispose();

            if (blobRef != null && blobRef.IsCreated)
            {
                blobRef.Dispose();
                blobRef = default;
            }
        }

        public abstract T CreateConfig(BlobAssetReference<TBlobRef> blobRef);

        public T CreateConfig(IBaker baker)
        {
            var blobRef = CreateConfigBlob();
            baker.AddBlobAsset(ref blobRef, out var hash);
            return CreateConfig(blobRef);
        }

        public virtual T CreateConfig()
        {
            var blobRef = CreateConfigBlob();
            return CreateConfig(blobRef);
        }

        protected override void ConvertInternal(Entity entity, EntityManager dstManager)
        {
            blobRef = CreateConfigBlob();
            dstManager.AddComponentData(entity, CreateConfig(blobRef));
        }

        protected abstract BlobAssetReference<TBlobRef> CreateConfigBlob();
    }
}
