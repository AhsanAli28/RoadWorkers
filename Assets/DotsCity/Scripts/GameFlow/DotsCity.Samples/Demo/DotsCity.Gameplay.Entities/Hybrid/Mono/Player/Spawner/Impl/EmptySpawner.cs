using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player.Spawn
{
    public class EmptySpawner : IPlayerEntitySpawner
    {
        public void Initialize()
        {
        }

        public GameObject Spawn(ScriptableObject playerSpawnDataConfig, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            return null;
        }
    }
}
