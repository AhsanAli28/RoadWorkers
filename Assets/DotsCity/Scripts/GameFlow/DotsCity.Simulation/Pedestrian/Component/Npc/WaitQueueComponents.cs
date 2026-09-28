using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [InternalBufferCapacity(0)]
    public struct WaitQueueElement : IBufferElementData
    {
        public Entity PedestrianEntity;
        public bool Activated;
    }

    public struct WaitQueueComponent : IComponentData
    {
        public float LastActiveTimeStamp;
        public int ActivatedCount;
    }

    public struct NodeProcessWaitQueueTag : IComponentData, IEnableableComponent
    {
    }

    public struct WaitQueueLinkedComponent : ICleanupComponentData
    {
        public Entity NodeEntity;
    }
}