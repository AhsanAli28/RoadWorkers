using Spirit604.DotsCity.Simulation.Car.Sound;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Car
{
    public static class CarSharedConfigUtils
    {
        public static int GetSoundID(ref this BlobAssetReference<CarSharedConfig> config, int carIndex, CarSoundType soundIndex)
        {
            return GetSoundID(ref config, carIndex, (int)soundIndex);
        }

        public static int GetSoundID(ref this BlobAssetReference<CarSharedConfig> config, int carIndex, int soundIndex)
        {
            if (config.Value.CarDatas.Length > carIndex)
            {
                var carData = config.Value.CarDatas[carIndex];
                var configIndex = carData.SoundConfigIndex;

                if (config.Value.SoundConfigs.Length > configIndex)
                {
                    ref var soundConfigData = ref config.Value.SoundConfigs[configIndex];

                    if (soundConfigData.Sounds.Length > soundIndex)
                    {
                        return soundConfigData.Sounds[soundIndex].SoundId;
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"CarSharedConfigUtils. Sound CarIndex {carIndex} has {soundConfigData.Sounds.Length} sounds. Required {soundIndex} soundIndex");
                    }
                }
                else
                {
                    UnityEngine.Debug.LogError($"CarSharedConfigUtils. Sound CarIndex {carIndex} ConfigIndex {configIndex} config not exist. Config count {config.Value.SoundConfigs.Length}");
                }
            }
            else
            {
                UnityEngine.Debug.LogError($"CarSharedConfigUtils. Sound CarIndex {carIndex} not exist");
            }

            return -1;
        }

        public static BlobEngineSoundData GetEngineData(this ref CarSharedDataConfigReference config, int modelIndex)
        {
            int engineIndex = -1;

            if (config.Config.Value.CarDatas.Length > modelIndex)
            {
                engineIndex = config.Config.Value.CarDatas[modelIndex].SoundEngineIndex;

                if (config.Config.Value.SoundEngineConfigs.Length > engineIndex)
                {
                    return config.Config.Value.SoundEngineConfigs[engineIndex];
                }
            }

            UnityEngine.Debug.LogError($"CarSharedConfigUtils. CarModel {modelIndex} Engine index {engineIndex} engine config not exist");

            return config.Config.Value.SoundEngineConfigs[0];
        }
    }
}