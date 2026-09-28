using Spirit604.Attributes;
using Spirit604.CityEditor;
using Spirit604.DotsCity.Core;
using System;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic.Authoring
{
    [CreateAssetMenu(fileName = "TrafficSettingsConfig", menuName = CityEditorBookmarks.CITY_EDITOR_LEVEL_CONFIG_LEVEL_PATH + "Traffic/TrafficSettingsConfig")]
    public class TrafficCarSettingsConfig : ScriptableObjectBase
    {
        //[ValidateInput(nameof(EntityTypeVariableValidation), "Traffic Cars can't detect obstacle - change 'cullphysics' parameter")]
        [Tooltip("" +
            "<b>HybridEntitySimplePhysics</b> : hybrid entities moved by the simple physical system\r\n\r\n" +
            "<b>HybridEntityCustomPhysics</b> : hybrid entities moved by the custom physical system\r\n\r\n" +
            "<b>HybridEntityMonoPhysics</b> : hybrid entities moved by the custom monobehaviour controller\r\n\r\n" +
            "<b>PureEntityCustomPhysics</b> : pure entities moved by the simple physical system\r\n\r\n" +
            "<b>PureEntitySimplePhysics</b> : pure entities moved by the simple physical system\r\n\r\n" +
            "<b>PureEntityNoPhysicsPure</b> : entities that moved by transform system without physics")]

        [ShowIf(nameof(DOTSSimulation))]
        [SerializeField] private EntityType entityType = EntityType.PureEntitySimplePhysics;

        [Label("Entity Type")]
        [HideIf(nameof(DOTSSimulation))]
        [EnableIf(nameof(DOTSSimulation))]
        [SerializeField] private EntityType monoEntityType = EntityType.HybridEntityMonoPhysics;

        [Tooltip("" +
            "<b>Hybrid</b> : combine types Calculate and Raycast. Raycast enabled only if the close target has 'TrafficCustomRaycastTargetTag' tag\r\n\r\n" +
            "<b>CalculateOnly</b> : mathematically calculates the obstacle\r\n\r\n" +
            "<b>RaycastOnly</b> : detect obstacle by raycast only")]
        [SerializeField] private DetectObstacleMode detectObstacleMode = DetectObstacleMode.Hybrid;

        [Tooltip("" +
            "<b>Calculate</b> : mathematically calculates the npc\r\n\r\n" +
            "<b>Raycast</b> : detect obstacle by raycast (npc should have PhysicsShape component)")]
        [SerializeField] private DetectNpcMode detectNpcMode = DetectNpcMode.Calculate;

        [HideIf(nameof(CustomPhysicsEntity))]
        [Tooltip("" +
            "<b>Car input</b> : simple emulation of real movement based on traffic input\r\n\r\n" +
            "<b>Follow target</b> : the vehicle rotation is set based on the destination direction")]
        [SerializeField] private SimplePhysicsSimulationType simplePhysicsType;

        [Label("Default Lane Speed Km/h")]
        [Tooltip("Default lane speed (if the lane speed limit is set to 0 the default speed will be selected)")]
        [SerializeField][Range(0, 300)] private float defaultLaneSpeed = ProjectConstants.DefaultLaneSpeed;

        [OnValueChanged(nameof(SettingsUpdated))]
        [Label("Max Car Speed Km/h")]
        [Tooltip("Maximum speed of the car")]
        [SerializeField][Range(0, 130)] private float maxCarSpeed = 60f;

        [OnValueChanged(nameof(SettingsUpdated))]
        [HideIf(nameof(CustomPhysicsEntity))]
        [Tooltip("Vehicle acceleration speed")]
        [SerializeField][Range(0, 40)] private float acceleration = 5f;

        [OnValueChanged(nameof(SettingsUpdated))]
        [HideIf(nameof(CustomPhysicsEntity))]
        [Tooltip("Backward vehicle acceleration speed")]
        [SerializeField][Range(0, 40)] private float backwardAcceleration = 2.5f;

        [OnValueChanged(nameof(SettingsUpdated))]
        [HideIf(nameof(CustomPhysicsEntity))]
        [Tooltip("Brake power")]
        [SerializeField][Range(0, 200)] private float brakePower = 80f;

        [OnValueChanged(nameof(SettingsUpdated))]
        [ShowIf(nameof(CustomPhysicsEntity))]
        [Tooltip("Braking rate if current vehicle speed limit is greater than speed limit")]
        [SerializeField][Range(0, 0.9f)] private float brakingInputRate = 0.9f;

        [OnValueChanged(nameof(SettingsUpdated))]
        [HideIf(nameof(CustomPhysicsEntity))]
        [Tooltip("Max steer angle of the wheels (simple dots cars only)")]
        [SerializeField][Range(0, 90)] private float maxSteerAngle = 35f;

        [OnValueChanged(nameof(SettingsUpdated))]
        [Tooltip("Angle between the front of the vehicle & the target of the vehicle when the steering input of the wheels is max")]
        [SerializeField][Range(0, 40)] private float maxSteerDirectionAngle = 20f;

        [OnValueChanged(nameof(SettingsUpdated))]
        [Tooltip("Maximum speed at which <b>MaxSteerDirectionAngle</b> option is available, if maximum speed steer is 0 <b>MaxSteerDirectionAngle</b> can be used at any speed")]
        [SerializeField][Range(0, 60)] private float maxSpeedSteerDirectionAngle = 40f;

        [OnValueChanged(nameof(SettingsUpdated))]
        [ShowIf(nameof(MonoEntity))]
        [SerializeField] private bool useSteeringDamping;

        [OnValueChanged(nameof(SettingsUpdated))]
        [ShowIf(nameof(ShowDamping))]
        [Tooltip("Wheel turn speed")]
        [SerializeField][Range(0, 5f)] private float steeringDamping = 0.1f;

        [OnValueChanged(nameof(SettingsUpdated))]
        [ShowIf(nameof(CustomPhysicsEntity))]
        [Tooltip("Steering power limited by vehicle speed")]
        [SerializeField] private bool limitSteeringPower = true;

        [OnValueChanged(nameof(SettingsUpdated))]
        [ShowIf(nameof(LimitSteeringPower))]
        [Tooltip("X axis vehicle speed in km/h, Y axis steering power. Final calculated steering: calculated steering * power steering")]
        [SerializeField]
        private AnimationCurve steeringPowerCurve = new AnimationCurve()
        {
            postWrapMode = WrapMode.ClampForever,
            preWrapMode = WrapMode.ClampForever,
            keys = new Keyframe[] {
                new Keyframe()
                {
                    value = 1,
                },
                new Keyframe()
                {
                    value = 1,
                    time = 30,
                },
                new Keyframe()
                {
                    value = 0.6f,
                    time = 50f,
                },
                new Keyframe()
                {
                    value = 0.2f,
                    time = 120f,
                },
            }
        };

        [OnValueChanged(nameof(SettingsUpdated))]
        [Tooltip("Number of hit points of the car (health systems should be enabled)")]
        [SerializeField][Range(0, 1000)] private int healthCount = 12;

        //[ValidateInput(nameof(CullPhysicsVariableValidation), "Traffic Cars can't detect obstacle - change 'cullphysics' parameter")]
        [OnValueChanged(nameof(SettingsUpdated))]
        [HideIf(nameof(NoPhysicsEntity))]
        [Tooltip("Off physics for the vehicle if they are outside the camera")]
        [SerializeField] private bool cullPhysics = true;

        [OnValueChanged(nameof(SettingsUpdated))]
        [ShowIf(nameof(RotationLerpSupport))]
        [SerializeField] private bool hasRotationLerp = true;

        [OnValueChanged(nameof(RotationSettingsUpdated))]
        [ShowIf(nameof(HasRotationLerp))]
        [Tooltip("Vehicle rotation speed")]
        [SerializeField][Range(0, 20)] private float rotationSpeed = 3.2f;

        [OnValueChanged(nameof(RotationSettingsUpdated))]
        [ShowIf(nameof(HasRotationLerp))]
        [Tooltip("Curve on the dependence of the speed of the car on its speed")]
        [SerializeField] private AnimationCurve rotationSpeedCurve;

        [ShowIf(nameof(CullWheelSupported))]
        [Tooltip("On/off wheel handling if they are outside the camera")]
        [SerializeField] private bool cullWheels = true;

        [Tooltip("On/off navmesh obstacles loading for traffic")]
        [SerializeField] private bool hasNavObstacle;

        [ShowIf(nameof(MonoEntity))]
        [Tooltip("Multithreaded raycasting for Arcade cars")]
        [SerializeField] private bool jobSystem = true;

        public bool CustomPhysicsEntity => EntityType == EntityType.HybridEntityCustomPhysics || EntityType == EntityType.PureEntityCustomPhysics || EntityType == EntityType.HybridEntityMonoPhysics;
        public bool SimplePhysicsEntity => EntityType == EntityType.HybridEntitySimplePhysics || EntityType == EntityType.PureEntitySimplePhysics;
        public bool NoPhysicsEntity => EntityType == EntityType.PureEntityNoPhysics;
        public bool HybridEntity => EntityType == EntityType.HybridEntitySimplePhysics || EntityType == EntityType.HybridEntityCustomPhysics || EntityType == EntityType.HybridEntityMonoPhysics;
        public bool MonoEntity => EntityType == EntityType.HybridEntityMonoPhysics;

        public EntityType EntityType
        {
            get
            {
                if (GeneralSettingData)
                {
                    return GeneralSettingData.DOTSSimulation ? entityType : monoEntityType;
                }

                return entityType;
            }
            set => entityType = value;
        }

        public DetectObstacleMode DetectObstacleMode { get => detectObstacleMode; set => detectObstacleMode = value; }
        public DetectNpcMode DetectNpcMode { get => detectNpcMode; set => detectNpcMode = value; }

        public PhysicsSimulationType PhysicsSimulation
        {
            get
            {
                if (EntityType == EntityType.HybridEntityCustomPhysics || EntityType == EntityType.PureEntityCustomPhysics)
                {
                    return PhysicsSimulationType.CustomDots;
                }

                if (EntityType == EntityType.HybridEntityMonoPhysics)
                {
                    return PhysicsSimulationType.HybridMono;
                }

                return PhysicsSimulationType.Simple;
            }
        }

        public SimplePhysicsSimulationType SimplePhysics { get => simplePhysicsType; set => simplePhysicsType = value; }

        public float DefaultLaneSpeed { get => defaultLaneSpeed; set => defaultLaneSpeed = value; }
        public float DefaultLaneSpeedMs => DefaultLaneSpeed / ProjectConstants.KmhToMs_RATE;
        public float MaxCarSpeed { get => maxCarSpeed; set => maxCarSpeed = value; }
        public float Acceleration { get => acceleration; set => acceleration = value; }
        public float BackwardAcceleration { get => backwardAcceleration; set => backwardAcceleration = value; }
        public float BrakePower { get => brakePower; set => brakePower = value; }
        public float BrakingRate { get => brakingInputRate; set => brakingInputRate = value; }
        public float MaxSteerAngle { get => maxSteerAngle; set => maxSteerAngle = value; }
        public float MaxSteerDirectionAngle { get => maxSteerDirectionAngle; set => maxSteerDirectionAngle = value; }
        public float MaxSpeedSteerDirectionAngle { get => maxSpeedSteerDirectionAngle; set => maxSpeedSteerDirectionAngle = value; }
        public bool UseSteeringDamping { get => useSteeringDamping; set => useSteeringDamping = value; }
        public float SteeringDamping { get => steeringDamping; set => steeringDamping = value; }
        public bool LimitSteeringPower { get => limitSteeringPower && CustomPhysicsEntity; set => limitSteeringPower = value; }
        public AnimationCurve SteeringPowerCurve { get => steeringPowerCurve; set => steeringPowerCurve = value; }
        public int HealthCount { get => healthCount; set => healthCount = value; }
        public bool HasRotationLerp { get => hasRotationLerp && RotationLerpSupport; set => hasRotationLerp = value; }
        public float RotationSpeed { get => rotationSpeed; set => rotationSpeed = value; }
        public AnimationCurve RotationSpeedCurve { get => rotationSpeedCurve; set => rotationSpeedCurve = value; }
        public bool CullPhysics { get => cullPhysics; set => cullPhysics = value; }
        public bool CullWheels { get => cullWheels; set => cullWheels = value; }
        public bool HasNavObstacle { get => hasNavObstacle; set => hasNavObstacle = value; }
        public bool JobSystem { get => jobSystem; set => jobSystem = value; }
        public bool CullWheelSupported => !CustomPhysicsEntity;

        public bool RotationLerpSupport => !CustomPhysicsEntity || CustomPhysicsEntity && cullPhysics;

        public GeneralSettingDataCore GeneralSettingData { get; set; }

        private bool DOTSSimulation => GeneralSettingData ? GeneralSettingData.DOTSSimulation : true;

        private bool ShowDamping => MonoEntity && useSteeringDamping || !MonoEntity && !CustomPhysicsEntity;

        public event Action OnTrafficSettingsChanged = delegate { };
        public event Action OnRotationSettingsChanged = delegate { };

        private void SettingsUpdated()
        {
            OnTrafficSettingsChanged();
        }

        private void RotationSettingsUpdated()
        {
            OnRotationSettingsChanged();
        }

        private bool CullPhysicsVariableValidation(bool input)
        {
            if (input && detectObstacleMode == DetectObstacleMode.RaycastOnly)
            {
                return false;
            }

            return true;
        }

        private bool EntityTypeVariableValidation(EntityType trafficEntityType)
        {
            if (trafficEntityType == EntityType.PureEntityNoPhysics && detectObstacleMode == DetectObstacleMode.RaycastOnly)
            {
                return false;
            }

            return true;
        }
    }
}