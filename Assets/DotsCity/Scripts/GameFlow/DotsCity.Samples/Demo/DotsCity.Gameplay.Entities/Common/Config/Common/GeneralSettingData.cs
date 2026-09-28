using Spirit604.Attributes;
using Spirit604.CityEditor;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Gameplay.Player;
using Spirit604.DotsCity.Simulation.Config;
using Spirit604.Extensions;
using Spirit604.Gameplay.InputService;
using Spirit604.Gameplay.Player;
using System;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Config.Common
{
    [CreateAssetMenu(fileName = "GeneralSettingData", menuName = CityEditorBookmarks.CITY_EDITOR_ROOT_PATH + "Game Data/General Setting Data")]
    public class GeneralSettingData : GeneralSettingDataSimulation
    {
        public enum BulletCollisionType { CalculateCollision, Raycast }
        public enum PlayerControllerType { BuiltIn, BuiltInCustom, Custom }
        public enum PlayerVehicleInteractionType { BuiltIn, Custom }

        #region Player variables

        [SerializeField] private PlayerAgentType playerAgentType = PlayerAgentType.Player;

        [HideInView]
        [Tooltip(
            "<b>Built In</b> - the player spawned by the built-in solution & has an example built-in controller\r\n\r\n" +
            "<b>Built In Custom</b> - the player spawned by the built-in solution, but the player NPC has a custom character controller & taken from 'PlayerCustomHybridMonoNpcFactory'\r\n\r\n" +
            "<b>Custom</b> - the player is spawned & handled entirely by the user's custom solution."
            )]
        [ShowIf(nameof(PlayerAvailable))]
        [SerializeField] private PlayerControllerType playerControllerType;

        [HideInView]
        [Tooltip(
            "<b>Built In</b> - the player interacting with cars by the built-in solution\r\n\r\n" +
            "<b>Custom</b> - the player interacting with the cars by the custom user solution"
        )]
        [ShowIf(nameof(BuiltInInteractionAvailable))]
        [SerializeField] private PlayerVehicleInteractionType vehicleInteractionType;

        [ShowIf(nameof(ImmediateEnterAvailable))]
        [SerializeField] private bool immediateEnterCar = true;

        [ShowIf(nameof(PlayerSelected))]
        [SerializeField] private bool bulletSupport = true;

        [ShowIf(nameof(BulletSupport))]
        [Tooltip("Method of calculating collisions for a bullet")]
        [SerializeField] private BulletCollisionType bulletCollisionType;

        [ShowIf(nameof(MobileInputSupport))]
        [SerializeField] private MobileShootDirectionSource shootDirectionSource = MobileShootDirectionSource.CrossHair;

        [ShowIf(nameof(PlayerSelected))]
        [Tooltip("Maximum distance for crosshair target capture")]
        [SerializeField][Range(0, 50f)] private float maxTargetDistance = 12f;

        [ShowIf(nameof(PlayerSelected))]
        [Tooltip("Maximum angle for crosshair target capture")]
        [SerializeField][Range(0, 90f)] private float maxCaptureAngle = 15f;

        [ShowIf(nameof(PlayerSelected))]
        [Tooltip("Distance between the player and the crosshair if there is no target")]
        [SerializeField][Range(0, 20f)] private float defaultAimPointDistance = 4f;

        [ShowIf(nameof(PlayerSelected))]
        [Tooltip("Default Y-axis crosshair position")]
        [SerializeField][Range(0, 1f)] private float defaultAimPointYPosition = 0.2f;

        #endregion

        #region Other variables

        [HideIf(nameof(DOTSSimulation))]
        [SerializeField] private bool chasingCarsSupport;

        [Tooltip("On/off main UI")]
        [SerializeField] private bool hideUI;

        [Tooltip("On/off FPS UI panel")]
        [SerializeField] private bool showFps;

        #endregion

        public PlayerAgentType CameraViewType
        {
            get => playerAgentType;
            set
            {
                if (playerAgentType != value)
                {
                    playerAgentType = value;
                    EditorSaver.SetObjectDirty(this);
                }
            }
        }

        public PlayerSimulationType PlayerSimulationType => DOTSSimulation ? PlayerSimulationType.HybridDOTS : PlayerSimulationType.HybridMono;
        public PlayerControllerType CurrentPlayerControllerType { get => playerControllerType; set => playerControllerType = value; }
        public bool BuiltInSolutionOnly => playerControllerType == PlayerControllerType.BuiltIn;
        public bool BuiltInSolution => playerControllerType != PlayerControllerType.Custom;
        public bool BuiltInInteractionAvailable => PlayerSelected;
        public bool BuiltInInteraction => vehicleInteractionType == PlayerVehicleInteractionType.BuiltIn;
        public bool ImmediateEnterAvailable => BuiltInInteraction && !DOTSSimulation;
        public bool ImmediateEnterCar { get => immediateEnterCar && ImmediateEnterAvailable; set => immediateEnterCar = value; }

        public override bool BulletSupport { get => bulletSupport && PlayerSelected; }
        public BulletCollisionType BulletType { get => bulletCollisionType; }

        public bool HideUI { get => hideUI; set => hideUI = value; }
        public bool ShowFps { get => showFps; set => showFps = value; }

        public bool PlayerAvailable => playerAgentType == PlayerAgentType.Player;
        public bool PlayerSelected => PlayerAvailable;
        public bool MobileInputSupport => BulletSupport && !PcPlatform;

        public bool ChasingCarsSupport { get => chasingCarsSupport && !DOTSSimulation; }

        public ShootDirectionSource GetShotDirection(IInputSettings inputSettings)
        {
            ShootDirectionSource currentShootDirectionSource = ShootDirectionSource.Mouse;

            if (inputSettings.InputMobilePlatform)
            {
                switch (shootDirectionSource)
                {
                    case MobileShootDirectionSource.Joystick:
                        {
                            currentShootDirectionSource = ShootDirectionSource.Joystick;
                            break;
                        }
                    case MobileShootDirectionSource.CrossHair:
                        {
                            currentShootDirectionSource = ShootDirectionSource.CrossHair;
                            break;
                        }
                }
            }

            return currentShootDirectionSource;
        }

        public PlayerTargetSettings GetPlayerTargetSettings(IInputSettings inputSettings) => new PlayerTargetSettings()
        {
            PlayerShootDirectionSource = GetShotDirection(inputSettings),
            MaxTargetDistanceSQ = maxTargetDistance * maxTargetDistance,
            MaxCaptureAngle = maxCaptureAngle,
            DefaultAimPointDistance = defaultAimPointDistance,
            DefaultAimPointYPosition = defaultAimPointYPosition
        };
    }
}