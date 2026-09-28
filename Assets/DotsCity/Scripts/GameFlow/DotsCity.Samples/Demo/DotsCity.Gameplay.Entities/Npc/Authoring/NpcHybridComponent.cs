using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.DotsCity.Simulation.Npc;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Npc
{
    [CreateAssetMenu(menuName = HybridComponentBase.BasePath + "NpcHybridComponent")]
    public class NpcHybridComponent : HybridComponentBase, IRuntimeEntityComponentSetProvider, IRuntimeInitEntity
    {
        ComponentType[] IRuntimeEntityComponentSetProvider.GetComponentSet()
        {
            return new ComponentType[] {
                ComponentType.ReadOnly<NpcTag>(),
                ComponentType.ReadOnly<AliveTag>(),
                ComponentType.ReadOnly<NpcTypeComponent>(),
            };
        }

        void IRuntimeInitEntity.Initialize(EntityManager entityManager, GameObject root, Entity entity)
        {
            entityManager.SetComponentData(entity, new NpcTypeComponent()
            {
                Type = NpcType.Npc
            });
        }
    }
}
