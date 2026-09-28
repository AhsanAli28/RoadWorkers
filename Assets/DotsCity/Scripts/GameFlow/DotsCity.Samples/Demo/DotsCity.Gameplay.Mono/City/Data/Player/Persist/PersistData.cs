using Spirit604.CityEditor;
using Spirit604.Extensions;
using Spirit604.Gameplay.Npc;
using Spirit604.Gameplay.Weapons;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.Gameplay.Config.Common
{
    [CreateAssetMenu(fileName = "PersistData", menuName = CityEditorBookmarks.CITY_EDITOR_ROOT_PATH + "Game Data/Persist Data")]
    public class PersistData : ScriptableObject
    {
        [SerializeField] private List<NpcId> availableCharacters = new List<NpcId>();
        [SerializeField] private List<WeaponType> availableWeapons = new List<WeaponType>();
        [SerializeField] private List<NpcId> selectedCharacters = new List<NpcId>();
        [SerializeField] private List<WeaponType> selectedWeapons = new List<WeaponType>();
        [SerializeField] private int moneyValue;
        [SerializeField] private bool isInitialzed;

        public List<NpcId> AvailableCharacters { get => availableCharacters; set => availableCharacters = value; }
        public List<WeaponType> AvailableWeapons { get => availableWeapons; set => availableWeapons = value; }
        public List<NpcId> SelectedCharacters { get => selectedCharacters; set => selectedCharacters = value; }
        public List<WeaponType> SelectedWeapons { get => selectedWeapons; set => selectedWeapons = value; }
        public int MoneyValue { get => moneyValue; set => moneyValue = value; }

        public void CheckForInitilization(GameConfig gameConfig)
        {
            if (!isInitialzed)
            {
                isInitialzed = true;
                MoneyValue = gameConfig.StartMoneyValue;
            }

            for (int i = 0; i < gameConfig.DefaultAvailableCharacters.Count; i++)
            {
                availableCharacters.TryToAdd(gameConfig.DefaultAvailableCharacters[i]);
            }
            for (int i = 0; i < gameConfig.DefaultAvailableWeapons.Count; i++)
            {
                availableWeapons.TryToAdd(gameConfig.DefaultAvailableWeapons[i]);
            }
        }

        public void OpenCharacter(NpcId characterType)
        {
            availableCharacters.TryToAdd(characterType);
        }

        public void OpenWeapon(WeaponType weaponType)
        {
            availableWeapons.TryToAdd(weaponType);
        }

        public void ChangeMoney(int diffValue)
        {
            MoneyValue += diffValue;
        }
    }
}