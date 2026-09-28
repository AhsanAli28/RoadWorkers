using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Sound.Pedestrian
{
    public struct SoundConfig
    {
        public int DeathSoundId;
        public int EnterTramSoundId;
        public int ExitTramSoundId;
    }

    public struct SoundConfigReference : IComponentData
    {
        public BlobAssetReference<SoundConfig> Config;
    }
}
