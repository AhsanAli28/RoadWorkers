using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Gameplay.Npc
{
    public struct NpcInitializeCustomDestinationTag : IComponentData
    {
    }

    public struct NpcCustomDestinationComponent : IComponentData
    {
        public float3 Destination;
        public quaternion DstRotation;
    }

    public struct NpcCustomReachComponent : IComponentData
    {
    }

    public struct ResetNpcCustomDestinationTag : IComponentData
    {
    }
}