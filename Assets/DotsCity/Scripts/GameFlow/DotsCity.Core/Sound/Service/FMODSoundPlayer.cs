#if FMOD
using FMODUnity;
using UnityEngine;

namespace Spirit604.DotsCity.Core.Sound
{
    public class FMODSoundPlayer : ISoundPlayer
    {
        private IFMODSoundService fMODSoundService;

        public FMODSoundPlayer(IFMODSoundService fMODSoundService)
        {
            this.fMODSoundService = fMODSoundService;
        }

        public void PlayOneShot(SoundData soundData, Vector3 position, float volume = 1f)
        {
            if (!soundData) return;

            var instance = RuntimeManager.CreateInstance(soundData.Name);

            if (volume != 1f)
                instance.setVolume(volume);

            instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
            instance.start();
            instance.release();
        }

        public void PlayOneShot(int id, Vector3 position, float volume = 1)
        {
            var data = fMODSoundService.GetSoundById(id);
            PlayOneShot(data, position, volume);
        }
    }
}
#endif