using Spirit604.DotsCity.Core;
using Spirit604.Gameplay.Weapons;
using System;

namespace Spirit604.Gameplay.Factory
{
    public class WeaponFactory : SimpleEnumKeyFactoryBase<WeaponType, Weapon>
    {
        public Weapon GetRandomWeapon()
        {
            WeaponType randomWeaponType = GetRandomWeaponType();

            return Get(randomWeaponType);
        }

        public static WeaponType GetRandomWeaponType()
        {
            Array arrayWeaponTypes = Enum.GetValues(typeof(WeaponType));
            WeaponType randomWeaponType = (WeaponType)arrayWeaponTypes.GetValue(UnityEngine.Random.Range(0, arrayWeaponTypes.Length));

            return randomWeaponType;
        }

        public int GetMaxAmmoWeapon(WeaponType weaponType)
        {
            foreach (var prefab in Prefabs)
            {
                if (prefab.Key == weaponType)
                {
                    return prefab.Value.MaxAmmo;
                }
            }

            return 0;
        }
    }
}
