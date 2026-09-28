using System;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    [Flags]
    public enum TrafficAdditionalSettings : byte
    {
        None = 0,
        HasRotationLerp = 1 << 0,
        HasRailRotationLerp = 1 << 1,
    }

    public struct TrafficSettingsComponent : IComponentData
    {
        public float MaxSpeed;
        public float Acceleration;
        public float BackwardAcceleration;
        public float BrakePower;
        public float MaxSteerAngle;
        public float MaxSteerDirectionAngle;
        public float SteeringDamping;
        public float OffsetY;
        public TrafficAdditionalSettings AdditionalSettings;
    }
}