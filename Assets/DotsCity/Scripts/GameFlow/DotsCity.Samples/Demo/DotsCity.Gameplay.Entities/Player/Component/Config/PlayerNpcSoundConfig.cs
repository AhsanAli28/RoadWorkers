using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Sound.Player
{
    public struct PlayerNpcSoundConfig
    {
        public int FootstepsSoundId;
        public float FootstepFrequency;
    }

    public struct PlayerNpcSoundConfigReference : IComponentData
    {
        public BlobAssetReference<PlayerNpcSoundConfig> Config;
    }
}
