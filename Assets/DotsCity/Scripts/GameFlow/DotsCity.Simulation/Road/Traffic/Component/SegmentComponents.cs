using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Road
{
    public struct SegmentComponent : IComponentData
    {
        public int SectionIndex;
        public int SegmentHash;
    }

    public struct SegmentInitTag : IComponentData, IEnableableComponent { }

    public struct SegmentUnloadTag : IComponentData, IEnableableComponent { }

    [InternalBufferCapacity(0)]
    public struct SegmentTrafficNodeData : IBufferElementData
    {
        public Entity Entity;
    }

    [InternalBufferCapacity(0)]
    public struct SegmentPedestrianNodeData : IBufferElementData
    {
        public Entity Entity;
    }
}