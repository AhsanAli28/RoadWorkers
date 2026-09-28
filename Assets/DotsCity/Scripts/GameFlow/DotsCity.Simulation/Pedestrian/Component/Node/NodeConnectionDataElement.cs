using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [InternalBufferCapacity(0)]
    public struct NodeConnectionDataElement : IBufferElementData
    {
        public Entity ConnectedEntity;
        public float SumWeight;
    }

    [InternalBufferCapacity(0)]
    public struct NodeSectionConnectionDataElement : IBufferElementData
    {
        public int ConnectedHash;
    }
}