using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Car
{
    public struct BlobSoundData
    {
        public int SoundId;
    }

    public struct BlobEngineSoundData
    {
        public float MinPitch;
        public float MaxPitch;
        public float MaxLoadSpeed;
        public float MaxVolumeSpeed;
        public float MinVolume;
    }

    public struct CarSoundConfig
    {
        public BlobArray<BlobSoundData> Sounds;
    }

    public struct CarSharedConfig
    {
        public BlobArray<CarSoundConfig> SoundConfigs;
        public BlobArray<BlobEngineSoundData> SoundEngineConfigs;
        public BlobArray<BlobCarData> CarDatas;
    }

    public struct BlobCarData
    {
        public int SoundConfigIndex;
        public int SoundEngineIndex;
    }

    public struct CarSharedDataConfigReference : IComponentData
    {
        public BlobAssetReference<CarSharedConfig> Config;
    }
}