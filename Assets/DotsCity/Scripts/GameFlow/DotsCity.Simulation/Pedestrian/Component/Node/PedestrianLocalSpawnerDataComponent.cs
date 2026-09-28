using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct PedestrianLocalSpawnerDataComponent : IComponentData
    {
        public int LocalSpawnerInstanceId;
        public int LocalIndex;
    }
}