using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.TrafficPublic
{
    public struct TrafficPublicSpawnerSettings
    {
        public float SpawnFrequency;
        public int RouteHashMapCapacity;
    }

    public struct TrafficPublicSpawnerSettingsReference : IComponentData
    {
        public BlobAssetReference<TrafficPublicSpawnerSettings> Config;
    }
}