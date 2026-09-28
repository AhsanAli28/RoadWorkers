using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Common
{
    public struct SimpleRouteFollowerComponent : IComponentData
    {
        public int NodeIndex;
    }

    public struct SimpleRouteFollowerSettingsComponent : IComponentData
    {
        public float MovementSpeed;
        public float AchieveDistance;
    }

    public struct SimpleRouteElement : IBufferElementData
    {
        public float3 Position;
    }
}