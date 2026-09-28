namespace Spirit604.Gameplay.Inventory
{
    [System.Serializable]
    public class WeaponData
    {
        public WeaponData(int ammo)
        {
            Ammo = ammo;
        }

        public int Ammo;
    }
}