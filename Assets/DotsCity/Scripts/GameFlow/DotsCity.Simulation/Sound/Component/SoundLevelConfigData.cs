using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Sound
{
    public struct SoundLevelConfigData
    {
        public bool HasSounds;
        public bool TrafficHasSounds;
        public bool RandomHornsSound;
    }

    public struct SoundLevelConfigReference : IComponentData
    {
        public BlobAssetReference<SoundLevelConfigData> Config;
    }
}