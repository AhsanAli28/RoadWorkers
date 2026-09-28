using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public enum ParkingIdleType { WaitForPedestrian, TempIdleAndStart }

    public struct TrafficParkingConfig
    {
        public ParkingIdleType ParkingIdleType;
        public float2 IdleDuration;
        public bool AligmentAtNode;
        public float RotationSpeed;
        public float CompleteAngle;
        public bool PrecisePosition;
        public float MovementSpeed;
        public float AchieveDistanceSQ;
    }

    public struct TrafficParkingConfigReference : IComponentData
    {
        public BlobAssetReference<TrafficParkingConfig> Config;
    }
}