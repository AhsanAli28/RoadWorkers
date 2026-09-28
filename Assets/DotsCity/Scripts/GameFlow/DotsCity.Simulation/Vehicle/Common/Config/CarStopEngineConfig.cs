using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Car
{
    public struct CarStopEngineConfig
    {
        public bool HasStopEngine;
        public float StoppingDuration;
        public float IdleAfterStopping;
        public float TargetMinPitch;
        public float TargetMinVolume;
    }

    public struct CarStopEngineConfigReference : IComponentData
    {
        public BlobAssetReference<CarStopEngineConfig> Config;
    }
}