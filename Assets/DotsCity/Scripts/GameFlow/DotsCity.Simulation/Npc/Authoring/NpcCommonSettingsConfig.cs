using Spirit604.CityEditor;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Npc.Authoring
{
    [CreateAssetMenu(fileName = "NpcCommonSettingsConfig", menuName = CityEditorBookmarks.CITY_EDITOR_LEVEL_CONFIG_LEVEL_PATH + "Npc/NpcCommonSettingsConfig")]
    public class NpcCommonSettingsConfig : ScriptableObject
    {
        [SerializeField] private bool registerHashmap = true;

        [Tooltip("Initial capacity of the hashmap containing data about the NPC (position, state, etc...)")]
        [SerializeField][Range(0, 200000)] private int npcHashMapCapacity = 300;

        public bool RegisterHashmap => registerHashmap;
        public int NpcHashMapCapacity => npcHashMapCapacity;
    }
}