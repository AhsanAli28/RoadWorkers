using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct TrafficRailConfig
    {
        public float MaxDistanceToRailLine;
        public float LateralSpeed;
        public float RotationLerpSpeed;
        public float TrainRotationLerpSpeed;
        public bool LerpTram;
        public bool LerpTraffic;
        public float2 ConvergenceSpeedRate;
        public float Acceleration;
        public float BrakePower;
    }

    public struct TrafficRailConfigReference : IComponentData
    {
        public BlobAssetReference<TrafficRailConfig> Config;
    }
}
