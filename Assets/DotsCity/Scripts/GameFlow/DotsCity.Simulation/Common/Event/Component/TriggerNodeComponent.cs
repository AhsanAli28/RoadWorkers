using Unity.Entities;

namespace Spirit604.DotsCity.Events
{
    public struct TriggerNodeComponent : IComponentData
    {
        public bool DestroyEntity;
    }
}