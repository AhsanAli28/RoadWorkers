using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Car
{
    public struct CarStartExplodeComponent : IComponentData
    {
        public int ExplodeIsEnabled;
        public int VfxIsCreated;
        public float3 Offset;
        public float EnableTimeStamp;
        public bool IsPooled;
    }

    public struct CarExplodeRequestedTag : IComponentData { }

    public struct CarExplodeVfxProcessedTag : IComponentData { }
}