using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct BenchConfig
    {
        public float MinIdleTime;
        public float MaxIdleTime;
        public float EntryDistance;
        public float ExitIdleDuration;
        public float SittingMovementSpeed;
        public float SittingRotationSpeed;
        public float SitPointDistanceSQ;
    }

    public struct BenchConfigReference : IComponentData
    {
        public BlobAssetReference<BenchConfig> Config;
    }
}
