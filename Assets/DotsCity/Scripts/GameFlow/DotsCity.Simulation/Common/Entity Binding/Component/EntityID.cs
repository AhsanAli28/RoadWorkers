using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Binding
{
    public struct EntityID : IComponentData
    {
        public int Value;
    }

    public struct EntityBindingCleanup : ICleanupComponentData
    {
        public int Value;
    }

    public struct EntityIDInitTag : IComponentData, IEnableableComponent { }

    [TemporaryBakingType]
    public struct EntityIDBakingData : IComponentData
    {
        public int Value;
    }
}