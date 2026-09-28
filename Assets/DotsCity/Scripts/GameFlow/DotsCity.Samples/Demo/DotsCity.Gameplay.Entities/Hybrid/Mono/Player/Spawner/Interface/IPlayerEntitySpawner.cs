using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player.Spawn
{
    public interface IPlayerEntitySpawner
    {
        void Initialize();
        GameObject Spawn(ScriptableObject spawnConfig, Vector3 spawnPosition, Quaternion spawnRotation);
    }
}
