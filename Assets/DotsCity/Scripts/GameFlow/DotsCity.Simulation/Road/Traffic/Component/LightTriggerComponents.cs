using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Road
{
    public struct LightTriggerEnabledTag : IComponentData, IEnableableComponent { }

    public struct LightTriggerBinding : IComponentData
    {
        public Entity TriggerEntity;
    }

    public struct LightTrigger : IComponentData
    {
        public bool Initialized;
        public bool Passed;
        public float TriggerDistanceSQ;
        public Entity SourceLightEntity;
    }

    [InternalBufferCapacity(1)]
    public struct LightTriggerRelatedEntity : IBufferElementData
    {
        public Entity LightEntity;
    }
}