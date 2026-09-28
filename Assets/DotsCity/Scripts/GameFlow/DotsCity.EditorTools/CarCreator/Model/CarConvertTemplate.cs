using Spirit604.CityEditor;
using Spirit604.Collections.Dictionary;
using Spirit604.DotsCity.Simulation.Factory;
using Spirit604.DotsCity.Simulation.Traffic;
using UnityEngine;

namespace Spirit604.DotsCity.EditorTools
{
    [CreateAssetMenu(fileName = "Car Convert Template", menuName = CityEditorBookmarks.CITY_EDITOR_TRAFFIC_EDITOR_CONFIGS_PATH + "Car Convert Template")]
    public class CarConvertTemplate : ScriptableObject
    {
        [System.Serializable]
        public class CarTemplateDataDictionary : AbstractSerializableDictionary<EntityType, CarPrefabPair> { }

        [System.Serializable]
        public class ControllerTemplateDataDictionary : AbstractSerializableDictionary<ControllerType, CarPrefabPair> { }

        [SerializeField]
        private CarTemplateDataDictionary carTemplateData = new CarTemplateDataDictionary();

        [SerializeField]
        private ControllerTemplateDataDictionary controllerData = new ControllerTemplateDataDictionary();

        public CarTemplateDataDictionary CarTemplates => carTemplateData;
        public ControllerTemplateDataDictionary ControllerData => controllerData;
    }
}