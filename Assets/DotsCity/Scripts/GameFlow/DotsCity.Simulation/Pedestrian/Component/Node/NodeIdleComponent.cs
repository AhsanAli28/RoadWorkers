using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct NodeIdleComponent : IComponentData
    {
        public float MinIdleTime;
        public float MaxIdleTime;
    }
}
