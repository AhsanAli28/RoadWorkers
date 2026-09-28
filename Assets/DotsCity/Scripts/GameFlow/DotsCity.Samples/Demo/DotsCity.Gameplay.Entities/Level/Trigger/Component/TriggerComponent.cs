using Unity.Collections;
using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Level
{
    public struct TriggerComponent : IComponentData
    {
        public TriggerInteractType InteractType;
        public TriggerType TriggerType;
        public bool IsClosed;
        public bool AvailableByDefault;
    }

    public struct LoadSceneDataComponent : IComponentData
    {
        public FixedString32Bytes SceneName;
    }
}