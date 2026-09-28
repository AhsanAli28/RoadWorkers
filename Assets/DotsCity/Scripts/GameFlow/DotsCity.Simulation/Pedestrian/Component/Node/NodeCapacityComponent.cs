using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct NodeHasCapacityOptionTag : IComponentData { }

    public struct NodeCapacityComponent : IComponentData
    {
        public int MaxAvailableCount;
        public int CurrentCount;

        public bool IsAvailable() => MaxAvailableCount == -1 || CurrentCount > 0;

        public NodeCapacityComponent Enter()
        {
            if (MaxAvailableCount == -1)
            {
                return this;
            }

            return new NodeCapacityComponent() { MaxAvailableCount = this.MaxAvailableCount, CurrentCount = math.clamp(--this.CurrentCount, 0, MaxAvailableCount) };
        }

        public NodeCapacityComponent Leave()
        {
            if (MaxAvailableCount == -1)
            {
                return this;
            }

            this.CurrentCount = math.clamp(++this.CurrentCount, -1, MaxAvailableCount);

            return this;
        }
    }
}