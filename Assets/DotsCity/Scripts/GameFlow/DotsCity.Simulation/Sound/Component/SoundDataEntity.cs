#if FMOD
using FMOD.Studio;

namespace Spirit604.DotsCity.Simulation.Sound
{
    public struct SoundDataEntity
    {
        public EventDescription EventDescription;
        public int StartParamIndex;
        public int EndParamIndex;

        public int ParamCount => StartParamIndex == -1 ? 0 : EndParamIndex - StartParamIndex + 1;
    }
}
#endif