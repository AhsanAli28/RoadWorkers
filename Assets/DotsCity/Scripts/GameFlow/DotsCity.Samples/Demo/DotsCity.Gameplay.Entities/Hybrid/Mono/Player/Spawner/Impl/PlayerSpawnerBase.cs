using Spirit604.Gameplay.Config.Player;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player.Spawn
{
    public abstract class PlayerSpawnerBase : IPlayerEntitySpawner
    {
        public abstract void Initialize();

        public GameObject Spawn(ScriptableObject playerSpawnDataConfig, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            return Spawn(playerSpawnDataConfig as PlayerSpawnDataConfig, spawnPosition, spawnRotation);
        }

        public abstract GameObject Spawn(PlayerSpawnDataConfig playerSpawnDataConfig, Vector3 spawnPosition, Quaternion spawnRotation);
    }
}