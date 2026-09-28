using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Car.Custom.Authoring
{
    [TemporaryBakingType]
    public struct VehicleWheelBaking : IComponentData
    {
        public Entity Entity;
        public float MaxSteeringAngle;
        public float DriveRate;
        public float BrakeRate;
        public float HandbrakeRate;
        public RigidTransform Origin;
        public float3 WheelOffset;
        public int InversionValue;
    }
}