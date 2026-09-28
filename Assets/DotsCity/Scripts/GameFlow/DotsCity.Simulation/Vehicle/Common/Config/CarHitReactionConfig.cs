using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Car
{
    public struct CarHitReactionConfig
    {
        public int PoolSize;
        public float EffectDuration;
        public float LerpSpeed;
        public float MaxLerp;
        public float DivHorizontalRate;
        public float DivVerticalRate;
    }

    public struct CarHitReactionConfigReference : IComponentData
    {
        public BlobAssetReference<CarHitReactionConfig> Config;
    }
}