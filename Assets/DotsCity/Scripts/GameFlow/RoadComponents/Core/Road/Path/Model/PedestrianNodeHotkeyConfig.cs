using Spirit604.CityEditor;
using UnityEngine;

namespace Spirit604.Gameplay.Config.Pedestrian
{
    [CreateAssetMenu(fileName = "PedestrianNodeHotkeyConfig", menuName = CityEditorBookmarks.CITY_EDITOR_LEVEL_CONFIG_HOTKEY_PATH + "PedestrianNodeHotkeyConfig")]
    public class PedestrianNodeHotkeyConfig : ScriptableObject
    {
        [Header("Pedestrian Node Settings")]
        public KeyCode ConnectButton = KeyCode.Tab;

        [Header("Pedestrian Node Creator Settings")]
        public KeyCode CreateButton = KeyCode.Tab;
        public KeyCode UnselectButton = KeyCode.Escape;
        public KeyCode SpawnOrConnectButton = KeyCode.E;
        public KeyCode SelectNodeButton = KeyCode.W;
    }
}
