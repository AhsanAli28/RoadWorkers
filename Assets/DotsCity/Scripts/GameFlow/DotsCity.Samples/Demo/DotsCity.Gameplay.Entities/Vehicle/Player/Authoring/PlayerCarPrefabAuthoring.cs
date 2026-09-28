using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Gameplay.Factory.Player;
using Spirit604.DotsCity.Gameplay.Initialization;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Factory;
using Spirit604.DotsCity.Simulation.Factory.Car;
using Spirit604.DotsCity.Simulation.Factory.Traffic;
using Spirit604.DotsCity.Simulation.Root.Authoring;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.Extensions;
using Spirit604.Gameplay.Player;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player.Authoring
{
    public struct PlayerCarPrefabContainer : IComponentData
    {
        public Entity PrefabEntity;
        public int CarModel;
    }

    public class PlayerCarPrefabAuthoring : SyncConfigBase, ISyncableConfig
    {
        [SerializeField] private CitySettingsInitializer citySettingsInitializer;
        [SerializeField] private VehicleDataHolder vehicleDataHolder;

        [Expandable]
        [OnValueChanged(nameof(Sync))]
        [SerializeField] private TrafficCarPoolPreset playerCarPoolPreset;

        public TrafficCarPoolPreset PlayerCarPoolPreset
        {
            get => playerCarPoolPreset;
            set
            {
                if (playerCarPoolPreset != value)
                {
                    playerCarPoolPreset = value;
                    EditorSaver.SetObjectDirty(this);
                }
            }
        }

        public bool HasPlayer => citySettingsInitializer.Settings.CameraViewType == PlayerAgentType.Player;

#if UNITY_EDITOR
        private void Start()
        {
            PlayerCarPool playerCarPool = GetPlayerPool();

            if (playerCarPool)
            {
                if (PlayerCarPoolPreset != playerCarPool.CarPoolPreset)
                {
                    UnityEngine.Debug.Log("PlayerCarPrefabAuthoring. <b>PlayerCarPrefabAuthoring</b> & <b>PlayerCarPool</b> have different presets, make sure you assign the same preset to both & synchronised with subscene");
                }
            }
        }
#endif

        [Button]
        public void SyncConfig()
        {
            PlayerCarPool playerCarPool = GetPlayerPool();

            if (playerCarPool)
            {
                PlayerCarPoolPreset = playerCarPool.CarPoolPreset;
            }
        }

        private PlayerCarPool GetPlayerPool() => ObjectUtils.FindObjectOfType<PlayerCarPool>();

        class PlayerCarPrefabBaker : Baker<PlayerCarPrefabAuthoring>
        {
            public override void Bake(PlayerCarPrefabAuthoring authoring)
            {
                DependsOn(authoring.playerCarPoolPreset);

                if (!authoring.vehicleDataHolder || !authoring.vehicleDataHolder.VehicleDataCollection)
                {
                    UnityEngine.Debug.Log("PlayerCarPrefabAuthoring. VehicleDataCollection preset not assigned");
                    return;
                }

                DependsOn(authoring.vehicleDataHolder.VehicleDataCollection);

                var preset = authoring.playerCarPoolPreset;

                if (preset == null)
                {
                    UnityEngine.Debug.Log("PlayerCarPrefabAuthoring. Preset not assigned");
                    return;
                }

                for (int i = 0; i < preset.PrefabDatas.Count; i++)
                {
                    CarPrefabPair item = preset.PrefabDatas[i];

                    if (item.EntityPrefab == null)
                    {
                        UnityEngine.Debug.Log($"PlayerCarPrefabAuthoring. {i} entity prefab is null");
                        continue;
                    }

                    var carModel = authoring.vehicleDataHolder.VehicleDataCollection.GetVehicleModelIndex(item.EntityPrefab);

                    if (carModel == -1 && authoring.HasPlayer)
                    {
                        UnityEngine.Debug.LogError($"PlayerCarPrefabAuthoring. Preset '{preset.name}' Vehicle {item.EntityPrefab.name} model index not found. Make sure you have added a vehicle ID to the vehicle collection.");
                        continue;
                    }

                    var prefabContainerEntity = CreateAdditionalEntity(TransformUsageFlags.None);
                    var prefabEntity = GetEntity(item.EntityPrefab.gameObject, TransformUsageFlags.Dynamic);

                    AddComponent(prefabContainerEntity, new PlayerCarPrefabContainer()
                    {
                        PrefabEntity = prefabEntity,
                        CarModel = carModel
                    });

                    AddComponent(prefabContainerEntity, new CarModelBakingData()
                    {
                        VehicleEntity = prefabEntity,
                        CarModel = carModel
                    });
                }
            }
        }
    }
}