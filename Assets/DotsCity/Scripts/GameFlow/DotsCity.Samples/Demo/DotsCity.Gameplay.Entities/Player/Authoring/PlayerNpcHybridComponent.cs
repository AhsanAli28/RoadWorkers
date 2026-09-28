using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.DotsCity.Simulation.Npc;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player
{
    [CreateAssetMenu(menuName = HybridComponentBase.BasePath + "PlayerNpcHybrid")]
    public class PlayerNpcHybridComponent : HybridComponentBase, IRuntimeEntityComponentSetProvider, IRuntimeInitEntity
    {
        ComponentType[] IRuntimeEntityComponentSetProvider.GetComponentSet()
        {
            return new ComponentType[] {
                ComponentType.ReadOnly<NpcTag>(),
                ComponentType.ReadOnly<AliveTag>(),
                ComponentType.ReadOnly<NpcTypeComponent>(),
                ComponentType.ReadOnly<PlayerTag>(),
                ComponentType.ReadOnly<PlayerNpcComponent>(),
            };
        }

        void IRuntimeInitEntity.Initialize(EntityManager entityManager, GameObject root, Entity entity)
        {
            entityManager.SetComponentData(entity, new NpcTypeComponent()
            {
                Type = NpcType.Player
            });

            entityManager.SetComponentData(entity, new PlayerNpcComponent()
            {
                AvailableCarEntityIndex = -1
            });
        }
    }
}
