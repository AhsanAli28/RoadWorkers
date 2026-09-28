using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.TrafficArea
{
    public struct TrafficAreaNode : IComponentData
    {
        public TrafficAreaNodeType TrafficAreaNodeType;
        public Entity AreaEntity;
    }

    public struct TrafficAreaEntryNodeComponent : IComponentData
    {
        public Entity TrafficEntity;
    }

    public struct TrafficAreaProcessEnteredNodeTag : IComponentData, IEnableableComponent
    {
    }
}