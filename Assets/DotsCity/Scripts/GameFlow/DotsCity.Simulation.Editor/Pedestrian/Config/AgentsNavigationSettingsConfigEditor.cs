using Spirit604.Extensions;
using Unity.AI.Navigation.Editor;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [CustomEditor(typeof(AgentsNavigationSettingsConfig))]
    class AgentsNavigationSettingsConfigEditor : Editor
    {
        static class Styles
        {
            public static readonly GUIContent Speed = EditorGUIUtility.TrTextContent("Speed", "Maximum movement speed when moving to destination.");
            public static readonly GUIContent Acceleration = EditorGUIUtility.TrTextContent("Acceleration", "The maximum acceleration of an agent as it follows a path, given in units / sec^2.");
            public static readonly GUIContent AngularSpeed = EditorGUIUtility.TrTextContent("Angular Speed", "Maximum turning speed in (deg/s) while following a path.");
            public static readonly GUIContent StoppingDistance = EditorGUIUtility.TrTextContent("Stopping Distance", "Stop within this distance from the target position.");
            public static readonly GUIContent AutoBreaking = EditorGUIUtility.TrTextContent("Auto Breaking", "Should the agent brake automatically to avoid overshooting the destination point?");
            public static readonly GUIContent Layers = EditorGUIUtility.TrTextContent("Layers", "");

            public static readonly GUIContent Radius = EditorGUIUtility.TrTextContent("Radius", "The radius of the agent.");
            public static readonly GUIContent Height = EditorGUIUtility.TrTextContent("Height", "The height of the agent for purposes of passing under obstacles, etc.");
            public static readonly Color32 Color = new Color32(255, 166, 89, 255);

            public static readonly GUIContent AgentTypeId = EditorGUIUtility.TrTextContent("Agent Type Id", "The type ID for the agent in NavMesh.");
            public static readonly GUIContent AreaMask = EditorGUIUtility.TrTextContent("Area Mask", "Specifies which NavMesh areas are passable. Changing areaMask will make the path stale (see isPathStale).");
            public static readonly GUIContent AutoRepath = EditorGUIUtility.TrTextContent("Auto Repath", "Should the agent attempt to acquire a new path if the existing path becomes invalid?");
            public static readonly GUIContent Grounded = EditorGUIUtility.TrTextContent("Grounded", "Anchors the agent to the surface. It is useful to disable then used with physics, to allow more freedom motion and precision.");
            public static readonly GUIContent OverrideAreaCosts = EditorGUIUtility.TrTextContent("Override Area Costs", "If enabled, allows overriden area cost for this agent.");
            public static readonly GUIContent LinkTraversalMode = EditorGUIUtility.TrTextContent("Link Traversal Mode", "Should the agent move across OffMeshLinks automatically?");
            public static readonly GUIContent MappingExtent = EditorGUIUtility.TrTextContent("Mapping Extent", "Maximum distance on each axis will be used when attempting to map the agent's position or destination onto navmesh. The higher the value, the bigger the performance cost.");

            public static readonly GUIContent SonarRadius = EditorGUIUtility.TrTextContent("Radius", "The maximum distance at which agent will attempt to avoid nearby agents.");
            public static readonly GUIContent Angle = EditorGUIUtility.TrTextContent("Velocity Obstacle Angle", "The angle of obstacle inserted behind agent velocity.");
            public static readonly GUIContent MaxAngle = EditorGUIUtility.TrTextContent("Max Angle", "The maximum angle at which agent will attempt to nearby agents.");
            public static readonly GUIContent Mode = EditorGUIUtility.TrTextContent("Mode", "Mode that modifies avoidance behaviour.");
            public static readonly GUIContent BlockedStop = EditorGUIUtility.TrTextContent("Blocked Stop", "Whenever agent should stop, if all directions are blocked.");
            public static readonly GUIContent UseWalls = EditorGUIUtility.TrTextContent("Use Walls", "Should avoidance account for static obstacles. Having this option enable will cost more performance.");
            public static readonly GUIContent SonarLayers = EditorGUIUtility.TrTextContent("Layers", "Should avoidance account for static obstacles. Having this option enable will cost more performance.");
        }

        private AgentsNavigationSettingsConfig navAgentSettingsConfig;

        private static SerializedProperty m_Acceleration;
        private static SerializedProperty m_AngularSpeed;
        private static SerializedProperty m_StoppingDistance;
        private static SerializedProperty m_AutoBreaking;
        private static SerializedProperty m_Layers;

        private static SerializedProperty m_Radius;
        private static SerializedProperty m_Height;

        private static SerializedProperty m_AgentTypeId;
        private static SerializedProperty m_AreaMask;
        private static SerializedProperty m_AutoRepath;
        private static SerializedProperty m_Grounded;
        private static SerializedProperty m_OverrideAreaCosts;
        private static SerializedProperty m_LinkTraversalMode;
        private static SerializedProperty m_MappingExtent;

        private static SerializedProperty m_HasCollider;
        private static SerializedProperty m_ColliderLayers;

        private static SerializedProperty m_AgentAvoidanceType;

        private static SerializedProperty m_SonarRadius;
        private static SerializedProperty m_Angle;
        private static SerializedProperty m_MaxAngle;
        private static SerializedProperty m_Mode;
        private static SerializedProperty m_BlockedStop;
        private static SerializedProperty m_UseWalls;
        private static SerializedProperty m_SonarLayers;

        private static SerializedProperty m_ReciprocalRadius;
        private static SerializedProperty m_ReciprocalLayers;

        private void OnEnable()
        {
            navAgentSettingsConfig = target as AgentsNavigationSettingsConfig;
            InitProps(serializedObject);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            Draw();
            serializedObject.ApplyModifiedProperties();
        }

        public static void Draw()
        {
            InspectorExtension.DrawGroupBox("Agent", () =>
            {
                EditorGUILayout.PropertyField(m_Acceleration, Styles.Acceleration);
                EditorGUILayout.PropertyField(m_AngularSpeed, Styles.AngularSpeed);
                EditorGUILayout.PropertyField(m_StoppingDistance, Styles.StoppingDistance);
                EditorGUILayout.PropertyField(m_AutoBreaking, Styles.AutoBreaking);
            });

            InspectorExtension.DrawGroupBox("Agent Shape", () =>
            {
                EditorGUILayout.PropertyField(m_Layers, Styles.Layers);

                EditorGUILayout.PropertyField(m_Radius, Styles.Radius);
                EditorGUILayout.PropertyField(m_Height, Styles.Height);
            });

            InspectorExtension.DrawGroupBox("Agent NavMesh Pathing", () =>
            {
                NavMeshComponentsGUIUtility.AgentTypePopup("Agent Type", m_AgentTypeId);

                AreaMaskField(m_AreaMask, Styles.AreaMask);

                EditorGUILayout.PropertyField(m_AutoRepath, Styles.AutoRepath);
                EditorGUILayout.PropertyField(m_Grounded, Styles.Grounded);

                using (new EditorGUI.DisabledScope(Application.isPlaying))
                {
                    EditorGUILayout.PropertyField(m_OverrideAreaCosts, Styles.OverrideAreaCosts);
                    EditorGUILayout.PropertyField(m_LinkTraversalMode, Styles.LinkTraversalMode);
                }

                EditorGUILayout.PropertyField(m_MappingExtent, Styles.MappingExtent);
            });

            InspectorExtension.DrawGroupBox("Agent Collider", () =>
            {
                EditorGUILayout.PropertyField(m_HasCollider);

                if (m_HasCollider.boolValue)
                {
                    EditorGUILayout.PropertyField(m_ColliderLayers, new GUIContent("Layers"));
                }
            });

            InspectorExtension.DrawGroupBox("Agent Avoidance", () =>
            {
                EditorGUILayout.PropertyField(m_AgentAvoidanceType);

                var agentAvoidanceType = (AgentsNavigationSettingsConfig.AvoidanceType)m_AgentAvoidanceType.enumValueIndex;

                switch (agentAvoidanceType)
                {
                    case AgentsNavigationSettingsConfig.AvoidanceType.SonarAvoidance:
                        {
                            EditorGUILayout.PropertyField(m_SonarRadius, Styles.SonarRadius);
                            EditorGUILayout.PropertyField(m_Angle, Styles.Angle);
                            EditorGUILayout.PropertyField(m_MaxAngle, Styles.MaxAngle);
#if EXPERIMENTAL_SONAR_TIME
                            using (new EditorGUI.DisabledGroupScope(true))
                            {
                                EditorGUILayout.PropertyField(m_Mode, Styles.Mode);
                                Rect controlRect = EditorGUILayout.GetControlRect();
                                controlRect.height = 20;
                                EditorGUI.HelpBox(controlRect, "This option is skipped with `Sonar Time Horizon` enabled!", MessageType.Warning);
                            }
#else
                            EditorGUILayout.PropertyField(m_Mode, Styles.Mode);
#endif
                            EditorGUILayout.PropertyField(m_BlockedStop, Styles.BlockedStop);
                            EditorGUILayout.PropertyField(m_SonarLayers, Styles.SonarLayers);
                            break;
                        }
                    case AgentsNavigationSettingsConfig.AvoidanceType.ReciprocalAvoidance:
                        {
                            EditorGUILayout.PropertyField(m_ReciprocalRadius, new GUIContent("Radius"));
                            EditorGUILayout.PropertyField(m_ReciprocalLayers, new GUIContent("Layers"));
                            break;
                        }
                }
            });
        }

        public static void InitProps(SerializedObject serializedObject)
        {
            m_Acceleration = serializedObject.FindProperty("Acceleration");
            m_AngularSpeed = serializedObject.FindProperty("AngularSpeed");
            m_StoppingDistance = serializedObject.FindProperty("StoppingDistance");
            m_AutoBreaking = serializedObject.FindProperty("AutoBreaking");
            m_Layers = serializedObject.FindProperty("m_Layers");

            m_Radius = serializedObject.FindProperty("Radius");
            m_Height = serializedObject.FindProperty("Height");

            m_AgentTypeId = serializedObject.FindProperty("AgentTypeId");
            m_AreaMask = serializedObject.FindProperty("AreaMask");
            m_AutoRepath = serializedObject.FindProperty("AutoRepath");
            m_Grounded = serializedObject.FindProperty("m_Grounded");
            m_OverrideAreaCosts = serializedObject.FindProperty("m_OverrideAreaCosts");
            m_MappingExtent = serializedObject.FindProperty("MappingExtent");
            m_LinkTraversalMode = serializedObject.FindProperty("m_LinkTraversalMode");

            m_HasCollider = serializedObject.FindProperty("HasCollider");
            m_ColliderLayers = serializedObject.FindProperty("m_ColliderLayers");

            m_AgentAvoidanceType = serializedObject.FindProperty("AgentAvoidanceType");

            m_SonarRadius = serializedObject.FindProperty("SonarRadius");
            m_Angle = serializedObject.FindProperty("Angle");
            m_MaxAngle = serializedObject.FindProperty("MaxAngle");
            m_Mode = serializedObject.FindProperty("Mode");
            m_BlockedStop = serializedObject.FindProperty("BlockedStop");
            m_UseWalls = serializedObject.FindProperty("UseWalls");
            m_SonarLayers = serializedObject.FindProperty("m_SonarLayers");

            m_ReciprocalRadius = serializedObject.FindProperty("ReciprocalRadius");
            m_ReciprocalLayers = serializedObject.FindProperty("ReciprocalLayers");
        }

        private static void AreaMaskField(SerializedProperty property, GUIContent label)
        {
            var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginProperty(rect, label, property);

            var areaIndex = 0;

#if !UNITY_6000_0_OR_NEWER
            var areaNames = GameObjectUtility.GetNavMeshAreaNames();
#else
            var areaNames = UnityEngine.AI.NavMesh.GetAreaNames();
#endif

            for (var i = 0; i < areaNames.Length; i++)
            {
#if !UNITY_6000_0_OR_NEWER
                var areaValue = 1 << GameObjectUtility.GetNavMeshAreaFromName(areaNames[i]);
#else
                var areaValue = 1 << UnityEngine.AI.NavMesh.GetAreaFromName(areaNames[i]);
#endif

                if ((areaValue & property.intValue) != 0)
                    areaIndex |= 1 << i;
            }

            EditorGUI.BeginChangeCheck();
            int value = EditorGUI.MaskField(rect, label, areaIndex, areaNames);
            if (EditorGUI.EndChangeCheck())
            {
                areaIndex = 0;
                for (var i = 0; i < areaNames.Length; i++)
                {
#if !UNITY_6000_0_OR_NEWER
                    var areaValue = 1 << GameObjectUtility.GetNavMeshAreaFromName(areaNames[i]);
#else
                    var areaValue = 1 << UnityEngine.AI.NavMesh.GetAreaFromName(areaNames[i]);
#endif

                    if ((value & 1 << i) != 0)
                        areaIndex |= areaValue;
                }

                property.intValue = areaIndex;
            }

            EditorGUI.EndProperty();
        }
    }
}