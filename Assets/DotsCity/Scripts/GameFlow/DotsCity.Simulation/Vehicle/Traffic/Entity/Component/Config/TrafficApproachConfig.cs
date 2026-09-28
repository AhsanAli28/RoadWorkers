using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct TrafficApproachConfig
    {
        public float MinApproachSpeed;
        public float MinApproachSpeedSoft;
        public float OnComingToRedLightSpeed;
        public float StoppingDistanceToLight;
        public bool AutoBrakeBeforeSpeedLimit;
        public float SoftBrakingDistance;
        public float SoftBrakingRate;
        public float BrakingDistance;
        public float SkipBrakingPathLength;
    }

    public struct TrafficApproachConfigReference : IComponentData
    {
        public BlobAssetReference<TrafficApproachConfig> Config;
    }
}