#if FMOD
using FMOD.Studio;
using Unity.Entities;

namespace Spirit604.DotsCity.Simulation.Sound
{
    public struct FMODSound : ICleanupComponentData
    {
        public EventInstance Event;
    }

    public struct FMODFloatParameter : IBufferElementData
    {
        public PARAMETER_ID ParameterId;
        public float CurrentValue;
    }
}
#endif