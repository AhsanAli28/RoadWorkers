using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Car.Custom
{
    public struct WheelDebugShared : ISharedComponentData
    {
        public bool ShowDebug;
    }

    public struct WheelDebug
    {
        public bool IsInContact;
        public float3 Start;
        public float3 End;
    }
}
