using Spirit604.Collections.Dictionary;
using Spirit604.Gameplay.Inventory;
using Spirit604.Gameplay.Npc;
using Spirit604.Gameplay.Weapons;
using System;

namespace Spirit604.Gameplay.Player
{
    [Serializable]
    public class BandCharacterSpawnData
    {
        [Serializable]
        public class WeaponDataDictionary : AbstractSerializableDictionary<WeaponType, WeaponData> { }

        public bool IsPlayer;
        public int CurrentHealth;
        public NpcId NpcId;
        public WeaponType CurrentSelectedWeapon;
        public WeaponDataDictionary WeaponData;

        public string NpcIdValue => NpcId.ToString();
    }
}