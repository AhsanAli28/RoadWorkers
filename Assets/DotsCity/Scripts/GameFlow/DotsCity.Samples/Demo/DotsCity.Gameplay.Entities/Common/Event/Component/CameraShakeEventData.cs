using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Gameplay.Events
{
    public struct CameraShakeEventData : IComponentData
    {
        public float3 Position;
    }
}