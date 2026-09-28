using Spirit604.Attributes;
using Spirit604.CityEditor;
using Spirit604.DotsCity.Core;
using System;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Traffic.Authoring
{
    [CreateAssetMenu(fileName = "TrafficSpawnerConfig", menuName = CityEditorBookmarks.CITY_EDITOR_LEVEL_CONFIG_LEVEL_PATH + "Traffic/TrafficSpawnerConfig")]
    public class TrafficCarSpawnerConfig : ScriptableObject
    {
        [GeneralOption("hasTraffic")]
        [OnValueChanged(nameof(SpawnSettingsUpdated))]
        [Tooltip("Maximum number of cars in the city")]
        [SerializeField][Range(0, 20000)] private int preferableCount = 10;

        [Tooltip("Maximum number of cars relative to the number of available nodes (value * available node count = maximum car) If the value is greater than preferableCount, preferableCount is used. If value is 0 value is not used")]
        [SerializeField][Range(0, 100)] private float maxCarsPerNode;

        [Tooltip("Initial capacity of the hashmap that contains the data of the traffic cars")]
        [SerializeField][Range(0, 25000)] private int hashMapCapacity = 10;

        [OnValueChanged(nameof(SpawnSettingsUpdated))]
        [Tooltip("Maximum number of cars that will be spawned in one iteration")]
        [SerializeField][Range(0, 500)] private int maxSpawnCountByIteration = 10;

        [OnValueChanged(nameof(SpawnSettingsUpdated))]
        [Tooltip("Maximum number of parked in the city")]
        [SerializeField][Range(0, 200)] private int maxParkingCarsCount = 20;

        [OnValueChanged(nameof(SpawnSettingsUpdated))]
        [Tooltip("Minimum duration between spawns")]
        [SerializeField][Range(0, 200)] private float minSpawnDelay = 2f;

        [OnValueChanged(nameof(SpawnSettingsUpdated))]
        [Tooltip("Maximum duration between spawns")]
        [SerializeField][Range(0, 200)] private float maxSpawnDelay = 6f;

        [OnValueChanged(nameof(SpawnSettingsUpdated))]
        [Tooltip("Minimum distance for spawning between cars")]
        [SerializeField][Range(0, 20f)] private float minSpawnCarDistance = 6.5f;

        public int PreferableCount { get => preferableCount; set => preferableCount = value; }
        public float MaxCarsPerNode { get => maxCarsPerNode; set => maxCarsPerNode = value; }
        public int HashMapCapacity { get => hashMapCapacity; set => hashMapCapacity = value; }
        public int MaxSpawnCountByIteration { get => maxSpawnCountByIteration; set => maxSpawnCountByIteration = value; }
        public int MaxParkingCarsCount { get => maxParkingCarsCount; set => maxParkingCarsCount = value; }
        public float MinSpawnDelay { get => minSpawnDelay; set => minSpawnDelay = value; }
        public float MaxSpawnDelay { get => maxSpawnDelay; set => maxSpawnDelay = value; }
        public float MinSpawnCarDistance { get => minSpawnCarDistance; set => minSpawnCarDistance = value; }

#if UNITY_EDITOR
        public event Action OnSettingsChanged = delegate { };
#endif

        private void SpawnSettingsUpdated()
        {
#if UNITY_EDITOR
            OnSettingsChanged();
#endif
        }
    }
}

