using Spirit604.CityEditor;
using Spirit604.Collections.Dictionary;
using Spirit604.Gameplay.Weapons;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Weapon.Authoring
{
    [CreateAssetMenu(fileName = "BulletEntityPrefabContainer", menuName = CityEditorBookmarks.CITY_EDITOR_LEVEL_FACTORY_PRESETS_PATH + "BulletEntityPrefabContainer")]
    public class BulletEntityPrefabContainer : ScriptableObject
    {
        [System.Serializable]
        public class BulletDataDictionary : AbstractSerializableDictionary<BulletType, GameObject>
        {
        }

        [SerializeField] private BulletDataDictionary bulletEntityDataDictionary;

        public BulletDataDictionary BulletEntityDataDictionary { get => bulletEntityDataDictionary; set => bulletEntityDataDictionary = value; }
    }
}