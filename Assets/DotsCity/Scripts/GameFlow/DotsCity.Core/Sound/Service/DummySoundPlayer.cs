using UnityEngine;

namespace Spirit604.DotsCity.Core.Sound
{
    public class DummySoundPlayer : ISoundPlayer
    {
        public void PlayOneShot(SoundData soundData, Vector3 position, float volume = 1)
        {
        }

        public void PlayOneShot(int id, Vector3 position, float volume = 1)
        {
        }
    }
}