using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian.Authoring
{
    public class PedestrianSpawnerConfigHolder : SyncConfigBase, IConfigInject, IPedestrianSkinInfoProvider
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/pedestrianInit.html#pedestrian-spawner-config")]
        [SerializeField] private string link;

        [Expandable]
        [OnValueChanged(nameof(Sync))]
        [SerializeField] private PedestrianSpawnerConfig pedestrianSpawnerConfig;

        [Header("Pedestrian Settings")]
        [Expandable]
        [OnValueChanged(nameof(Sync))]
        [SerializeField] private PedestrianSettingsConfig pedestrianSettingsConfig;

        [Header("Prefab Settings")]
        [Expandable]
        [OnValueChanged(nameof(Sync))]
        [SerializeField] private PedestrianPrefabEntityData pedestrianPrefabEntityData;

        public int MinPedestrianCount { get => pedestrianSpawnerConfig.MinPedestrianCount; }
        public float MinSpawnDelay { get => pedestrianSpawnerConfig.MinSpawnDelay; }
        public float MaxSpawnDelay { get => pedestrianSpawnerConfig.MaxSpawnDelay; }
        public PedestrianSpawnerConfig PedestrianSpawnerConfig { get => pedestrianSpawnerConfig; set => pedestrianSpawnerConfig = value; }
        public PedestrianSettingsConfig PedestrianSettingsConfig { get => pedestrianSettingsConfig; set => pedestrianSettingsConfig = value; }
        public PedestrianPrefabEntityData PedestrianPrefabEntityData { get => pedestrianPrefabEntityData; }

        public int SkinCount
        {
            get
            {
                if (pedestrianSettingsConfig && pedestrianPrefabEntityData)
                {
                    if (pedestrianSettingsConfig.PedestrianRigType == NpcRigType.Rukhanka || pedestrianSettingsConfig.PedestrianRigType == NpcRigType.RukhankaHybrid)
                    {
                        if (pedestrianPrefabEntityData.PedestrianEntityPrefabData.TryGetValue("Rukhanka", out var prefabList))
                        {
                            return prefabList.List.Count;
                        }
                    }
                }

                return 0;
            }
        }

        public void InjectConfig(object config)
        {
            pedestrianSpawnerConfig = config as PedestrianSpawnerConfig;
        }

#if UNITY_EDITOR
        private bool configUpdated;
        private bool IsConfigUpdated => configUpdated && Application.isPlaying;

        public event System.Action<PedestrianSettingsConfig> ConfigUpdated = delegate { };

        private void Awake()
        {
            if (pedestrianSettingsConfig != null)
            {
                pedestrianSettingsConfig.ConfigUpdated += PedestrianSettingsConfig_ConfigUpdated;
            }
        }

        [ShowIf(nameof(IsConfigUpdated))]
        [Button]
        public void UpdateConfig()
        {
            configUpdated = false;
            ConfigUpdated(pedestrianSettingsConfig);
        }

        private void PedestrianSettingsConfig_ConfigUpdated()
        {
            configUpdated = true;
        }

        [OnInspectorEnable]
        private void OnInspectorEnabled()
        {
            pedestrianSpawnerConfig.HideRagdoll = !pedestrianSettingsConfig.HasRagdoll;
            pedestrianSpawnerConfig.HideHybrid = !pedestrianSettingsConfig.HybridSkin;

            pedestrianSettingsConfig.OnRigTypeChanged += PedestrianSettingsConfig_OnRigTypeChanged;
            pedestrianSettingsConfig.OnRagdollChanged += PedestrianSettingsConfig_OnRagdollChanged;
        }

        [OnInspectorDisable]
        private void OnInspectorDisabled()
        {
            pedestrianSpawnerConfig.HideRagdoll = false;
            pedestrianSpawnerConfig.HideHybrid = false;
            pedestrianSettingsConfig.OnRigTypeChanged -= PedestrianSettingsConfig_OnRigTypeChanged;
            pedestrianSettingsConfig.OnRagdollChanged -= PedestrianSettingsConfig_OnRagdollChanged;
        }

        private void PedestrianSettingsConfig_OnRigTypeChanged(NpcRigType obj)
        {
            pedestrianSpawnerConfig.HideHybrid = !pedestrianSettingsConfig.HybridSkin;
        }

        private void PedestrianSettingsConfig_OnRagdollChanged(bool obj)
        {
            pedestrianSpawnerConfig.HideRagdoll = !obj;
        }

#endif
    }
}