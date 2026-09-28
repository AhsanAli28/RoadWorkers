using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Traffic
{
    public struct VehicleLightComponent : IComponentData
    {
        public BlinkerState BlinkerState;
        public bool Reverse;
        public bool Braking;
        public bool EngineOn;
    }

    public enum BlinkerState { None, Left, Right }
}