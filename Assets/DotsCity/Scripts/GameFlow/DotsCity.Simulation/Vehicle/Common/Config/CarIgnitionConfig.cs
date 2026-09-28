using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Car
{
    public struct CarIgnitionConfig
    {
        public bool HasIgnition;
        public float IdleBeforeStart;
        public float IgnitionDuration;
        public float StartedTimeDuration;
        public float MaxPitch;
        public float MaxVolume;
        public BlobArray<float> EngineStartedPitchCurve;
        public BlobArray<float> EngineStartedVolumeCurve;
    }

    public struct CarIgnitionConfigReference : IComponentData
    {
        public BlobAssetReference<CarIgnitionConfig> Config;
    }
}