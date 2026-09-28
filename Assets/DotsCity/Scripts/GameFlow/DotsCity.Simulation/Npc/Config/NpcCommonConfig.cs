using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Npc
{
    public struct NpcCommonConfig
    {
        public bool HashEnabled;
        public int NpcHashMapCapacity;
    }

    public struct NpcCommonConfigReference : IComponentData
    {
        public BlobAssetReference<NpcCommonConfig> Config;
    }
}