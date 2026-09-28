using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct ProcessScaryRunningTag : IComponentData, IEnableableComponent
    {
        public float3 TriggerPosition;
    }

    public struct ScaryRunningTag : IComponentData, IEnableableComponent
    {
    }
}