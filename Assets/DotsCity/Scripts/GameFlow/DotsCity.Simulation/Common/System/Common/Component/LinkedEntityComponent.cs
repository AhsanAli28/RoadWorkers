using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Common
{
    public struct LinkedEntityComponent : IComponentData
    {
        public Entity LinkedEntity;
    }
}