namespace Spirit604.Gameplay.Weapons
{
    public static class WeaponAnimatorID
    {
        public static int GetID(WeaponType weaponType)
        {
            int currentId = -1;

            if (weaponType == WeaponType.Revolver)
            {
                currentId = 0;
            }
            else if (weaponType == WeaponType.TommyGun)
            {
                currentId = 1;
            }

            return currentId;
        }
    }
}