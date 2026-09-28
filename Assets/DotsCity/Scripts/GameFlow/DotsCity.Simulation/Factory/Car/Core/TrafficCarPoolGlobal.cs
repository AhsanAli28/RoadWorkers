using Spirit604.Attributes;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Traffic;
using Spirit604.DotsCity.Simulation.Traffic.Authoring;
using Spirit604.Extensions;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Spirit604.DotsCity.Simulation.Factory.Traffic
{
    public class TrafficCarPoolGlobal : MonoBehaviour
    {
        [DocLinker("https://dotstrafficcity.readthedocs.io/en/latest/trafficPreset.html")]
        [SerializeField] private string link;

        [SerializeField] private TrafficSettings trafficSettings;
        [SerializeField] private VehicleDataHolder vehicleDataHolder;
        [SerializeField] private TrafficEntityPoolBakerRef trafficEntityPoolBakerRef;

        private Dictionary<EntityType, Dictionary<int, ObjectPool>> pools = new Dictionary<EntityType, Dictionary<int, ObjectPool>>();

        private VehicleDataCollection VehicleDataCollection => vehicleDataHolder?.VehicleDataCollection ?? null;

        private int PoolSize
        {
            get
            {
                if (trafficSettings)
                {
                    return Mathf.Max(trafficSettings.PreferableCount, TrafficCarPoolBase.DefaultPoolSize);
                }

                return TrafficCarPoolBase.DefaultPoolSize;
            }
        }

        private void Start()
        {
            Assert.IsNotNull(trafficSettings);
            Assert.IsNotNull(trafficSettings.TrafficSettingsConfig);
            Assert.IsNotNull(trafficEntityPoolBakerRef);
            Assert.IsNotNull(VehicleDataCollection);

            if (!trafficSettings.TrafficSettingsConfig.HybridEntity)
                return;

            var type = trafficSettings.EntityType;

            var data = GetPoolData(type);
            var localPool = TrafficCarPoolBase.InitPool(VehicleDataCollection, name, data, PoolSize);

            if (localPool != null)
            {
                pools.Add(type, localPool);
            }
            else
            {
                if (type == trafficSettings.EntityType)
                {
                    Debug.Log($"TrafficCarPoolGlobal. Pool {type} not assigned");
                }
            }
        }

        public List<CarPrefabPair> GetPoolData(EntityType trafficEntityType)
        {
            return trafficEntityPoolBakerRef.GetPoolData(trafficEntityType);
        }

        public bool SetPreset(TrafficCarPoolPreset trafficCarPoolPreset, EntityType trafficEntityType)
        {
            return trafficEntityPoolBakerRef.SetPreset(trafficCarPoolPreset, trafficEntityType);
        }

        public TrafficCarPoolPreset GetPreset(EntityType trafficEntityType)
        {
            return trafficEntityPoolBakerRef.GetPreset(trafficEntityType);
        }

        public GameObject GetCarGo(EntityType trafficEntityType, int carModelType)
        {
            if (pools.ContainsKey(trafficEntityType))
            {
                if (pools[trafficEntityType].ContainsKey(carModelType))
                {
                    return pools[trafficEntityType][carModelType].Pop();
                }
                else
                {
                    Debug.Log($"{trafficEntityType} pool doesn't have {VehicleDataCollection.GetName(carModelType)} {carModelType} model");
                }
            }

            return null;
        }
    }
}
