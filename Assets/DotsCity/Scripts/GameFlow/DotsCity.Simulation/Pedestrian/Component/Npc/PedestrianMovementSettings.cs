using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct PedestrianMovementSettings : IComponentData
    {
        public float WalkingValue;
        public float RunningValue;
        public float RotationSpeed;
        public float CurrentMovementSpeed;
        public float CurrentMovementSpeedSQ;
    }
}
