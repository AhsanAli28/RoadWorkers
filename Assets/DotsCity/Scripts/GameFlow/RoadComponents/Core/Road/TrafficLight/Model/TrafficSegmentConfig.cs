using Spirit604.CityEditor;
using Spirit604.Gameplay.Road;
using UnityEngine;

namespace Spirit604.Gameplay.Config.Road
{
    [CreateAssetMenu(fileName = "TrafficSegmentConfig", menuName = CityEditorBookmarks.CITY_EDITOR_LEVEL_CONFIG_EDITOR_ROAD_PATH + "Traffic Segment Config")]
    public class TrafficSegmentConfig : ScriptableObject
    {
        [SerializeField]
        private TrafficLightHandler trafficLightHandlerPrefab;

        public TrafficLightHandler TrafficLightHandlerPrefab => trafficLightHandlerPrefab;
    }
}