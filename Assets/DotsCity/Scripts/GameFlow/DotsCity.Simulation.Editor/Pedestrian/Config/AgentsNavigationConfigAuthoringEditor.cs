using Spirit604.Extensions;
using System;
using UnityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian.Authoring
{
    [CustomEditor(typeof(AgentsNavigationConfigAuthoring))]
    public class AgentsNavigationConfigAuthoringEditor : SharedSettingsEditorBase<AgentsNavigationConfigAuthoringEditor.EditorSettings>
    {
        [Serializable]
        public class EditorSettings
        {
            public bool ShowSettings;
        }

        private const string TipKey = "AgentsNavigationConfigAuthoringEditorTip";

        private AgentsNavigationConfigAuthoring agentConfig;
        private PedestrianSpawnerConfigHolder holder;
        private AgentsNavigationSettingsConfig initConfig;
        private SerializedObject configSo;

        protected override string SaveKey => "AgentsNavigationConfigAuthoringEditor";

        private bool Agents => holder && holder.PedestrianSettingsConfig && holder.PedestrianSettingsConfig.ObstacleAvoidanceType == Npc.Navigation.ObstacleAvoidanceType.AgentsNavigation && holder.PedestrianSettingsConfig.AutoAddAgentComponents;

#if PROJECTDAWN_NAV
        protected override void OnEnable()
        {
            base.OnEnable();

            agentConfig = target as AgentsNavigationConfigAuthoring;

            holder = agentConfig.GetComponent<PedestrianSpawnerConfigHolder>();
        }

        public override void OnInspectorGUI()
        {
            if (!Agents)
                return;

            serializedObject.Update();

            InspectorExtension.DrawGroupBox("Agents Navigation Settings", () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(agentConfig.config)));

                if (agentConfig.config == null)
                {
                    if (GUILayout.Button("Generate"))
                    {
                        Generate();
                    }

                    EditorTipExtension.TryToShowInspectorTip(TipKey, "Generate config if you want to customize agents via config, otherwise default values will be used.");
                }
                else
                {
                    if (initConfig != agentConfig.config)
                    {
                        initConfig = agentConfig.config;

                        if (initConfig)
                        {
                            configSo = new SerializedObject(agentConfig.config);
                            AgentsNavigationSettingsConfigEditor.InitProps(configSo);
                        }
                        else
                        {
                            configSo = null;
                        }
                    }

                    if (configSo != null)
                    {
                        configSo.Update();
                        AgentsNavigationSettingsConfigEditor.Draw();
                        configSo.ApplyModifiedProperties();
                    }
                }

            }, ref SharedSettings.ShowSettings);

            serializedObject.ApplyModifiedProperties();
        }
#else
        public override void OnInspectorGUI() { }
#endif

        private void Generate()
        {
            if (!holder || !holder.PedestrianSettingsConfig)
                return;

            var prefab = PrefabUtility.GetNearestPrefabInstanceRoot(holder.gameObject);

            if (prefab == null)
            {
                Debug.LogError($"GetNearestPrefabInstanceRoot is null");
                return;
            }

            prefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(prefab);

            if (prefab == null)
            {
                Debug.LogError($"Original prefab not found");
                return;
            }

            PrefabExtension.EditPrefab(prefab, (prefabRef) =>
            {
                var authoring = prefabRef.GetComponentInChildren<AgentsNavigationConfigAuthoring>();

                if (authoring)
                {
                    var config = AssetDatabaseExtension.CreatePersistScriptableObject<AgentsNavigationSettingsConfig>("Assets/", "AgentsNavigationConfig");
                    authoring.config = config;
                    EditorSaver.SetObjectDirty(authoring.config);

                    Debug.Log("Auto-generated 'Assets/AgentsNavigationConfig.asset'");
                }
                else
                {
                    Debug.LogError($"Failed to find 'AgentsNavigationSettingsConfig' in the '{prefabRef.name}' prefab");
                }
            });
        }

        protected override EditorSettings GetDefaultSettings() => new EditorSettings();
    }
}
