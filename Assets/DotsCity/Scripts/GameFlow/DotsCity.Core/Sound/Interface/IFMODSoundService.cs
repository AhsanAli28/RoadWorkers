#if FMOD
using FMOD.Studio;

namespace Spirit604.DotsCity.Core.Sound
{
    public interface IFMODSoundService : ISoundService
    {
        EventDescription GetEventDescription(SoundData soundData);
        PARAMETER_DESCRIPTION GetFloatParameterDescription(SoundData soundData, EventFloatParameter parameter);
    }
}
#endif