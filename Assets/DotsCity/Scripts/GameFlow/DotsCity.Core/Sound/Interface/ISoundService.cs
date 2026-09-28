using System.Collections.Generic;

namespace Spirit604.DotsCity.Core.Sound
{
    public interface ISoundService
    {
        List<SoundData> Sounds { get; }
        SoundData GetSoundById(int id);
        void Initialize();
        void Mute();
        void Unmute();
    }
}