using Unity.Entities;

namespace Spirit604.DotsCity.Events
{
    public struct TriggerNodeConfig
    {
        public bool IsActivated;
    }

    public struct TriggerNodeConfigReference : IComponentData
    {
        public BlobAssetReference<TriggerNodeConfig> Config;
    }
}