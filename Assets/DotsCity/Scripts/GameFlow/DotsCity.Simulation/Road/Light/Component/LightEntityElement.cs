using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Road
{
    public struct LightFrameData : IComponentData
    {
        public float3 IndexPosition;
        public Entity RedEntity;
        public Entity YellowEntity;
        public Entity GreenEntity;
    }
}