using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.TrafficArea
{
    public struct TrafficAreaLinked : ICleanupComponentData
    {
        public Entity AreaEntity;
    }

    public struct TrafficAreaAlignedTag : IComponentData
    {
    }

    public struct TrafficWaitForExitTag : IComponentData, IEnableableComponent
    {
    }

    public struct TrafficMovingToExitTag : IComponentData, IEnableableComponent
    {
    }

    public struct TrafficWaitForEnterAreaTag : IComponentData, IEnableableComponent
    {
    }
}