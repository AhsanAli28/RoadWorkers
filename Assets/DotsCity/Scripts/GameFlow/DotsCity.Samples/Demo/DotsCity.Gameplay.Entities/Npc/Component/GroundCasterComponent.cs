using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Npc
{
    public struct GroundCasterComponent : IComponentData
    {
        public bool Hit;
        public uint CastingLayer;
        public float Distance;
    }
}