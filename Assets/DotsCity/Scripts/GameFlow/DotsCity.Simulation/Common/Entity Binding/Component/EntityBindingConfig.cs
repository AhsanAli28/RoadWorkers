using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Binding
{
    public struct EntityBindingConfig
    {
        public bool IsAvailable;
    }

    public struct EntityBindingConfigReference : IComponentData
    {
        public BlobAssetReference<EntityBindingConfig> Config;
    }
}