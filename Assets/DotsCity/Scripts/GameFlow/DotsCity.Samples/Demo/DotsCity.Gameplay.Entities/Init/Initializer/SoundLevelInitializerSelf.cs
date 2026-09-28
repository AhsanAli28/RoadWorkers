using Spirit604.DotsCity.Core.Sound;
using Spirit604.DotsCity.Simulation.Initialization;
using Spirit604.DotsCity.Simulation.Sound;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Initialization
{
    public class SoundLevelInitializerSelf : SoundLevelInitializer
    {
        [SerializeField] private FMODSoundService fmodSoundService;
        [SerializeField] private SoundLevelConfig customSoundLevelConfig;

#if FMOD 
        private void Awake()
        {
            Construct(fmodSoundService, null, customSoundLevelConfig);
            PreInitialize();
            Initialize();
            LateInitialize();
        }
#endif
    }
}