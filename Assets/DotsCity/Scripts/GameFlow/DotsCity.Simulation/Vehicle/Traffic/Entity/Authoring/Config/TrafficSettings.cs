using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic.Authoring
{
    public class TrafficSettings : SyncConfigBase
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/trafficCarConfigs.html#traffic-car-spawner-config")]
        [SerializeField] private string link;

        [ShowIf(nameof(GeneralSettingsIsNull))]
        [SerializeField] private CitySettingsInitializerBase citySettingsInitializer;

        [Space]
        [Header("Spawn Settings")]
        [Expandable]
        [OnValueChanged(nameof(Sync))]
        [SerializeField] private TrafficCarSpawnerConfig trafficCarSpawnerConfig;

        [Space]
        [Header("Traffic Car Settings")]
        [Space]
        [Expandable]
        [OnValueChanged(nameof(Sync))]
        [SerializeField] private TrafficCarSettingsConfig trafficCarSettingsConfig;

        public int PreferableCount => trafficCarSpawnerConfig.PreferableCount;

        public TrafficCarSpawnerConfig TrafficSpawnerConfig { get => trafficCarSpawnerConfig; set => trafficCarSpawnerConfig = value; }

        public TrafficCarSettingsConfig TrafficSettingsConfig
        {
            get
            {
                SetConfig();
                return trafficCarSettingsConfig;
            }
            set => trafficCarSettingsConfig = value;
        }

        public EntityType EntityType
        {
            get
            {
                if (trafficCarSettingsConfig)
                {
                    if (citySettingsInitializer)
                    {
                        if (citySettingsInitializer.DOTSSimulation)
                        {
                            return trafficCarSettingsConfig.EntityType;
                        }
                        else
                        {
                            return EntityType.HybridEntityMonoPhysics;
                        }
                    }

                    return trafficCarSettingsConfig.EntityType;
                }

                return default;
            }
        }

        private bool GeneralSettingsIsNull => !citySettingsInitializer;

#if UNITY_EDITOR
        private void Update()
        {
            if (Extensions.InputExtension.GetKeyDown(KeyCode.Backspace))
            {
                ClearCars();
            }
        }
#endif

        [Button]
        public void ForceSpawn()
        {
            Unity.Entities.World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TrafficSpawnerSystem>().AddCars(true);
        }

        [Button]
        public void ClearCars()
        {
            Unity.Entities.World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TrafficCleanerSystem>().Clear();
        }

#if UNITY_EDITOR
        [OnInspectorEnable]
        protected void OnInspectorEnabled2()
        {
            SetConfig();
        }
#endif

        private void SetConfig()
        {
            if (citySettingsInitializer && trafficCarSettingsConfig)
            {
                trafficCarSettingsConfig.GeneralSettingData = citySettingsInitializer.GetSettings<GeneralSettingDataCore>();
            }
        }
    }
}