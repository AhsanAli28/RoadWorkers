using Spirit604.DotsCity.Core.Sound;
using Spirit604.DotsCity.Simulation.Sound;
using UnityEngine;

#if ZENJECT
using Zenject;
#else
using Spirit604.DotsCity.Simulation.Initialization;
#endif

namespace Spirit604.DotsCity.Installer
{
    public class SoundInstaller :
#if ZENJECT
        MonoInstaller
#else
        ManualReferenceInstaller
#endif
    {
        [SerializeField] private SoundLevelConfigHolder soundLevelConfigHolder;

        [SerializeField] private FMODSoundService fmodSoundService;

        [SerializeField] private BuiltInSoundService builtInSoundService;

#if !ZENJECT

        [Header("Resolve")]
        [EditorResolve][SerializeField] private SoundLevelInitializer soundLevelInitializer;
        [EditorResolve][SerializeField] private SoundManager soundManager;

        private ISoundService soundService;
        private ISoundPlayer soundPlayer;

        public ISoundService SoundService
        {
            get
            {
                if (soundService == null)
                    InitSound();

                return soundService;
            }

        }

        public ISoundPlayer SoundPlayer
        {
            get
            {
                if (soundPlayer == null)
                    InitSound();

                return soundPlayer;
            }
        }

#endif

#if ZENJECT
        public override void InstallBindings()
        {
            Container.Bind<SoundLevelConfig>().FromInstance(soundLevelConfigHolder.SoundLevelConfig).AsSingle();

            var hasSounds = soundLevelConfigHolder.SoundLevelConfig.HasSounds;

#if FMOD
            if (hasSounds)
            {
                Container.BindInterfacesTo<FMODSoundService>().FromInstance(fmodSoundService).AsSingle();
                Container.Bind<ISoundPlayer>().To<FMODSoundPlayer>().AsSingle();
            }
#else

            if (hasSounds)
            {
                Container.BindInterfacesAndSelfTo<BuiltInSoundService>().FromInstance(builtInSoundService).AsSingle();
            }
#endif

            if (!hasSounds)
            {
#if FMOD
                Container.BindInterfacesTo<DummyFMODSoundService>().AsSingle();
#else
                Container.Bind<ISoundService>().To<DummySoundService>().AsSingle();
#endif

                Container.Bind<ISoundPlayer>().To<DummySoundPlayer>().AsSingle();
            }
        }

#else
        public override void Resolve()
        {
            soundLevelInitializer.Construct(
                SoundService,
                SoundPlayer,
                soundLevelConfigHolder.SoundLevelConfig
#if !FMOD
                ,
                builtInSoundService
#endif
            );

            soundManager.Construct(SoundPlayer);
        }

        private void InitSound()
        {
            var hasSounds = soundLevelConfigHolder.SoundLevelConfig.HasSounds;

#if FMOD
            if (hasSounds)
            {
                soundService = fmodSoundService;
                soundPlayer = new FMODSoundPlayer(soundService as IFMODSoundService);
            }
#else
                           
            if (hasSounds)
            {
                soundService = builtInSoundService;
                soundPlayer = builtInSoundService;
            }
#endif

            if (!hasSounds)
            {
#if FMOD
                soundService = new DummyFMODSoundService();
#else
                soundService = new DummySoundService();
#endif

                soundPlayer = new DummySoundPlayer();
            }
        }
#endif
    }
}
