using Spirit604.DotsCity.Core;
using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Car
{
    public struct CarHashEntityComponent : IComponentData
    {
        public Entity Entity;
        public int CarModel;
        public float3 Position;
        public quaternion Rotation;
        public float3 BoundsExtents;
        public float3 Velocity;
        public int Health;
        public FactionType FactionType;

        public float3 BoundsSize => BoundsExtents * 2;
    }
}