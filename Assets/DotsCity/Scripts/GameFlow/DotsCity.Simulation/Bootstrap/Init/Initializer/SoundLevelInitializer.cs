using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Core.Bootstrap;
using Spirit604.DotsCity.Core.Sound;
using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.DotsCity.Simulation.Sound;
using Spirit604.DotsCity.Simulation.Sound.Pedestrian;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Initialization
{
    public class SoundLevelInitializer : InitializerBase, IPreInitializer, ILateInitializer
    {
#if !FMOD
        private BuiltInSoundService builtInSoundService;
#endif

        private ISoundService soundService;
        private ISoundPlayer soundPlayer;
        private SoundLevelConfig soundLevelConfig;

        [InjectWrapper]
        public void Construct(
            ISoundService soundService,
            ISoundPlayer soundPlayer,
            SoundLevelConfig soundLevelConfig
#if !FMOD   
            ,
            BuiltInSoundService builtInSoundService = null
#endif
            )
        {
#if !FMOD
            this.builtInSoundService = builtInSoundService;
#endif

            this.soundService = soundService;
            this.soundPlayer = soundPlayer;
            this.soundLevelConfig = soundLevelConfig;
        }

        public override void Initialize()
        {
            base.Initialize();

            if (soundLevelConfig.HasSounds)
            {
                InitSound();
                InitSoundSettings();
                CreateAudioListener();
            }
            else
            {
                DisableSoundSystems();
            }
        }

        private void InitSound()
        {
            soundService.Initialize();
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<SoundEventPlaybackSystem>().Initialize(soundPlayer);

#if FMOD
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<FMODPoolSoundSystem, DestroyGroup>();
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<FMODSoundSyncPositionSystem, PreEarlyJobGroup>();
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<FMODInitSoundSystem, StructuralInitGroup>();
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<FMODSoundDelaySystem, MainThreadEventGroup>();
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<FMODCleanupSoundSystem, CleanupGroup>();
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<FMODVolumeSystem, LateSimulationGroup>();
            DefaultWorldUtils.CreateAndAddSystemUnmanaged<FMODTrackTargetEntityPositionSystem, LateSimulationGroup>();

            DefaultWorldUtils.CreateAndAddSystemManaged<FMODCleanupOnDestroySoundSystem, DestroyGroup>();
            DefaultWorldUtils.CreateAndAddSystemManaged<FMODUpdateFloatParameterSystem, MainThreadInitGroup>();

            World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<FMODSoundDataProviderSystem>().Initialize(soundService);
#else
            if (builtInSoundService)
                World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<UnitySoundInitSystem>().Initialize(builtInSoundService);

            DefaultWorldUtils.CreateAndAddSystemManaged<UnitySoundEventPlaybackSystem, MainThreadEventPlaybackGroup>();
            DefaultWorldUtils.CreateAndAddSystemManaged<UnitySoundDelaySystem, StructuralSystemGroup>();
            DefaultWorldUtils.CreateAndAddSystemManaged<UnitySoundEntityVolumeSystem, MainThreadInitGroup>();
            DefaultWorldUtils.CreateAndAddSystemManaged<UnitySoundSyncPositionSystem, MainThreadInitGroup>();
            DefaultWorldUtils.CreateAndAddSystemManaged<UnitySoundTrackTargetEntitySystem, MonoSyncGroup>();
#endif
        }

        private void InitSoundSettings()
        {
            if (soundLevelConfig.CrowdSound)
            {
                DefaultWorldUtils.CreateAndAddSystemUnmanaged<CrowdSoundSystem, BeginSimulationGroup>();
            }
        }

        private void DisableSoundSystems()
        {
            DefaultWorldUtils.SwitchActiveUnmanagedSystem<SoundLoopSystem>(false);
        }

        private void CreateAudioListener()
        {
            if (!soundLevelConfig.CustomAudioListener)
                return;

            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            var entity = entityManager.CreateEntity(
                typeof(CopyTransformToGameObject),
                typeof(PlayerTrackerTag),
                typeof(LocalTransform),
                typeof(LocalToWorld),
                typeof(Transform));

            var audioListener = new GameObject("AudioListener");

#if FMOD
            audioListener.AddComponent<FMODUnity.StudioListener>();
#else
            audioListener.AddComponent<AudioListener>();
#endif

            entityManager.AddComponentObject(entity, audioListener.transform);
        }

        public void PreInitialize()
        {
            MuteSound();
        }

        public void LateInitialize()
        {
            EnableSound();
        }

        private void MuteSound()
        {
            soundService.Mute();
        }

        private void EnableSound()
        {
            if (soundLevelConfig.HasSounds)
            {
                soundService.Unmute();
            }
        }
    }
}