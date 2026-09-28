using Spirit604.Gameplay.Road;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Road
{
    public struct LightFrameHandlerEntityComponent : IComponentData
    {
        public Entity RelatedEntityHandler;
    }

    public struct LightFrameHandlerStateComponent : IComponentData
    {
        public LightState CurrentLightState;
    }
}

