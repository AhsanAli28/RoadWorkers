using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Road
{
    public struct TrafficNodeComponent : IComponentData, IEnableableComponent
    {
        public int CrossRoadIndex;
        public Entity LightEntity;
    }
}