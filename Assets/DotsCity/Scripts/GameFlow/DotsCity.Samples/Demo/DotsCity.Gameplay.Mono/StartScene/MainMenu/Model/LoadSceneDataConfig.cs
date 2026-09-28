using Spirit604.Attributes;
using Spirit604.CityEditor;
using Spirit604.Collections.Dictionary;
using UnityEngine;

namespace Spirit604.Gameplay.Config
{
    [CreateAssetMenu(fileName = "LoadSceneDataConfig", menuName = CityEditorBookmarks.CITY_EDITOR_LEVEL_EDITOR_CONFIG_OTHER_PATH + "LoadSceneDataConfig")]
    public class LoadSceneDataConfig : ScriptableObject
    {
        [System.Serializable]
        public class LoadSceneDataConfigDictionary : AbstractSerializableDictionary<string, ScriptableObject> { }

        [Scene]
        [SerializeField] private string sceneName;
        [SerializeField] private LoadSceneDataConfigDictionary configDataDictionary;

        public string SceneName { get => sceneName; }

        public LoadSceneDataConfigDictionary ConfigData { get => configDataDictionary; }
    }

    public static class LoadSceneDataExtension
    {
        public static ScriptableObject GetConfig(this LoadSceneDataConfig.LoadSceneDataConfigDictionary configDataDictionary, string key, bool isMobile)
        {
            if (configDataDictionary[key] is ConfigInjectorData)
            {
                var data = configDataDictionary[key] as ConfigInjectorData;
                return data.GetConfig(isMobile);
            }

            return configDataDictionary[key];
        }
    }
}