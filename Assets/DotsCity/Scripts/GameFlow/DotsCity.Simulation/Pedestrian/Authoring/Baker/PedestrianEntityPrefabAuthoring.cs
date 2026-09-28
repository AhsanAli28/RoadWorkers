using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Config;
using Spirit604.DotsCity.Simulation.Root.Authoring;
using Spirit604.Extensions;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian.Authoring
{
    public class PedestrianEntityPrefabAuthoring : MonoBehaviourBase, ISyncableConfig
    {
        [SerializeField] private PedestrianSettingsConfig pedestrianSettingsConfig;

        [SerializeField] private CitySettingsInitializerBase citySettingsInitializerBase;

        [Expandable]
        [SerializeField] private PedestrianPrefabEntityData pedestrianPrefabEntityData;

        public PedestrianSettingsConfig PedestrianSettingsConfig
        {
            get => pedestrianSettingsConfig;
            set
            {
                if (pedestrianSettingsConfig != value)
                {
                    pedestrianSettingsConfig = value;
                    EditorSaver.SetObjectDirty(this);
                }
            }
        }

        public GeneralSettingDataSimulation GeneralSettings
        {
            get
            {
                if (citySettingsInitializerBase)
                    return citySettingsInitializerBase.GetSettings<GeneralSettingDataSimulation>();

                return null;
            }
        }

        public PedestrianPrefabEntityData PedestrianPrefabEntityData
        {
            get => pedestrianPrefabEntityData;
            set
            {
                if (pedestrianPrefabEntityData != value)
                {
                    pedestrianPrefabEntityData = value;
                    EditorSaver.SetObjectDirty(this);
                }
            }
        }

        [Button]
        public void SyncConfig()
        {
            var pedestrianSpawnerConfigHolder = ObjectUtils.FindObjectOfType<PedestrianSpawnerConfigHolder>();

            if (pedestrianSpawnerConfigHolder)
            {
                PedestrianSettingsConfig = pedestrianSpawnerConfigHolder.PedestrianSettingsConfig;
                PedestrianPrefabEntityData = pedestrianSpawnerConfigHolder.PedestrianPrefabEntityData;
            }

            if (!citySettingsInitializerBase)
            {
                citySettingsInitializerBase = ObjectUtils.FindObjectOfType<CitySettingsInitializerBase>();
            }
        }

        class PedestrianEntityPrefabAuthoringBaker : Baker<PedestrianEntityPrefabAuthoring>
        {
            public override void Bake(PedestrianEntityPrefabAuthoring authoring)
            {
                DependsOn(authoring.GeneralSettings);
                DependsOn(authoring.pedestrianSettingsConfig);
                DependsOn(authoring.pedestrianPrefabEntityData);

                var entity = CreateAdditionalEntity(TransformUsageFlags.None);

                var prefabs = authoring.GetPrefabs();

                var buffer = AddBuffer<PedestrianEntityPrefabContainer>(entity);

                for (int i = 0; i < prefabs?.Count; i++)
                {
                    var prefab = prefabs[i];

                    if (prefab == null) continue;

                    var prefabEntity = GetEntity(prefab, TransformUsageFlags.Dynamic);

                    buffer.Add(new PedestrianEntityPrefabContainer()
                    {
                        PrefabEntity = prefabEntity
                    });
                }

#if RUKHANKA
                if (authoring.pedestrianSettingsConfig.PedestrianRigType == NpcRigType.Rukhanka && authoring.pedestrianSettingsConfig.HasRagdoll)
                {
                    var prefabContainer = authoring.pedestrianPrefabEntityData;

                    if (prefabContainer.PedestrianEntityPrefabData.TryGetValue("RukhankaRagdoll", out var ragdollData))
                    {
                        var ragdolls = ragdollData.List;

                        int count = ragdolls.Count;

                        if (prefabs.Count != ragdolls.Count)
                        {
                            count = Mathf.Min(prefabs.Count, ragdolls.Count);
                            Debug.LogError($"PedestrianEntityPrefabAuthoring. Prefabs {prefabs.Count} for Rukhanka & ragdolls {ragdolls.Count} for RukhankaRagdoll should match");
                        }

                        var ragdollBuffer = AddBuffer<RukhankaRagdollPrefabContainer>(entity);

                        for (int i = 0; i < count; i++)
                        {
                            var prefab = ragdolls[i];

                            if (prefab == null) continue;

                            var prefabEntity = GetEntity(prefab, TransformUsageFlags.Dynamic);

                            ragdollBuffer.Add(new RukhankaRagdollPrefabContainer()
                            {
                                PrefabEntity = prefabEntity
                            });
                        }
                    }
                    else
                    {
                        Debug.LogError($"PedestrianEntityPrefabAuthoring. 'RukhankaRagdoll' key not found for ragdoll");
                    }
                }
#endif
            }
        }

        private List<PedestrianAuthoring> GetPrefabs()
        {
            var pedestrianEntityType = PedestrianSettingsConfig.PedestrianEntityType;

            string prefabKey = string.Empty;

            var rigType = PedestrianSettingsConfig.PedestrianRigType;

            switch (rigType)
            {
                case NpcRigType.Rukhanka:
                    {
                        prefabKey = "Rukhanka";
                        break;
                    }
                case NpcRigType.RukhankaHybrid:
                    {
                        prefabKey = "Rukhanka";
                        break;
                    }
                default:
                    {
                        if (GeneralSettings && PedestrianSettingsConfig.PedestrianEntityType == EntityType.Physics && this.GeneralSettings.SimulationType == Unity.Physics.SimulationType.NoPhysics)
                        {
                            pedestrianEntityType = EntityType.NoPhysics;
                        }

                        prefabKey = pedestrianEntityType.ToString();
                        break;
                    }
            }

            if (pedestrianPrefabEntityData.PedestrianEntityPrefabData.TryGetValue(prefabKey, out var sourceData) && sourceData != null)
            {
                return sourceData.List;
            }
            else
            {
                Debug.LogError($"PedestrianEntityPrefabAuthoring. PrefabKey {prefabKey} not found!");
            }

            return null;
        }
    }
}