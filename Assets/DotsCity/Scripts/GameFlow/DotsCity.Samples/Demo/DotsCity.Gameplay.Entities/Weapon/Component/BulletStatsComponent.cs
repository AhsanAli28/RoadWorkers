using Unity.Entities;

namespace Spirit604.DotsCity.Gameplay.Weapon
{
    public struct BulletStatsComponent : IComponentData
    {
        public float FlySpeed;
        public float LifeTime;
        public int Damage;
    }
}