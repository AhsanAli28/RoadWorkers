using Spirit604.Attributes;
using Spirit604.CityEditor;
using Spirit604.DotsCity.Core;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [CreateAssetMenu(fileName = "PedestrianSpawnerConfig", menuName = CityEditorBookmarks.CITY_EDITOR_LEVEL_CONFIG_LEVEL_PATH + "Pedestrian/PedestrianSpawnerConfig")]
    public class PedestrianSpawnerConfig : ScriptableObjectBase
    {
        private const int MaxPoolSize = 101000;
        private const int MaxPedestrianCount = 1000000;

        [GeneralOption("hasPedestrian")]
        [SerializeField][Range(0, MaxPedestrianCount)] private int minPedestrianCount = 1000;

        [Tooltip("Maximum number of pedestrians relative to the number of available nodes (value * number of available nodes = maximum pedestrian count). If the value is greater than minPedestrianCount, minPedestrianCount is used. If value is 0, value is not used")]
        [SerializeField][Range(0, 100)] private float maxPedestrianPerNode;

        [HideIf(nameof(HideHybrid))]
        [Tooltip("Pool size of hybrid pedestrian skin")]
        [SerializeField][Range(0, MaxPoolSize)] private int poolSize = 500;

        [Tooltip("Ragdoll skin pool size for each pedestrian skin")]
        [HideIf(nameof(HideRagdoll))]
        [SerializeField][Range(0, 100)] private int ragdollPoolSize = 25;

        [SerializeField][MinMaxSlider(0.0f, 20.0f)] private Vector2 minMaxSpawnDelay = new Vector2(2f, 6f);

        public int MinPedestrianCount { get => minPedestrianCount; set => minPedestrianCount = value; }
        public float MaxPedestrianPerNode { get => maxPedestrianPerNode; }
        public float MinSpawnDelay { get => minMaxSpawnDelay.x; }
        public float MaxSpawnDelay { get => minMaxSpawnDelay.y; }
        public int PoolSize { get => poolSize; }
        public int RagdollPoolSize { get => ragdollPoolSize; }
        public bool HideHybrid { get; set; }
        public bool HideRagdoll { get; set; }
    }
}
