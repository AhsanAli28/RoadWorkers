using UnityEngine;

#if PROJECTDAWN_NAV
using Spirit604.CityEditor;
using ProjectDawn.Navigation;
#endif

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
#if PROJECTDAWN_NAV
    [CreateAssetMenu(fileName = "AgentsNavigationSettingsConfig", menuName = CityEditorBookmarks.CITY_EDITOR_LEVEL_CONFIG_LEVEL_PATH + "Pedestrian/AgentsNavigationSettingsConfig")]
#endif
    public class AgentsNavigationSettingsConfig : ScriptableObject
    {
        public enum AvoidanceType { Disabled, SonarAvoidance, ReciprocalAvoidance }

#if PROJECTDAWN_NAV
        #region AgentAuthoring

        public float Acceleration = 8f;
        public float AngularSpeed = 180f;
        public float StoppingDistance = 0.1f;
        public bool AutoBreaking = true;
        public NavigationLayers m_Layers = NavigationLayers.Default;

        #endregion

        #region ShapeAuthoring

        public float Radius = 0.3f;
        public float Height = 1f;
        public ShapeType Type = ShapeType.Cylinder;

        #endregion

        #region ColliderAuthoring

        public bool HasCollider = true;

        public NavigationLayers m_ColliderLayers = NavigationLayers.Everything;

        #endregion

        #region AgentAuthoring

        public int AgentTypeId = 0;

        public int AreaMask = -1;

        public bool AutoRepath = true;

        public bool m_Grounded = true;

        public bool m_OverrideAreaCosts;

        public NavMeshLinkTraversalMode m_LinkTraversalMode = NavMeshLinkTraversalMode.None;

        #endregion

        public AvoidanceType AgentAvoidanceType = AgentsNavigationSettingsConfig.AvoidanceType.SonarAvoidance;

        #region AgentSonarAvoid Authoring

        public float SonarRadius = 2f;

        public Vector3 MappingExtent = new Vector3(5, 5, 5);

        public float Angle = 180f;
        public float MaxAngle = 300f;

        public SonarAvoidMode Mode = SonarAvoidMode.IgnoreBehindAgents;

        public bool BlockedStop = false;
        public bool UseWalls = true;

        public NavigationLayers m_SonarLayers = NavigationLayers.Everything;

        #endregion

        #region Reciprocal Authoring

        public float ReciprocalRadius = 2f;
        public NavigationLayers ReciprocalLayers = NavigationLayers.Everything;

        #endregion
#endif
    }
}