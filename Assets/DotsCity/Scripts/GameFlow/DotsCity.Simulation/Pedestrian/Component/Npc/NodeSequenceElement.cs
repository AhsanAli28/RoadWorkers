using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    /// <summary>
    /// Sequence of specified target nodes, e.g. specified by a custom navigation system, required CustomReachTargetTag to be held by pedestrian.
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct NodeSequenceElement : IBufferElementData
    {
        public Entity Entity;
    }
}
