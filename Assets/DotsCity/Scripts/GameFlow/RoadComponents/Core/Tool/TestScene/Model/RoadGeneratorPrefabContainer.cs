using Spirit604.Collections.Dictionary;
using UnityEngine;

namespace Spirit604.CityEditor.Road.Tests
{
    [CreateAssetMenu(fileName = "RoadGeneratorPrefabContainer", menuName = CityEditorBookmarks.CITY_EDITOR_LEVEL_CONFIG_EDITOR_ROAD_PATH + "RoadGeneratorPrefabContainer")]
    public class RoadGeneratorPrefabContainer : ScriptableObject
    {
        [System.Serializable]
        public class GeneratorPrefabDictionary : AbstractSerializableDictionary<SegmentType, RoadGeneratorPrefab> { }

        [SerializeField] private float straightRoadOffset = 4.07872f;
        [SerializeField] private string lightParentName = "Lights";
        [SerializeField] private GeneratorPrefabDictionary generatorPrefabDictionary;

        public float StraightRoadOffset => straightRoadOffset;
        public string LightParentName => lightParentName;
        public GeneratorPrefabDictionary GeneratorPrefabData => generatorPrefabDictionary;
    }
}
