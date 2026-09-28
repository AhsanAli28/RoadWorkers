using Spirit604.DotsCity.Gameplay.CameraService;
using Spirit604.DotsCity.Gameplay.Initialization;
using Spirit604.DotsCity.Gameplay.Player;
using Spirit604.DotsCity.Gameplay.UI;
using Spirit604.Gameplay.InputService;
using Spirit604.Gameplay.Npc;
using Spirit604.Gameplay.Player;
using Spirit604.Gameplay.UI;
using UnityEngine;

#if ZENJECT
using Zenject;
#endif

namespace Spirit604.DotsCity.Installer
{
    public class GameplayUIInstaller :
#if ZENJECT
        MonoInstaller
#else
        ManualReferenceInstaller
#endif
    {
        [SerializeField] private GameObject mainCameraBase;

        [SerializeField] private CitySettingsInitializer citySettingsInitializer;

        [SerializeField] private UIManagerCity uiManagerCity;

        [SerializeField] private InputManager joystickManager;

        [SerializeField] private InputSettingsProvider inputSettingsProvider;

        [SerializeField] private PlayerTargetProvider playerTargetProvider;

        [ResolveLabel][SerializeField] private KeyboardInputManager keyboardInputManager;

#if !ZENJECT

        [Header("Resolve")]
        [EditorResolve][SerializeField] private UIInitializer uiInitializer;
        [EditorResolve][SerializeField] private PcGameplayInputManager pcGameplayInputManager;
        [EditorResolve][SerializeField] private PlayerTargetSettingsRuntimeAuthoring playerTargetSettingsRuntimeAuthoring;
        [EditorResolve][SerializeField] private ResetManager resetManager;

        [Header("Additional Refs")]
        [EditorResolve][SerializeField] private GameplayCityInstaller gameplayCityInstaller;
        [EditorResolve][SerializeField] private PlayerActorTracker playerActorTracker;

        private IMotionInput motionInput;
        private ICarMotionInput carInput;
        private IShootTargetProvider playerShootTargetProvider;
        private bool cameraReported;

        public IMotionInput Input
        {
            get
            {
                if (motionInput == null)
                {
                    bool set = false;
                    bool isMobile = inputSettingsProvider.IsMobile();

                    if (citySettingsInitializer.Settings.PlayerSelected)
                    {
                        if (citySettingsInitializer.Settings.BuiltInSolutionOnly)
                        {
                            if (isMobile)
                            {
                                if (joystickManager && MainCamera)
                                {
                                    motionInput = new TopDownMobileMotionInput(joystickManager, MainCamera);
                                    set = true;
                                }
                            }
                            else
                            {
                                if (MainCamera)
                                {
                                    motionInput = new TopDownPCMotionInput(MainCamera);
                                    set = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (isMobile)
                        {
                            if (joystickManager)
                            {
                                motionInput = new MobileMotionInput(joystickManager);
                                set = true;
                            }
                        }
                        else
                        {
                            if (MainCamera)
                            {
                                motionInput = new PcMotionInput(MainCamera);
                                set = true;
                            }
                        }
                    }

                    if (!set)
                    {
                        motionInput = new EmptyInput();
                    }
                }

                return motionInput;
            }
        }

        public ICarMotionInput CarInput
        {
            get
            {
                if (carInput == null)
                {
                    bool set = false;
                    bool isMobile = inputSettingsProvider.IsMobile();

                    if (isMobile)
                    {
                        if (MainCamera && joystickManager)
                        {
                            carInput = new TopDownMobileCarMotionInput(joystickManager, MainCamera);
                            set = true;
                        }
                    }
                    else
                    {
                        if (MainCamera)
                        {
                            carInput = new TopDownPCCarMotionInput(MainCamera);
                            set = true;
                        }
                    }

                    if (!set)
                    {
                        carInput = new EmptyCarInput();
                    }
                }

                return carInput;
            }
        }

        public IShootTargetProvider PlayerShootTargetProvider
        {
            get
            {
                if (playerShootTargetProvider == null)
                {
                    var setTargetProvider = false;

                    if (MainCamera && citySettingsInitializer.Settings.CameraViewType == PlayerAgentType.Player)
                    {
                        var shootDirectionSource = citySettingsInitializer.Settings.GetShotDirection(inputSettingsProvider.GetInputSettings());

                        playerShootTargetProvider = playerTargetProvider.Create(Input, MainCamera, shootDirectionSource);
                        setTargetProvider = true;
                    }

                    if (!setTargetProvider)
                    {
                        playerShootTargetProvider = new EmptyTargetProvider();
                    }
                }

                return playerShootTargetProvider;
            }
        }

        public Camera MainCamera
        {
            get
            {
                if (mainCameraBase == null)
                {
                    if (!cameraReported)
                    {
                        Debug.Log("UIInstaller. Main camera is not set. Set the main camera object that contains the 'Camera' component in the 'mainCameraBase' field in the UIInstaller.");
                        cameraReported = true;
                    }

                    return Camera.main;
                }

                return mainCameraBase.GetComponent<Camera>();
            }
        }

        public CameraController CameraController => mainCameraBase.GetComponent<CameraController>();

#endif

#if UNITY_EDITOR
        public GameObject MainCameraBase
        {
            get => mainCameraBase;
            set
            {
                mainCameraBase = value;
                Extensions.EditorSaver.SetObjectDirty(this);
            }
        }
#endif

#if ZENJECT
        public override void InstallBindings()
        {
            Container.Bind<UIManagerCity>().FromInstance(uiManagerCity).AsSingle();
            Container.Bind<InputManager>().FromInstance(joystickManager).AsSingle();

            if (mainCameraBase == null)
            {
                Debug.LogError($"GameplayUIInstaller. Camera not assigned.");
            }

            if (mainCameraBase)
            {
                var camController = this.mainCameraBase.GetComponent<CameraController>();

                if (camController)
                {
                    Container.Bind<CameraController>().FromInstance(camController).AsSingle();
                }
            }

            bool setTargetProvider = false;
            bool setInput = false;

            bool isMobile = inputSettingsProvider.IsMobile();

            if (isMobile)
            {
                Container.Bind<IKeyboardInputManager>().To<EmptyInputManager>().AsSingle();
            }
            else
            {
                Container.Bind<IKeyboardInputManager>().FromInstance(keyboardInputManager).AsSingle();
                joystickManager.SwitchEnabledState(false);
            }

            Camera mainCamera = null;

            if (this.mainCameraBase)
                mainCamera = this.mainCameraBase.GetComponent<Camera>();

#if UNITY_EDITOR

            if (!mainCamera)
            {
                mainCamera = Camera.main;
                CameraController controller = null;

                if (mainCamera != null)
                {
                    controller = mainCamera.GetComponent<CameraController>();
                }

                if (controller)
                {
                    Debug.Log("UIInstaller. Main camera is not set. Set the main camera object that contains the 'Camera' component in the 'mainCameraBase' field in the UIInstaller.");
                }
            }
#endif

            if (mainCamera)
            {
                Container.Bind<Camera>().FromInstance(mainCamera).AsSingle();

                IMotionInput motionInput = null;
                ICarMotionInput carMotionInput = null;

                if (citySettingsInitializer.Settings.PlayerSelected)
                {
                    if (isMobile)
                    {
                        motionInput = new TopDownMobileMotionInput(joystickManager, mainCamera);
                        carMotionInput = new TopDownMobileCarMotionInput(joystickManager, mainCamera);
                    }
                    else
                    {
                        motionInput = new TopDownPCMotionInput(mainCamera);
                        carMotionInput = new TopDownPCCarMotionInput(mainCamera);
                    }
                }
                else
                {
                    if (isMobile)
                    {
                        motionInput = new MobileMotionInput(joystickManager);
                    }
                    else
                    {
                        motionInput = new PcMotionInput(mainCamera);
                    }

                    carMotionInput = new EmptyCarInput();
                }

                setInput = true;
                Container.Bind<IMotionInput>().FromInstance(motionInput).AsSingle();
                Container.Bind<ICarMotionInput>().FromInstance(carMotionInput).AsSingle();

                if (citySettingsInitializer.Settings.CameraViewType == PlayerAgentType.Player)
                {
                    var shootDirectionSource = citySettingsInitializer.Settings.GetShotDirection(inputSettingsProvider.GetInputSettings());

                    Container.Bind<IShootTargetProvider>().FromMethod(() => playerTargetProvider.Create(motionInput, mainCamera, shootDirectionSource)).AsSingle();
                    setTargetProvider = true;
                }
            }

            if (!setTargetProvider)
            {
                Container.Bind<IShootTargetProvider>().To<EmptyTargetProvider>().AsSingle();
            }

            if (!setInput)
            {
                Container.Bind<IMotionInput>().To<EmptyInput>().AsSingle();
                Container.Bind<ICarMotionInput>().To<EmptyCarInput>().AsSingle();
            }

            Container.Bind<IInputSettings>().FromMethod(() => inputSettingsProvider.GetInputSettings()).AsSingle();
        }

#else

        public override void Resolve()
        {
            uiInitializer.Construct(gameplayCityInstaller.Settings);

            IInputSettings inputSettings = inputSettingsProvider.GetInputSettings();
            bool isMobile = inputSettingsProvider.IsMobile();

            IKeyboardInputManager keyboardInputManager = null;

            if (isMobile)
            {
                keyboardInputManager = new EmptyInputManager();
            }
            else
            {
                this.keyboardInputManager.Construct(inputSettings);
                keyboardInputManager = this.keyboardInputManager;
                joystickManager.SwitchEnabledState(false);
            }

            pcGameplayInputManager.Construct(keyboardInputManager);

            playerTargetSettingsRuntimeAuthoring.Construct(inputSettings);

            var cam = CameraController;

            if (cam != null)
            {
                cam.Construct(gameplayCityInstaller.SceneService, playerActorTracker);
            }

            resetManager.Construct(gameplayCityInstaller.SceneService);
        }

#endif
    }
}