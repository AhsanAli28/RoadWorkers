using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Factory
{
    [System.Serializable]
    public class CarPrefabPair
    {
        public GameObject HullPrefab;
        public GameObject EntityPrefab;

        [Tooltip("How often the car will spawn (spawn weight)")]
        [Range(0f, 1f)] public float Weight = 1f;
    }
}
