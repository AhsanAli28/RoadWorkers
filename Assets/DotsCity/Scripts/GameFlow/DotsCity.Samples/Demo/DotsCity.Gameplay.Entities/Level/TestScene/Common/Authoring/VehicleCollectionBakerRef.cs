using Spirit604.Collections.Dictionary;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Car.Custom.Authoring;
using Spirit604.DotsCity.Simulation.Factory.Car;
using Spirit604.DotsCity.Simulation.Factory.Traffic;
using Spirit604.DotsCity.Simulation.Traffic;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.TestScene
{
    public class VehicleCollectionBakerRef : MonoBehaviour
    {
        [System.Serializable]
        public class PresetDictionary : AbstractSerializableDictionary<VehicleOwnerType, TrafficCarPoolPreset> { }

        [SerializeField] private VehicleDataHolder vehicleDataHolder;
        [SerializeField] private PresetDictionary presets = new PresetDictionary();
        [SerializeField] private bool hasInput;
        [SerializeField] private bool cleanSound = true;
        [SerializeField] private bool addPoolable = true;

        public VehicleDataCollection VehicleDataCollection => vehicleDataHolder?.VehicleDataCollection ?? null;

        public class VehicleCollectionBaker : Baker<VehicleCollectionBakerRef>
        {
            public override void Bake(VehicleCollectionBakerRef authoring)
            {
                if (authoring.presets.Keys.Count == 0)
                {
                    return;
                }

                foreach (var preset in authoring.presets)
                {
                    var prefabs = preset.Value.PrefabDatas;

                    foreach (var prefab in prefabs)
                    {
                        if (!prefab.EntityPrefab)
                        {
                            continue;
                        }

                        var vehicleAuthoring = prefab.EntityPrefab.GetComponent<VehicleAuthoring>();

                        if (!vehicleAuthoring)
                        {
                            continue;
                        }

                        var entity = GetEntity(prefab.EntityPrefab, TransformUsageFlags.Dynamic);

                        var prefabEntity = CreateAdditionalEntity(TransformUsageFlags.None);

                        int carModel = 0;

                        if (authoring.VehicleDataCollection)
                        {
                            var newCarModel = authoring.VehicleDataCollection.GetVehicleModelIndex(prefab.EntityPrefab);

                            if (newCarModel != -1)
                            {
                                carModel = newCarModel;
                            }
                        }

                        AddSharedComponent(prefabEntity, new PrefabContainerSort()
                        {
                            OwnerType = preset.Key
                        });

                        AddComponent(prefabEntity, new PrefabContainer()
                        {
                            Entity = entity,
                            HasInput = authoring.hasInput,
                            CleanSound = authoring.cleanSound,
                            AddPoolable = authoring.addPoolable,
                        });

                        AddComponent(prefabEntity, new CarModelBakingData()
                        {
                            VehicleEntity = entity,
                            CarModel = carModel
                        });
                    }
                }
            }
        }
    }
}