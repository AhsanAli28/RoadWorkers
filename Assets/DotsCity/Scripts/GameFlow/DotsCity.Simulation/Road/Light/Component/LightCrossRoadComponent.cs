using Spirit604.Gameplay.Road;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Road
{
    public struct LightHandlerComponent : IComponentData
    {
        public int CrossRoadIndex;
        public float NextSwitchTime;
        public float CycleDuration;
        public LightState State;
        public int StateIndex;

        public LightHandlerComponent SetState(LightState state)
        {
            this.State = state;
            return this;
        }

        public LightHandlerComponent SetState(int state)
        {
            this.State = (LightState)state;
            return this;
        }
    }

    [InternalBufferCapacity(0)]
    public struct LightHandlerStateElement : IBufferElementData
    {
        public LightState LightState;
        public float Duration;
    }

    public struct LightHandlerID : IComponentData
    {
        public int Value;
    }

    public struct LightHandlerOverrideStateTag : IComponentData { }

    public struct LightHandlerInitTag : IComponentData, IEnableableComponent { }

    public struct LightHandlerStateUpdateTag : IComponentData, IEnableableComponent { }
}
