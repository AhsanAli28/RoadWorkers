using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Road
{
    public struct TrafficNodeCapacityComponent : IComponentData
    {
        public int Capacity;
        public Entity PedestrianNodeEntity;
        public Entity CarEntity;

        public bool HasCar() => CarEntity != Entity.Null;

        public bool HasSlots() => Capacity == -1 || Capacity > 0;
    }
}
