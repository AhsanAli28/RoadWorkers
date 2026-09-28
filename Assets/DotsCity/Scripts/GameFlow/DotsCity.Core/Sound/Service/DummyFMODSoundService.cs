#if FMOD
using FMOD.Studio;
using System.Collections.Generic;
#endif

namespace Spirit604.DotsCity.Core.Sound
{
#if FMOD
    public class DummyFMODSoundService : IFMODSoundService
    {
        List<SoundData> ISoundService.Sounds => throw new System.NotImplementedException();

        public EventDescription GetEventDescription(SoundData soundData)
        {
            return default;
        }

        public PARAMETER_DESCRIPTION GetFloatParameterDescription(SoundData soundData, EventFloatParameter parameter)
        {
            return default;
        }

        public SoundData GetSoundById(int id)
        {
            return null;
        }

        public void Initialize()
        {
        }

        public void Mute()
        {
        }

        public void Unmute()
        {
        }
    }
#endif
}