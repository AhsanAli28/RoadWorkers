using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct ScaryTriggerConfig
    {
        public float DeathTriggerSqDistance;
        public float DeathTriggerDuration;
        public bool HasScreamSound;
        public int ScreamEntityLimit;
        public float ChanceToScream;
        public float MinScreamDelay;
        public float MaxScreamDelay;
        public int ScreamSoundId;
    }

    public struct ScaryTriggerConfigReference : IComponentData
    {
        public BlobAssetReference<ScaryTriggerConfig> Config;
    }
}
