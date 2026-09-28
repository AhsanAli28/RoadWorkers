using Spirit604.Gameplay.Inventory;
using Spirit604.Gameplay.Npc;
using Spirit604.Gameplay.Weapons;
using System;
using System.Collections.Generic;

namespace Spirit604.Gameplay.Player.Session
{
    [Serializable]
    public class CharacterSessionData
    {
        public int CurrentHealth;
        public string NpcId;
        public WeaponType CurrentSelectedWeapon;
        public bool WeaponIsHided;
        public List<ItemBase> Items = new List<ItemBase>();
        [NonSerialized] public NpcBehaviourBase WorldNpcRef;
    }
}
