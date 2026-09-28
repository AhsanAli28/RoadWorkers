using Spirit604.DotsCity.Gameplay.Chaser;
using Spirit604.DotsCity.Gameplay.Config.Common;
using Spirit604.DotsCity.Gameplay.Initialization;
using Spirit604.DotsCity.Gameplay.Npc.Factory;
using Spirit604.Gameplay.Factory.Npc;
using Spirit604.Gameplay.Services;
using UnityEngine;

#if ZENJECT
using Zenject;
#else
using Spirit604.DotsCity.Gameplay.CameraService;
using Spirit604.DotsCity.Simulation.Pedestrian.Authoring;
using Spirit604.DotsCity.Simulation.VFX;
#endif

namespace Spirit604.DotsCity.Installer
{
    public class GameplayCityInstaller :
#if ZENJECT
        MonoInstaller
#else
        ManualReferenceInstaller
#endif
    {
        [SerializeField] private CitySettingsInitializer citySettingsInitializer;
        [SerializeField] private SceneService sceneService;
        [SerializeField] private PoliceNpcFactory policeNpcFactory;
        [SerializeField] private PoliceMonoNpcFactory policeMonoNpcFactory;
        [SerializeField] private PoliceNpcInCarFactory policeNpcInCarFactory;

#if ZENJECT
        [EditorResolve][SerializeField] private ChaserController chaserController;
#else
        [Header("Resolve")]
        [EditorResolve][SerializeField] private CommonEntitySystemsInitializer commonEntitySystemsInitializer;
        [EditorResolve][SerializeField] private ChaserController chaserController;
#endif

#if !ZENJECT

        [Header("Additional Refs")]
        [EditorResolve][SerializeField] private GameplayCityInstaller gameplayCityInstaller;
        [EditorResolve][SerializeField] private PlayerInstaller playerInstaller;
        [EditorResolve][SerializeField] private VFXFactory vfxFactory;
        [EditorResolve][SerializeField] private PedestrianSpawnerConfigHolder pedestrianSpawnerConfigHolder;
        [ResolveLabel(false, true)][EditorResolve(true)][SerializeField] private CameraController cameraController;

        public GeneralSettingData Settings => citySettingsInitializer.Settings;
        public ISceneService SceneService => sceneService;

#endif

#if ZENJECT
        public override void InstallBindings()
        {
            BindCommon();
            BindPolice();
        }

        private void BindCommon()
        {
            Container.Bind<CitySettingsInitializer>().FromInstance(citySettingsInitializer).AsSingle();

            var generalSettings = citySettingsInitializer.Settings;
            Container.Bind<GeneralSettingData>().FromInstance(generalSettings).AsSingle();

            Container.Bind<ISceneService>().FromInstance(sceneService).AsSingle();
        }

        private void BindPolice()
        {
            Container.Bind<ChaserController>().FromInstance(chaserController).AsSingle();
            Container.Bind<PoliceNpcFactory>().FromInstance(policeNpcFactory).AsSingle();
            Container.Bind<PoliceMonoNpcFactory>().FromInstance(policeMonoNpcFactory).AsSingle();
            Container.Bind<PoliceNpcInCarFactory>().FromInstance(policeNpcInCarFactory).AsSingle();
        }
#else
        public override void Resolve()
        {
            commonEntitySystemsInitializer.Construct(
                vfxFactory,
                gameplayCityInstaller.Settings,
                pedestrianSpawnerConfigHolder,
                cameraController);

            chaserController.Construct(policeMonoNpcFactory, policeNpcInCarFactory, gameplayCityInstaller.Settings);
        }
#endif

    }
}
