using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct NpcDeathEventData : IComponentData
    {
        public float3 Position;
    }
}