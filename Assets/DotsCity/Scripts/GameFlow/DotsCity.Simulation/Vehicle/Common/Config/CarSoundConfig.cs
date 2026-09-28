using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Car.Sound
{
    public struct CarSoundCommonConfig
    {
        public int CollisionSoundId;
        public int CarExplodeSoundId;
        public int BulletHitSoundId;
        public int NpcHitSoundId;
    }

    public struct CarSoundCommonConfigReference : IComponentData
    {
        public BlobAssetReference<CarSoundCommonConfig> Config;
    }
}
