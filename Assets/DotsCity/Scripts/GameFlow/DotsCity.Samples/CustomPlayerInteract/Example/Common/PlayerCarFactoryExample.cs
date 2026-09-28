using Spirit604.DotsCity.Simulation.Car;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.Samples.PlayerInteract
{
    public class PlayerCarFactoryExample : MonoBehaviour
    {
        [SerializeField] private List<GameObject> prefabs = new List<GameObject>();

        private Dictionary<int, GameObject> prefabData = new Dictionary<int, GameObject>();

        private void Awake()
        {
            for (int i = 0; i < prefabs.Count; i++)
            {
                var carModelRuntime = prefabs[i].GetComponent<CarModelRuntimeAuthoring>();

                if (carModelRuntime)
                {
                    var carModel = carModelRuntime.CarModel;

                    if (!prefabData.ContainsKey(carModel))
                    {
                        prefabData.Add(carModel, prefabs[i]);
                    }
                    else
                    {
                        UnityEngine.Debug.Log($"PlayerCarFactoryExample. Prefab '{prefabs[i].name}' car model '{carModel}' collision with '{prefabData[carModel].name}'");
                    }
                }
                else
                {
                    UnityEngine.Debug.Log($"PlayerCarFactoryExample. Prefab '{prefabs[i].name}' component 'CarModelRuntimeAuthoring' not found");
                }
            }
        }

        public GameObject Get(int carModel)
        {
            if (prefabData.TryGetValue(carModel, out var prefab))
            {
                return Instantiate(prefab);
            }
            else
            {
                UnityEngine.Debug.Log($"PlayerCarFactoryExample. Prefab car model '{carModel}' not found");
                return null;
            }
        }
    }
}