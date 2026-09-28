using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct NodeCanSpawnInVisionTag : IComponentData
    {
    }

    public struct PedestrianSectionData : IComponentData
    {
        public int NodeHash;
    }
}
