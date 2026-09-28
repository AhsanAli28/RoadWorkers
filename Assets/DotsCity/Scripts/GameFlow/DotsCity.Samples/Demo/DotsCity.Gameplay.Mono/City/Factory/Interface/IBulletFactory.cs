using UnityEngine;

namespace Spirit604.Gameplay.Factory
{
    public interface IBulletFactory
    {
        void SpawnBullet(GameObject owner, Vector3 heading, Vector3 spawnPosition, int bulletType, float damageMultiplier = 1f);
    }
}