using UnityEngine;

namespace Spirit604.Gameplay.Factory
{
    public interface INpcFactory
    {
        public GameObject Get(string npcId, Vector3 spawnPosition, Quaternion spawnRotation);
    }
}