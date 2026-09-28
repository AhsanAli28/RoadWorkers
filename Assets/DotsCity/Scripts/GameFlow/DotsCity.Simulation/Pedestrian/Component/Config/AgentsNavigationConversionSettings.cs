#if PROJECTDAWN_NAV
using ProjectDawn.Navigation;
using Unity.Entities;
using Unity.Mathematics;
using static Spirit604.DotsCity.Simulation.Pedestrian.AgentsNavigationSettingsConfig;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public struct AgentsNavigationConversionSettings
    {
        #region AgentAuthoring

        public float Acceleration;
        public float AngularSpeed;
        public float StoppingDistance;
        public bool AutoBreaking;
        public NavigationLayers m_Layers;

        #endregion

        #region ShapeAuthoring

        public float Radius;
        public float Height;
        public ShapeType Type;

        #endregion

        #region ColliderAuthoring

        public bool HasCollider;

        public NavigationLayers m_ColliderLayers;

        #endregion

        #region AgentAuthoring

        public int AgentTypeId;

        public int AreaMask;

        public bool AutoRepath;

        public bool m_Grounded;

        public bool m_OverrideAreaCosts;

        public NavMeshLinkTraversalMode m_LinkTraversalMode;

        #endregion

        #region Avoidance

        public AvoidanceType AgentAvoidanceType;

        #region AgentSonarAvoid Authoring

        public float SonarRadius;

        public float3 MappingExtent;

        public float Angle;
        public float MaxAngle;

        public SonarAvoidMode Mode;

        public bool BlockedStop;
        public bool UseWalls;

        public NavigationLayers m_SonarLayers;

        #endregion

        #region Reciprocal Authoring

        public float ReciprocalRadius;
        public NavigationLayers ReciprocalLayers;

        #endregion

        #endregion

        #region Constructor

        public static AgentsNavigationConversionSettings GetDefault()
        {
            AgentsNavigationConversionSettings settings;
            settings.m_Layers = NavigationLayers.Default;

            settings.Acceleration = 8f;
            settings.AngularSpeed = math.radians(180f);
            settings.StoppingDistance = 0.1f;
            settings.AutoBreaking = true;

            settings.Radius = 0.3f;
            settings.Height = 1f;
            settings.Type = ShapeType.Cylinder;

            settings.HasCollider = true;
            settings.m_ColliderLayers = NavigationLayers.Everything;

            settings.AgentTypeId = 0;
            settings.AreaMask = -1;
            settings.AutoRepath = true;
            settings.m_Grounded = true;
            settings.m_OverrideAreaCosts = false;
            settings.m_LinkTraversalMode = NavMeshLinkTraversalMode.None;

            settings.AgentAvoidanceType = AgentsNavigationSettingsConfig.AvoidanceType.SonarAvoidance;

            settings.SonarRadius = 2f;
            settings.MappingExtent = new float3(5, 5, 5);
            settings.Angle = math.radians(180f);
            settings.MaxAngle = math.radians(300f);
            settings.Mode = SonarAvoidMode.IgnoreBehindAgents;
            settings.BlockedStop = false;
            settings.UseWalls = true;
            settings.m_SonarLayers = NavigationLayers.Everything;

            settings.ReciprocalRadius = 2f;
            settings.ReciprocalLayers = NavigationLayers.Everything;

            return settings;
        }

        public AgentsNavigationConversionSettings(AgentsNavigationSettingsConfig navAgentSettingsConfig)
        {
            m_Layers = navAgentSettingsConfig.m_Layers;

            Acceleration = navAgentSettingsConfig.Acceleration;
            AngularSpeed = math.radians(navAgentSettingsConfig.AngularSpeed);
            StoppingDistance = navAgentSettingsConfig.StoppingDistance;
            AutoBreaking = navAgentSettingsConfig.AutoBreaking;

            Radius = navAgentSettingsConfig.Radius;
            Height = navAgentSettingsConfig.Height;
            Type = navAgentSettingsConfig.Type;

            HasCollider = navAgentSettingsConfig.HasCollider;
            m_ColliderLayers = navAgentSettingsConfig.m_ColliderLayers;

            AgentTypeId = navAgentSettingsConfig.AgentTypeId;
            AreaMask = navAgentSettingsConfig.AreaMask;
            AutoRepath = navAgentSettingsConfig.AutoRepath;
            m_Grounded = navAgentSettingsConfig.m_Grounded;
            m_OverrideAreaCosts = navAgentSettingsConfig.m_OverrideAreaCosts;
            m_LinkTraversalMode = navAgentSettingsConfig.m_LinkTraversalMode;

            AgentAvoidanceType = navAgentSettingsConfig.AgentAvoidanceType;

            SonarRadius = navAgentSettingsConfig.SonarRadius;
            MappingExtent = navAgentSettingsConfig.MappingExtent;
            Angle = math.radians(navAgentSettingsConfig.Angle);
            MaxAngle = math.radians(navAgentSettingsConfig.MaxAngle);
            Mode = navAgentSettingsConfig.Mode;
            BlockedStop = navAgentSettingsConfig.BlockedStop;
            UseWalls = navAgentSettingsConfig.UseWalls;
            m_SonarLayers = navAgentSettingsConfig.m_SonarLayers;

            ReciprocalRadius = navAgentSettingsConfig.ReciprocalRadius;
            ReciprocalLayers = navAgentSettingsConfig.ReciprocalLayers;
        }

        #endregion
    }

    public struct AgentsNavigationConversionSettingsReference : IComponentData
    {
        public BlobAssetReference<AgentsNavigationConversionSettings> Config;
    }
}
#endif
