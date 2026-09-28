using Spirit604.Gameplay.Weapons;

namespace Spirit604.Gameplay.Inventory
{
    [System.Serializable]
    public class WeaponItem : ItemBase
    {
        public WeaponType WeaponType;
        public WeaponData WeaponData;

        public WeaponItem(WeaponType weaponType, WeaponData weaponData)
        {
            WeaponType = weaponType;
            WeaponData = weaponData;
        }
    }
}