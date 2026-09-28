using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct DefaultWheelData : IComponentData
    {
        public Entity VehicleEntity;
        public quaternion InitialRotation;
        public float WheelBase;
        public bool Steering;
        public sbyte InverseValue;
        public float Angle;
    }
}
