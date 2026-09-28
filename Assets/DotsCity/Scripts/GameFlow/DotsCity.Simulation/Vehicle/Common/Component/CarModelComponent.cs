using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Car
{
    public struct CarModelComponent : IComponentData
    {
        public int Value;
        public int LocalIndex;
    }
}