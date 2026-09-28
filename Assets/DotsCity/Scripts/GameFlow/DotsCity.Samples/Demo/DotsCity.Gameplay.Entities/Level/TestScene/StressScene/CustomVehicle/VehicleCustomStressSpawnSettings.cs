using Spirit604.CityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.TestScene
{
    [CreateAssetMenu(fileName = "CustomStressSpawnSettings", menuName = CityEditorBookmarks.CITY_EDITOR_LEVEL_CONFIG_TEST_SCENE_PATH + "CustomStressSpawnSettings")]
    public class VehicleCustomStressSpawnSettings : ScriptableObject
    {
        [Range(1, 10)]
        [SerializeField]
        private int rows = 10;

        [Range(5, 250)]
        [SerializeField]
        private int countPerRow = 50;

        [Range(0, 10)]
        [SerializeField]
        private float xOffset = 2;

        [Range(0, 10)]
        [SerializeField]
        private float zOffset = 5;

        public int Rows { get => rows; set => rows = value; }
        public int CountPerRow { get => countPerRow; set => countPerRow = value; }
        public float XOffset { get => xOffset; set => xOffset = value; }
        public float ZOffset { get => zOffset; set => zOffset = value; }

        public int Count => rows * countPerRow;
    }
}
