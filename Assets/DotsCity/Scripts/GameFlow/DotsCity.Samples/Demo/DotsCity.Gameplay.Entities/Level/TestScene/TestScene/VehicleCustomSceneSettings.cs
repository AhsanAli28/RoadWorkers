using Spirit604.CityEditor;
using Spirit604.DotsCity.Simulation.Car;
using UnityEngine;

namespace Spirit604.DotsCity.TestScene
{
    [CreateAssetMenu(fileName = "VehicleCustomSceneSettings", menuName = CityEditorBookmarks.CITY_EDITOR_LEVEL_CONFIG_TEST_SCENE_PATH + "VehicleCustomSceneSettings")]
    public class VehicleCustomSceneSettings : ScriptableObject
    {
        [SerializeField]
        private VehicleOwnerType spawnType;

        public VehicleOwnerType SpawnType => spawnType;
    }
}
