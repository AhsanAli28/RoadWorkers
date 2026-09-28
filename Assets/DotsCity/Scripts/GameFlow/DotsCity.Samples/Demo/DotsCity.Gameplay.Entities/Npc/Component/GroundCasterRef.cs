using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Npc
{
    public struct GroundCasterRef : IComponentData
    {
        public Entity CasterEntity;
    }

    [TemporaryBakingType]
    public struct GroundCasterBakingData : IComponentData
    {
        public uint CastingLayer;
    }
}