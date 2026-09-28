using System.IO;
using UnityEditor;

namespace Spirit604.CityEditor
{
    public class CityEditorBookmarks
    {
        #region Root

        private const string ROOT_PATH_SAVE_KEY = "CityEditorRoot";

#if UNITY_EDITOR
        public static string GetRootSaveKey() => Extensions.EditorExtension.GetUniquePrefsKey(ROOT_PATH_SAVE_KEY);
#else
        public static string GetRootSaveKey() => string.Empty;
#endif

        internal static string OBJECT_ROOT_PATH
        {
            get
            {
#if UNITY_EDITOR
                return EditorPrefs.GetString(GetRootSaveKey());
#else
                return string.Empty;
#endif
            }
        }

        public static string PREFAB_ROOT_PATH => GetPath("Prefabs/CityEditor/");
        public static string PREFAB_GAMEFLOW_ROOT_PATH => GetPath("Prefabs/GameFlow/");

#endregion

        #region Prefab paths

        public static string GetPath(string relativePath) => Path.Combine(OBJECT_ROOT_PATH, relativePath);
        public static string CITY_EDITOR_CONFIGS_PATH => PREFAB_ROOT_PATH + "Configs/";
        public static string CITY_EDITOR_COMPONENTS_PATH => PREFAB_ROOT_PATH + "RoadComponents/";
        public static string PATH_PREFAB_PATH => CITY_EDITOR_COMPONENTS_PATH + "Path.prefab";
        public static string TRAFFIC_NODE_PREFAB_PATH => CITY_EDITOR_COMPONENTS_PATH + "TrafficNode.prefab";
        public static string ROAD_SEGMENT_PREFAB_PATH => CITY_EDITOR_COMPONENTS_PATH + "RoadSegment.prefab";
        public static string ROUTE_PREFAB_PATH => CITY_EDITOR_COMPONENTS_PATH + "Route.prefab";
        public static string PEDESTRIAN_NODE_PREFAB_PATH => CITY_EDITOR_COMPONENTS_PATH + "PedestrianNode.prefab";
        public static string PEDESTRIAN_NODE_CREATOR_PREFAB_PATH => CITY_EDITOR_COMPONENTS_PATH + "PedestrianNodeCreator.prefab";
        public static string ROADSEGMENTPLACER_PREFAB_PATH => CITY_EDITOR_COMPONENTS_PATH + "RoadSegmentPlacer.prefab";
        public static string CITY_BASE_PATH => PREFAB_GAMEFLOW_ROOT_PATH + "Level/City/Core/";
        public static string PRESET_PATH => PREFAB_GAMEFLOW_ROOT_PATH + "Level/Presets/";
        public static string TRAFFIC_PRESET_PATH => PRESET_PATH + "Traffic/";
        public static string VEHICLE_TEMPLATE_PATH => CityEditorBookmarks.TRAFFIC_PRESET_PATH + "Templates/";

        #endregion

        #region Unity project context

        public const string WINDOW_ROOT_PATH = "Spirit604/";

        #region Project context menu

        public const string CITY_EDITOR_ROOT_PATH = WINDOW_ROOT_PATH + "City/";
        public const string CITY_EDITOR_LEVEL_CONFIGS_PATH = CITY_EDITOR_ROOT_PATH + "Configs/";

        public const string CITY_EDITOR_LEVE_PRESETS_PATH = CITY_EDITOR_ROOT_PATH + "Level/Presets/";
        public const string CITY_EDITOR_LEVEL_FACTORY_PRESETS_PATH = CITY_EDITOR_LEVE_PRESETS_PATH + "Factory/";
        public const string CITY_EDITOR_LEVEL_CONFIG_LEVEL_PATH = CITY_EDITOR_LEVEL_CONFIGS_PATH + "Level/";
        public const string CITY_EDITOR_LEVEL_CONFIG_COMMON_PATH = CITY_EDITOR_LEVEL_CONFIG_LEVEL_PATH + "Common/";
        public const string CITY_EDITOR_LEVEL_CONFIG_ROAD_PATH = CITY_EDITOR_LEVEL_CONFIG_LEVEL_PATH + "Road/";
        public const string CITY_EDITOR_LEVEL_CONFIG_OTHER_PATH = CITY_EDITOR_LEVEL_CONFIG_LEVEL_PATH + "Other/";
        public const string CITY_EDITOR_LEVEL_CONFIG_TEST_SCENE_PATH = CITY_EDITOR_LEVEL_CONFIG_LEVEL_PATH + "TestScene/";

        #endregion

        #region Project context menu editor

        public const string CITY_EDITOR_LEVEL_EDITOR_CONFIGS_PATH = CITY_EDITOR_ROOT_PATH + "Configs Editor/";
        public const string CITY_EDITOR_LEVEL_CONFIG_EDITOR_LEVEL_PATH = CITY_EDITOR_LEVEL_EDITOR_CONFIGS_PATH + "Level/";
        public const string CITY_EDITOR_LEVEL_CONFIG_EDITOR_ROAD_PATH = CITY_EDITOR_LEVEL_CONFIG_EDITOR_LEVEL_PATH + "Road/";
        public const string CITY_EDITOR_TRAFFIC_EDITOR_CONFIGS_PATH = CITY_EDITOR_LEVEL_EDITOR_CONFIGS_PATH + "Traffic/";
        public const string CITY_EDITOR_LEVEL_EDITOR_CONFIG_OTHER_PATH = CITY_EDITOR_LEVEL_EDITOR_CONFIGS_PATH + "Other/";
        public const string CITY_EDITOR_LEVEL_CONFIG_HOTKEY_PATH = CITY_EDITOR_LEVEL_EDITOR_CONFIGS_PATH + "HotKeys/";

        #endregion

        #region Toolbar bookmarks

        public const string CITY_EDITOR_PATH = WINDOW_ROOT_PATH + "CityEditor/";
        public const string CITY_CREATE_PATH = CITY_EDITOR_PATH + "Create/";
        public const string CITY_WINDOW_PATH = CITY_EDITOR_PATH + "Window/";


        #endregion

        #endregion
    }
}
