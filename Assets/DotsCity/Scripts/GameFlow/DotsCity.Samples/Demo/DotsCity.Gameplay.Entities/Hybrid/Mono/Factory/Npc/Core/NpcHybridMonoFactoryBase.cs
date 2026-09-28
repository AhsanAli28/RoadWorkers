using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Npc.Factory
{
    public class NpcHybridMonoFactoryBase : SimpleCustomTypedFactoryBase
    {
        private IEntityWorldService entityWorldService;

        [InjectWrapper]
        public void Construct(IEntityWorldService entityWorldService)
        {
            this.entityWorldService = entityWorldService;
        }

        public virtual GameObject Get(int npcIndex, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            var id = GetName(npcIndex);

            if (string.IsNullOrEmpty(id))
            {
#if UNITY_EDITOR
                UnityEngine.Debug.LogError($"{FactoryName}. Npc '{npcIndex}' index not found.");
#endif

                return null;
            }

            return Get(id, spawnPosition, spawnRotation);
        }

        public virtual GameObject Get(string npcId, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            var npc = base.Get(npcId);

            if (npc == null)
            {
#if UNITY_EDITOR
                UnityEngine.Debug.LogError($"{FactoryName}. Npc '{npcId}' is null.");
#endif
                return null;
            }

            return npc;
        }
    }
}

