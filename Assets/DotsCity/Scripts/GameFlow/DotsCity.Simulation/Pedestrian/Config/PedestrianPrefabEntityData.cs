using Spirit604.CityEditor;
using Spirit604.Collections.Dictionary;
using Spirit604.DotsCity.Simulation.Pedestrian.Authoring;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    [CreateAssetMenu(fileName = "PedestrianPrefabEntityData", menuName = CityEditorBookmarks.CITY_EDITOR_LEVEL_CONFIG_LEVEL_PATH + "Pedestrian/PedestrianPrefabEntityData")]
    public class PedestrianPrefabEntityData : ScriptableObject
    {
        [System.Serializable]
        public class PrefabList
        {
            public List<PedestrianAuthoring> List = new List<PedestrianAuthoring>();
        }

        [System.Serializable]
        public class PedestrianEntityPrefabDataDictionary : AbstractSerializableDictionary<string, PrefabList> { }

        [SerializeField] private PedestrianEntityPrefabDataDictionary pedestrianEntityPrefabData;

        public PedestrianEntityPrefabDataDictionary PedestrianEntityPrefabData => pedestrianEntityPrefabData;
    }
}
