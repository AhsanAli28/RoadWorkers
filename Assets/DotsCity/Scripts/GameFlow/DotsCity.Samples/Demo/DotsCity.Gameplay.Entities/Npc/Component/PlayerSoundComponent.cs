using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Sound.Player
{
    public struct PlayerSoundComponent : IComponentData
    {
        public Entity FootstepsSound;
        public bool FootstepsPlaying;
        public float FootstepTimestep;
        public float FootstepFrequency;
    }
}
