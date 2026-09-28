#if !FMOD
using System.Collections.Generic;

namespace Spirit604.DotsCity.Core.Sound
{
    public class DummySoundService : ISoundService
    {
        List<SoundData> ISoundService.Sounds => throw new System.NotImplementedException();

        SoundData ISoundService.GetSoundById(int id)
        {
            throw new System.NotImplementedException();
        }

        void ISoundService.Initialize()
        {
        }

        void ISoundService.Mute()
        {
        }

        void ISoundService.Unmute()
        {
        }
    }
}
#endif