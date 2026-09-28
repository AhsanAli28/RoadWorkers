using Spirit604.Attributes;
using Spirit604.Extensions;
using UnityEngine;

namespace Spirit604.DotsCity.Core.Sound
{
    public class SoundManager : SingletonMonoBehaviour<SoundManager>
    {
        private ISoundPlayer _player;

        [InjectWrapper]
        public void Construct(ISoundPlayer player)
        {
            _player = player;
        }

        public void PlayOneShot(SoundData soundData, Vector3 position, float volume = 1f)
        {
            if (!soundData)
                return;

            _player.PlayOneShot(soundData, position, volume);
        }
    }
}
