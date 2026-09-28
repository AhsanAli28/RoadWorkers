using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Npc.Navigation
{
    public struct NpcNavAgentComponent : IComponentData
    {
        public int PathCalculated;
    }
}