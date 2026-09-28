using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Gameplay.Npc
{
    public struct InputComponent : IComponentData
    {
        public float3 ShootDirection;
        public float3 ShootInput;
        public float2 MovingInput;
    }
}
