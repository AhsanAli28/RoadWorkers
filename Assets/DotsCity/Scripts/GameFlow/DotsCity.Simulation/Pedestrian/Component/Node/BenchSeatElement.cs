using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [InternalBufferCapacity(0)]
    public struct BenchSeatElement : IBufferElementData
    {
        public Entity Seat;
    }
}
