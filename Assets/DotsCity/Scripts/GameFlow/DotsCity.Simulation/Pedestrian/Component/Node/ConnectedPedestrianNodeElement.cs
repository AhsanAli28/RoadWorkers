using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [InternalBufferCapacity(1)]
    public struct ConnectedPedestrianNodeElement : IBufferElementData
    {
        public Entity PedestrianNodeEntity;
    }
}