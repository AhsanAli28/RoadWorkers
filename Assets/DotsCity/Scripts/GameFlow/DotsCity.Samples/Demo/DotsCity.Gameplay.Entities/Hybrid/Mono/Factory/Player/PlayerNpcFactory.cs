using Spirit604.Gameplay.Factory.Npc;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Npc.Factory.Player
{
    public class PlayerNpcFactory : NpcHybridEntityFactoryBase, IPlayerNpcFactory
    {
        public override GameObject Get(string npcId, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            return base.Get(npcId, spawnPosition, spawnRotation);
        }
    }
}