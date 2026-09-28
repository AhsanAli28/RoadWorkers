using Spirit604.Attributes;
using Spirit604.CityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [CreateAssetMenu(fileName = "PedestrianTestAnimationSpawnerConfig", menuName = CityEditorBookmarks.CITY_EDITOR_LEVEL_CONFIG_LEVEL_PATH + "Pedestrian/PedestrianTestAnimationSpawnerConfig")]
    public class PedestrianTestAnimationSpawnerConfig : ScriptableObjectBase
    {
        [SerializeField] private PedestrianAnimationTestSpawner.NpcRigType npcRigType;

        [ShowIf(nameof(HybridSkin))]
        [SerializeField][Range(0, 10000)] private int spawnCountHybrid = 1000;

        [ShowIf(nameof(GPUSkin))]
        [SerializeField][Range(0, 200000)] private int spawnCountGPU = 100000;

        [ShowIf(nameof(HybridSkin))]
        [SerializeField][Range(0, 10000)] private int spawnCountMaxHybrid = 10000;

        [ShowIf(nameof(GPUSkin))]
        [SerializeField][Range(0, 200000)] private int spawnCountMaxGPU = 200000;

        [SerializeField] private bool randomizeSkin = true;

        [SerializeField][Range(1, 1000)] private int objectInRowCount = 30;

        [SerializeField][Range(0.02f, 10)] private float spawnOffset = 1;

        [SerializeField] private PedestrianAnimationTestSpawner.PedestrianAnimationType animationType;

        public PedestrianAnimationTestSpawner.NpcRigType NpcRigType { get => npcRigType; set => npcRigType = value; }

        public int SpawnCount => spawnCountHybrid;

        public int SpawnCountGPU => spawnCountGPU;

        public bool RandomizeSkin { get => randomizeSkin; set => randomizeSkin = value; }

        public int ObjectInRowCount => objectInRowCount;

        public float SpawnOffset => spawnOffset;

        public PedestrianAnimationTestSpawner.PedestrianAnimationType AnimationType => animationType;

        public int SpawnCountMaxHybrid { get => spawnCountMaxHybrid; set => spawnCountMaxHybrid = value; }

        public int SpawnCountMaxGPU { get => spawnCountMaxGPU; set => spawnCountMaxGPU = value; }

        private bool HybridSkin => npcRigType == PedestrianAnimationTestSpawner.NpcRigType.HybridLegacy;

        private bool GPUSkin => npcRigType == PedestrianAnimationTestSpawner.NpcRigType.PureGPU;
    }
}
