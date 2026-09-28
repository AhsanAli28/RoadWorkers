using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct PathLocalAvoidanceEnabledTag : IComponentData, IEnableableComponent
    {
    }

    public struct LocalAvoidanceAgentTag : IComponentData
    {
    }

    [InternalBufferCapacity(2)]
    public struct PathPointAvoidanceElement : IBufferElementData
    {
        public float3 Point;
    }
}