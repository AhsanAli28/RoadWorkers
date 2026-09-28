using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct DestinationDistanceComponent : IComponentData
    {
        public float DestinationDistanceSQ;
    }
}