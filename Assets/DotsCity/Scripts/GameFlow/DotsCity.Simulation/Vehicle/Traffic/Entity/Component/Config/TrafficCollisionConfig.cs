using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct TrafficCollisionConfig
    {
        public float IdleDuration;
        public bool AvoidStuckedCollision;
        public float IgnoreCollisionDuration;
        public float CollisionDuration;
        public float CalculationCollisionFrequency;
        public float RepeatAvoidanceFrequency;
        public float ForwardDirectionValue;
        public float SideDirectionValue;
        public float StuckDistance;
        public float StuckDuration;
        public float PostActivationDelay;
        public float AvoidanceDistance;
        public float ReverseDriveMaxDuration;
    }

    public struct TrafficCollisionConfigReference : IComponentData
    {
        public BlobAssetReference<TrafficCollisionConfig> Config;
    }
}
