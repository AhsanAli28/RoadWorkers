using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Level.Streaming
{
    public struct PedestrianNodeAwaitingData
    {
        /// <summary> PedestrianNode entity waiting for missing nodes. </summary>
        public Entity AwaitingEntity;
        public int LocalConnectionIndex;
    }
}