using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct PedestrianSpawnSettings
    {
        public int MinPedestrianCount;
        public float MaxPedestrianPerNode;
        public float MinSpawnDelay;
        public float MaxSpawnDelay;
    }

    public struct PedestrianSpawnSettingsReference : IComponentData
    {
        public BlobAssetReference<PedestrianSpawnSettings> Config;
    }
}
