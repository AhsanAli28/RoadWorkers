using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Navigation
{
    public struct PathAgentComponent : IComponentData
    {
        public PathStatus Status;
    }
}
