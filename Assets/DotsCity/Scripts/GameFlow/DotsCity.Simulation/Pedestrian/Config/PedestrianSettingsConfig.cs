using Spirit604.Attributes;
using Spirit604.CityEditor;
using Spirit604.DotsCity.Simulation.Npc;
using Spirit604.DotsCity.Simulation.Npc.Navigation;
using System;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [CreateAssetMenu(fileName = "PedestrianSettingsConfig", menuName = CityEditorBookmarks.CITY_EDITOR_LEVEL_CONFIG_LEVEL_PATH + "Pedestrian/PedestrianSettingsConfig")]
    public class PedestrianSettingsConfig : ScriptableObjectBase
    {
        [ShowIf(nameof(CullingAvailable))]
        [SerializeField] private NpcSkinType pedestrianSkinType = NpcSkinType.RigShowOnlyInView;

        [Helpbox("To use this rig type, you should purchase & install this package:",
            nameof(ShowRukhankaPopup), MessageBoxType.Error,
            url: "https://assetstore.unity.com/packages/tools/animation/rukhanka-animation-system-2-298480")]
        [ShowIf(nameof(HasRig))]
        [OnValueChanged(nameof(RigTypeChanged))]
        [Tooltip("" +
            "<b>Hybrid Legacy</b> : Hybrid entity with animator component\r\n\r\n" +
            "<b>Pure GPU</b> : pure entity with GPU animations\r\n\r\n" +
            "<b>Hybrid And GPU</b> : hybrid GPU mode that allows you to mix Hybrid animator models for near and GPU animation for far at the same time\r\n\r\n" +
            "<b>Hybrid On Request And GPU</b> : Hybrid skin will be load if 'PreventHybridSkinTagTag' tag is manually disabled by the user for the specific entity, otherwise it will be animated by GPU\r\n\r\n" +
            "<b>Hybrid Shape GPU</b> : hybrid entity animated on GPU in `DOTS` & has hybrid monobehaviour collider to interact with pedestrians in a familiar way\r\n\r\n" +
            "<b>Rukhanka</b> : pure entities animated with Rukhanka Animation System in `DOTS`\r\n\r\n" +
            "<b>Rukhanka Hybrid</b> : hybrid entities animated with Rukhanka Animation System with hybrid monobehaviour collider & rigidbody to control or interact with pedestrians in a familiar way")]
        [SerializeField] private NpcRigType pedestrianRigType;

        [HideIf(nameof(CustomSkin))]
        [SerializeField] private EntityType pedestrianEntityType;

        [OnValueChanged(nameof(SignalConfigUpdated))]
        [Tooltip("Walking speed")]
        [SerializeField][MinMaxSlider(0, 5f)] private Vector2 walkingSpeed = new Vector2(1.3f, 1.5f);

        [OnValueChanged(nameof(SignalConfigUpdated))]
        [Tooltip("Running speed")]
        [SerializeField][MinMaxSlider(0, 10f)] private Vector2 runningSpeed = new Vector2(2.8f, 3.2f);

        [ShowIf(nameof(lerpRotation))]
        [OnValueChanged(nameof(SignalConfigUpdated))]
        [Tooltip("Rotation speed")]
        [SerializeField][Range(0, 20)] private float rotationSpeed = 4f;

        [OnValueChanged(nameof(SignalConfigUpdated))]
        [Tooltip("Default achieve distance to destination")]
        [SerializeField][Range(0, 2f)] private float achieveDistance = 1f;

        [OnValueChanged(nameof(SignalConfigUpdated))]
        [Tooltip("Collider radius of the pedestrians")]
        [SerializeField][Range(0, 2f)] private float colliderRadius = 0.3f;

        [OnValueChanged(nameof(SignalConfigUpdated))]
        [Tooltip("Number of hit points for pedestrian")]
        [SerializeField][Range(0, 100)] private int health = 1;

        [Tooltip("Chance of spawning talking pedestrians")]
        [SerializeField][Range(0, 1f)] private float talkingPedestrianSpawnChance = 0.04f;

        [MinMaxSlider(0.0f, 500.0f)]
        [Tooltip("Min/max talk time")]
        [SerializeField] private Vector2 minMaxTalkTime = new Vector2(10f, 50f);

        [Helpbox("To use this type of navigation, you should purchase & install this package:",
            nameof(ShowNavAgentsPopup), MessageBoxType.Error,
            url: "https://assetstore.unity.com/packages/tools/behavior-ai/agents-navigation-239233")]
        [Tooltip("ObstacleAvoidanceType LocalAvoidance or NavMesh")]
        [SerializeField] private ObstacleAvoidanceType obstacleAvoidanceType = ObstacleAvoidanceType.LocalAvoidance;

        [ShowIf(nameof(NavMeshNavigationEnabled))]
        [Tooltip("NavigationType Temp, Persist, Disabled")]
        [SerializeField] private NpcNavigationType pedestrianNavigationType = NpcNavigationType.Temp;

        [Tooltip("Collision type calculate of physics")]
        [SerializeField] private CollisionType pedestrianCollisionType;

        [ShowIf(nameof(AgentsNavigation))]
        [Tooltip("Auto add AgentsNavigation components")]
        [SerializeField] private bool autoAddAgentComponents;

        [Tooltip("On/off lerp rotation of the pedestrians")]
        [SerializeField] private bool lerpRotation = true;

        [ShowIf(nameof(lerpRotation))]
        [Tooltip("Lerp rotation in view of camera only")]
        [SerializeField] private bool lerpRotationInView = true;

        [Tooltip("On/off ragdoll for pedestrian after death")]
        [OnValueChanged(nameof(RagdollChanged))]
        [EnableIf(nameof(RagdollAvailable))]
        [SerializeField] private bool hasRagdoll = true;

        [ShowIf(nameof(CustomRagdollAvailable))]
        [Tooltip("" +
            "<b>Default</b> : uses the default embedded Ragdoll system\r\n\r\n" +
            "<b>Custom</b> : uses the custom user's Ragdoll system")]
        [SerializeField] private RagdollType ragdollType;

        [Tooltip("Align pedestrian along Y-axis between nodes. Available for <b>No physics</b> pedestrian only")]
        [ShowIf(nameof(NoPhysics))]
        [SerializeField] private bool alignYAxis;

        [ShowIf(nameof(AlignYAxis))]
        [SerializeField][Range(0.1f, 10)] private float yAxisLerpSpeed = 1f;

        public NpcSkinType PedestrianSkinType
        {
            get
            {
                return CullingAvailable ? pedestrianSkinType : NpcSkinType.RigShowOnlyInView;
            }
        }

        public NpcRigType PedestrianRigType => pedestrianRigType;
        public EntityType PedestrianEntityType { get => pedestrianEntityType; set => pedestrianEntityType = value; }
        public Vector2 WalkingSpeed => walkingSpeed;
        public Vector2 RunningSpeed => runningSpeed;
        public float AchieveDistance => achieveDistance;
        public float ColliderRadius => colliderRadius;
        public int Health => health;
        public float RotationSpeed => rotationSpeed;
        public float TalkingPedestrianSpawnChance => talkingPedestrianSpawnChance;
        public Vector2 MinMaxTalkTime => minMaxTalkTime;
        public NpcNavigationType PedestrianNavigationType => pedestrianNavigationType;
        public ObstacleAvoidanceType ObstacleAvoidanceType { get => obstacleAvoidanceType; set => obstacleAvoidanceType = value; }
        public CollisionType PedestrianCollisionType { get => pedestrianCollisionType; set => pedestrianCollisionType = value; }

#if PROJECTDAWN_NAV
        public bool AutoAddAgentComponents => autoAddAgentComponents;
#else
        public bool AutoAddAgentComponents => false;
#endif

        public bool LerpRotation => lerpRotation;

        public bool LerpRotationInView => lerpRotationInView;

#if RUKHANKA
        public bool RagdollAvailable => pedestrianRigType != NpcRigType.RukhankaHybrid;
#else
        public bool RagdollAvailable => true;
#endif

        public bool HasRagdoll => hasRagdoll && RagdollAvailable;

        public RagdollType RagdollType => CustomRagdollAvailable ? ragdollType : RagdollType.Default;

        public bool HasRig => PedestrianSkinType == NpcSkinType.RigShowOnlyInView || PedestrianSkinType == NpcSkinType.RigShowAlways || CustomSkin;

        public bool GPUHybridSkin => pedestrianRigType == NpcRigType.HybridAndGPU || pedestrianRigType == NpcRigType.HybridOnRequestAndGPU;

        public bool CullingAvailable => !GPUHybridSkin && !CustomSkin;

        public bool CustomSkin => pedestrianRigType == NpcRigType.Rukhanka;

        public bool HybridSkin => pedestrianRigType == NpcRigType.HybridAndGPU || pedestrianRigType == NpcRigType.HybridOnRequestAndGPU || pedestrianRigType == NpcRigType.HybridLegacy;

        public bool CustomRagdollAvailable => hasRagdoll && HybridSkin;

        public bool AlignYAxis { get => NoPhysics ? alignYAxis : false; internal set => alignYAxis = value; }

        public float YAxisLerpSpeed { get => yAxisLerpSpeed; internal set => yAxisLerpSpeed = value; }

        private bool NavMeshNavigationEnabled => obstacleAvoidanceType == ObstacleAvoidanceType.CalcNavPath;

        private bool AgentsNavigation => obstacleAvoidanceType == ObstacleAvoidanceType.AgentsNavigation;

        private bool NoPhysics => pedestrianEntityType == EntityType.NoPhysics;

#if PROJECTDAWN_NAV
        private bool ShowNavAgentsPopup => false;
#else
        private bool ShowNavAgentsPopup => AgentsNavigation;
#endif

#if RUKHANKA
        private bool ShowRukhankaPopup => false;
#else
        private bool ShowRukhankaPopup =>
            pedestrianRigType == NpcRigType.Rukhanka ||
            pedestrianRigType == NpcRigType.RukhankaHybrid;
#endif

        public event Action ConfigUpdated = delegate { };
        public event Action<NpcRigType> OnRigTypeChanged = delegate { };
        public event Action<bool> OnRagdollChanged = delegate { };

        public void SignalConfigUpdated()
        {
            if (Application.isPlaying)
            {
                ConfigUpdated();
            }
        }

        private void RigTypeChanged()
        {
            OnRigTypeChanged(pedestrianRigType);
        }

        private void RagdollChanged()
        {
            OnRagdollChanged(hasRagdoll);
        }
    }
}