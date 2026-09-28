using Unity.Entities;
using Unity.Mathematics;

namespace Spirit604.DotsCity.Simulation.Car
{
    public struct EngineDamageData : IComponentData
    {
        public int CurrentState;
        public Entity RelatedEntity;
        public float3 SpawnOffset;

        public EngineDamageData(EngineDamageBakingData carEngineDamageBakingData)
        {
            CurrentState = carEngineDamageBakingData.CurrentState;
            RelatedEntity = carEngineDamageBakingData.RelatedEntity;
            SpawnOffset = carEngineDamageBakingData.SpawnOffset;
        }
    }

    [TemporaryBakingType]
    public struct EngineDamageBakingData : IComponentData
    {
        public int CurrentState;
        public Entity RelatedEntity;
        public float3 SpawnOffset;
    }

    public struct EngineStateData
    {
        public float MinHp;
        public float MaxHp;
    }

    public struct EngineStateSettings
    {
        public bool EngineDamageEnabled;
        public BlobArray<EngineStateData> Settings;
    }

    public struct EngineStateElement : IBufferElementData
    {
        public Entity Prefab;
    }

    public struct EngineStateSettingsHolder : IComponentData
    {
        public BlobAssetReference<EngineStateSettings> SettingsReference;
    }
}
