using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Npc
{
    public struct NpcGroundConfig
    {
        public float CastDistance;
        public float StopFallingDistance;
        public float FallingDistance;
        public float GroundedDistance;
    }

    public struct NpcGroundConfigReference : IComponentData
    {
        public BlobAssetReference<NpcGroundConfig> Config;
    }
}