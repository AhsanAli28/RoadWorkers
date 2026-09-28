using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct TriggerConfig
    {
        public int TriggerHashMapCapacity;
        public float TriggerHashMapCellSize;
        public BlobArray<TriggerDataConfig> TriggerDataConfigs;
    }

    [System.Serializable]
    public struct TriggerDataConfig
    {
        [Range(0, 600f)]
        public float ImpactTriggerDuration;
    }

    public struct TriggerConfigReference : IComponentData
    {
        public BlobAssetReference<TriggerConfig> Config;
    }
}
