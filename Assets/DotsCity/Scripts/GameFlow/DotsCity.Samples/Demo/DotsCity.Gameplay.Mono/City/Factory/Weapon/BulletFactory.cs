using Spirit604.DotsCity.Core;
using Spirit604.Gameplay.Weapons;
using UnityEngine;

namespace Spirit604.Gameplay.Factory
{
    public class BulletFactory : SimpleEnumKeyFactoryBase<BulletType, BulletBehaviourBase>, IBulletFactory
    {
        public void SpawnBullet(GameObject owner, Vector3 heading, Vector3 spawnPosition, int bulletType, float damageMultiplier = 1f)
        {
            var bullet = Get((BulletType)bulletType);
            bullet.Initialize(owner, heading, spawnPosition, damageMultiplier);
        }
    }
}
