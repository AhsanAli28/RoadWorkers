using Spirit604.CityEditor;
using Spirit604.Gameplay.Npc;
using Spirit604.Gameplay.Weapons;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.Gameplay.Config.Common
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = CityEditorBookmarks.CITY_EDITOR_ROOT_PATH + "Game Data/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [SerializeField] private List<NpcId> defaultAvailableCharacters = new List<NpcId>();
        [SerializeField] private List<WeaponType> defaultAvailableWeapons = new List<WeaponType>();
        [SerializeField] private int startMoneyValue;

        public List<NpcId> DefaultAvailableCharacters { get => defaultAvailableCharacters; set => defaultAvailableCharacters = value; }
        public List<WeaponType> DefaultAvailableWeapons { get => defaultAvailableWeapons; set => defaultAvailableWeapons = value; }
        public int StartMoneyValue { get => startMoneyValue; set => startMoneyValue = value; }
    }
}
