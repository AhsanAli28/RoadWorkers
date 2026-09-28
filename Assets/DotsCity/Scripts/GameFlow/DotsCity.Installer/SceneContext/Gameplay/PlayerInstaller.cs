using Spirit604.DotsCity.Gameplay;
using Spirit604.DotsCity.Gameplay.Car;
using Spirit604.DotsCity.Gameplay.Config.Common;
using Spirit604.DotsCity.Gameplay.Factory.Player;
using Spirit604.DotsCity.Gameplay.Initialization;
using Spirit604.DotsCity.Gameplay.Npc.Factory.Player;
using Spirit604.DotsCity.Gameplay.Player;
using Spirit604.DotsCity.Gameplay.Player.Session;
using Spirit604.DotsCity.Gameplay.Player.Spawn;
using Spirit604.DotsCity.Gameplay.UI;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.Gameplay.Factory;
using Spirit604.Gameplay.Factory.Npc;
using Spirit604.Gameplay.Factory.Player;
using Spirit604.Gameplay.Player;
using Spirit604.Gameplay.UI;
using UnityEngine;

#if ZENJECT
using Zenject;
#else
using Spirit604.DotsCity.Gameplay.Bootstrap;
#endif

namespace Spirit604.DotsCity.Installer
{
    public class PlayerInstaller :
#if ZENJECT
        MonoInstaller
#else
        ManualReferenceInstaller
#endif
    {
        [Header("Refs")]

        [Tooltip("IPlayerSpawnerService service")]
        [ResolveLabel][SerializeField] private GameObject playerSpawnerService;

        [Tooltip("IPlayerInteractCarService service")]
        [ResolveLabel][SerializeField] private GameObject playerInteractCarService;

        [Tooltip("Custom user IMonoCarConverter service, otherwise ArcadeMonoCarConverter will be used")]
        [ResolveLabel][SerializeField] private GameObject monoCarConverterService;

        [SerializeField] private PlayerActorTracker playerTargetHandler;

        [ResolveLabel][SerializeField] private PlayerCarSpawner playerCarSpawner;

        [SerializeField] private PlayerSession playerSession;

#if ZENJECT

        [EditorResolve][SerializeField] private CitySettingsInitializer citySettingsInitializer;

#endif

        [Header("Factories")]

        [SerializeField] private PlayerNpcFactory playerNpcFactory;
        [SerializeField] private PlayerNpcInCarFactory playerNpcInCarFactory;
        [SerializeField] private PlayerMobNpcFactory playerMobNpcFactory;
        [SerializeField] private PlayerMonoNpcFactory playerMonoNpcFactory;
        [SerializeField] private PlayerHybridMonoNpcFactory playerHybridMonoNpcFactory;
        [SerializeField] private PlayerCarPool playerCarPool;
        [SerializeField] private FreeFlyCameraFactory freeFlyCameraFactory;

        [Header("UI")]

        [SerializeField] private PlayerInteractTriggerPresenter playerInteractTriggerPresenter;
        [SerializeField] private PlayerEnterCarStatePresenter playerEnterCarStatePresenter;

#if !ZENJECT

        [Header("Resolve")]
        [EditorResolve][SerializeField] private PlayerInitializer playerInitializer;
        [ResolveLabel(false, true)][EditorResolve(true)][SerializeField] private SceneBootstrap sceneBootstrap;
        [EditorResolve][SerializeField] private PlayerSessionListener playerSessionListener;
        [EditorResolve][SerializeField] private PlayerWeaponPresenter playerWeaponPresenter;

        [Header("Additional Refs")]

        [EditorResolve][SerializeField] private GameplayCityInstaller gameplayInstaller;
        [EditorResolve][SerializeField] private GameplayUIInstaller uiGameplayInstaller;
        [EditorResolve][SerializeField] private WeaponFactory weaponFactory;
        [EditorResolve][SerializeField] private InputManager inputManager;

#endif

        private IPlayerNpcFactory PlayerNpcFactory
        {
            get
            {
                if (Settings.CurrentPlayerControllerType == GeneralSettingData.PlayerControllerType.BuiltInCustom)
                    return playerHybridMonoNpcFactory;

                return Settings.DOTSSimulation ? playerNpcFactory : playerMonoNpcFactory;
            }
        }

        private IPlayerMobNpcFactory PlayerMobNpcFactory => playerMobNpcFactory;

#if !ZENJECT
        public PlayerActorTracker PlayerActorTracker => playerTargetHandler;
        private GeneralSettingData Settings => gameplayInstaller.Settings;
#endif

#if ZENJECT

        private GeneralSettingData Settings => citySettingsInitializer.Settings;

        public override void InstallBindings()
        {
            Container.Bind<IPlayerSpawnerService>().FromInstance(playerSpawnerService.GetComponent<IPlayerSpawnerService>()).AsSingle();
            Container.Bind<IPlayerInteractCarService>().FromInstance(playerInteractCarService.GetComponent<IPlayerInteractCarService>()).AsSingle();
            Container.Bind<PlayerActorTracker>().FromInstance(playerTargetHandler).AsSingle();
            Container.Bind<PlayerCarSpawner>().FromInstance(playerCarSpawner).AsSingle();
            Container.Bind<PlayerSession>().FromInstance(playerSession).AsSingle();
            Container.Bind<PlayerInteractTriggerPresenter>().FromInstance(playerInteractTriggerPresenter).AsSingle();
            Container.Bind<IPlayerEntityTriggerProccesor>().To<PlayerEntityTriggerProccesor>().AsSingle();
            Container.Bind<PlayerEnterCarStatePresenter>().FromInstance(playerEnterCarStatePresenter).AsSingle();

            BindFactories();

            if (Settings.BuiltInInteraction)
            {
                if (Settings.BuiltInSolutionOnly)
                {
                    if (Settings.DOTSSimulation)
                    {
                        Container.Bind<INpcInteractCarService>().To<PlayerNpcInteractCarService>().AsSingle();
                    }
                    else
                    {
                        if (Settings.ImmediateEnterCar)
                        {
                            Container.Bind<INpcInteractCarService>().To<PlayerNpcMonoInteractCarServiceBase>().AsSingle();
                        }
                        else
                        {
                            Container.Bind<INpcInteractCarService>().To<PlayerNpcMonoAnimationInteractCarServiceBase>().AsSingle();
                        }

                        if (monoCarConverterService == null || !monoCarConverterService.TryGetComponent<IMonoCarConverter>(out var customMonoCarConverter))
                            Container.Bind<IMonoCarConverter>().To<ArcadeMonoCarConverter>().AsSingle();
                        else
                            Container.Bind<IMonoCarConverter>().FromInstance(customMonoCarConverter).AsSingle();

                        Container.Bind<IPlayerMonoInteractListener>().To<PlayerMonoInteractListener>().AsSingle();
                    }
                }
                else
                {
                    Container.Bind<INpcInteractCarService>().To<PlayerCustomNpcExitCarService>().AsSingle();
                }
            }
            else
            {
                Container.Bind<INpcInteractCarService>().To<EmptyInteractCarService>().AsSingle();
            }

            var generalSettings = Settings;

            bool hasSpawner = false;

            switch (generalSettings.CameraViewType)
            {
                case PlayerAgentType.Player:
                    {
                        switch (generalSettings.CurrentPlayerControllerType)
                        {
                            case GeneralSettingData.PlayerControllerType.BuiltIn:
                                {
                                    hasSpawner = true;

                                    if (generalSettings.DOTSSimulation)
                                    {
                                        Container.Bind<IPlayerEntitySpawner>().To<DefaultPlayerCitySpawner>().AsSingle();
                                    }
                                    else
                                    {
                                        Container.Bind<IPlayerEntitySpawner>().To<DefaultMonoPlayerCitySpawner>().AsSingle();
                                    }
                                }
                                break;
                            case GeneralSettingData.PlayerControllerType.BuiltInCustom:
                                Container.Bind<IPlayerEntitySpawner>().To<HybridMonoPlayerSpawner>().AsSingle();
                                hasSpawner = true;
                                break;
                        }

                        break;
                    }
                case PlayerAgentType.FreeFlyCamera:
                    {
                        hasSpawner = true;
                        Container.Bind<IPlayerEntitySpawner>().To<FreeFlyCameraCitySpawner>().AsSingle();
                        break;
                    }
            }

            if (!hasSpawner)
                Container.Bind<IPlayerEntitySpawner>().To<EmptySpawner>().AsSingle();

            Container.BindInterfacesAndSelfTo<CarConvertService>().AsSingle();
        }

        private void BindFactories()
        {
            Container.Bind<IPlayerNpcFactory>().FromInstance(PlayerNpcFactory).AsSingle();
            Container.Bind<INpcFactory>().FromInstance(PlayerMobNpcFactory).AsSingle();
            Container.Bind<IPlayerMobNpcFactory>().FromInstance(PlayerMobNpcFactory).AsSingle();
            Container.Bind<INpcInCarFactory>().FromInstance(playerNpcInCarFactory).AsSingle();
            Container.Bind<PlayerCarPool>().FromInstance(playerCarPool).AsSingle();
            Container.Bind<FreeFlyCameraFactory>().FromInstance(freeFlyCameraFactory).AsSingle();
        }

#else

        public override void Resolve()
        {
            var generalSettings = gameplayInstaller.Settings;

            IPlayerEntityTriggerProccesor playerEntityTriggerProccesor = new PlayerEntityTriggerProccesor(playerInteractTriggerPresenter, playerSession, gameplayInstaller.SceneService);

            IPlayerEntitySpawner spawner = null;

            bool hasSpawner = false;

            switch (generalSettings.CameraViewType)
            {
                case PlayerAgentType.Player:
                    {
                        switch (generalSettings.CurrentPlayerControllerType)
                        {
                            case GeneralSettingData.PlayerControllerType.BuiltIn:
                                spawner = new DefaultPlayerCitySpawner(playerSession, playerCarSpawner, playerEntityTriggerProccesor, PlayerNpcFactory, PlayerMobNpcFactory);
                                hasSpawner = true;
                                break;
                            case GeneralSettingData.PlayerControllerType.BuiltInCustom:
                                spawner = new HybridMonoPlayerSpawner(playerHybridMonoNpcFactory, playerCarSpawner);
                                hasSpawner = true;
                                break;
                        }

                        break;
                    }
                case PlayerAgentType.FreeFlyCamera:
                    {
                        hasSpawner = true;
                        spawner = new FreeFlyCameraCitySpawner(freeFlyCameraFactory, uiGameplayInstaller.Input, uiGameplayInstaller.MainCamera, inputManager);
                        break;
                    }
            }

            if (!hasSpawner)
                spawner = new EmptySpawner();

            IPlayerSpawnerService spawnerService = null;

            var playerCitySpawnController = playerSpawnerService.GetComponent<PlayerCitySpawnController>();

            if (playerCitySpawnController)
            {
                spawnerService = playerCitySpawnController;
                playerCitySpawnController.Construct(spawner, playerSession, weaponFactory);
            }
            else
            {
                spawnerService = playerSpawnerService.GetComponent<IPlayerSpawnerService>();
            }

            if (sceneBootstrap)
                sceneBootstrap.Construct(spawnerService, uiGameplayInstaller.CameraController);

            INpcInteractCarService interactCarService = null;
            IMonoCarConverter monoCarConverter = null;
            IPlayerMonoInteractListener playerMonoInteractListener = null;
            IPlayerInteractCarService playerInteractService = playerInteractCarService.GetComponent<IPlayerInteractCarService>();

            if (Settings.BuiltInInteraction)
            {
                if (Settings.BuiltInSolutionOnly)
                {
                    if (Settings.DOTSSimulation)
                    {
                        interactCarService = new PlayerNpcInteractCarService(playerTargetHandler, playerSession, PlayerNpcFactory, playerNpcInCarFactory, playerMobNpcFactory);
                    }
                    else
                    {
                        if (Settings.ImmediateEnterCar)
                        {
                            interactCarService = new PlayerNpcMonoInteractCarServiceBase(playerTargetHandler);
                        }
                        else
                        {
                            interactCarService = new PlayerNpcMonoAnimationInteractCarServiceBase(playerTargetHandler);
                        }

                        if (monoCarConverterService == null || !monoCarConverterService.TryGetComponent<IMonoCarConverter>(out var customMonoCarConverter))
                            monoCarConverter = new ArcadeMonoCarConverter(playerCarPool, playerCarSpawner, uiGameplayInstaller.PlayerShootTargetProvider, interactCarService);
                        else
                            monoCarConverter = customMonoCarConverter;

                        playerMonoInteractListener = new PlayerMonoInteractListener(playerEnterCarStatePresenter, playerTargetHandler, playerInteractService, monoCarConverter);
                    }
                }
                else
                {
                    interactCarService = new PlayerCustomNpcExitCarService(playerNpcInCarFactory, PlayerNpcFactory);
                }
            }
            else
            {
                interactCarService = new EmptyInteractCarService();
            }

            ICarConverter carConverter = new CarConvertService(playerCarSpawner);

            playerInitializer.Construct(
                generalSettings,
                playerInteractService,
                carConverter,
                playerEntityTriggerProccesor,
                playerTargetHandler,
                playerCarPool,
                playerEnterCarStatePresenter,
                uiGameplayInstaller.Input,
                uiGameplayInstaller.CarInput,
                uiGameplayInstaller.PlayerShootTargetProvider,
                playerSession,
                playerMonoInteractListener);

            playerCarSpawner.Construct(playerCarPool, uiGameplayInstaller.PlayerShootTargetProvider, interactCarService);

            playerSessionListener.Construct(playerSession);
            playerWeaponPresenter.Construct(playerTargetHandler, playerSession);
        }
#endif

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (playerInteractTriggerPresenter == null)
            {
                playerInteractTriggerPresenter = Extensions.ObjectUtils.FindObjectOfType<PlayerInteractTriggerPresenter>();
                Extensions.EditorSaver.SetObjectDirty(this);
            }
        }
#endif
    }
}