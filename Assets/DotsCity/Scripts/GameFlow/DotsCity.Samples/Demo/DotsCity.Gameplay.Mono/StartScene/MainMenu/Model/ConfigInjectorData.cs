using Spirit604.CityEditor;
using UnityEngine;

namespace Spirit604.Gameplay.Config
{
    [CreateAssetMenu(fileName = "ConfigInjectorData", menuName = CityEditorBookmarks.CITY_EDITOR_LEVEL_EDITOR_CONFIG_OTHER_PATH + "ConfigInjectorData")]
    public class ConfigInjectorData : ScriptableObject
    {
        public ScriptableObject ConfigPC;
        public ScriptableObject ConfigMobile;
        public string TargetScript;

        public ScriptableObject GetConfig(bool isMobile) => isMobile ? ConfigMobile : ConfigPC;
    }
}