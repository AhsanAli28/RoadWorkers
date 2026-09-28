using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct MonoAdapterComponent : IComponentData
    {
        public float3 Position;
        public quaternion Rotation;
        public bool Synced;
        public bool Interpolate;
    }
}