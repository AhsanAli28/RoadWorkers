using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Level.Streaming
{
    public struct RoadSectionData : IComponentData
    {
        public int SegmentHash;
        public int SectionIndex;
        public float3 Position;
    }
}
