using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Common;
using Spirit604.DotsCity.Simulation.Npc;
using Spirit604.DotsCity.Simulation.Npc.Navigation;
using Spirit604.Gameplay.Npc;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Npc.Authoring
{
    internal class PlayerNpcInitilization : EntityNpcInitilizationBase
    {
        private Entity prefabEntity;

        public PlayerNpcInitilization(EntityManager entityManager) : base(entityManager)
        {
            var npcPrefabQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NpcPrefabComponent>(), ComponentType.ReadOnly<PlayerNpcPrefabTag>());
            prefabEntity = npcPrefabQuery.GetSingleton<NpcPrefabComponent>().PrefabEntity;

            EntityManager.TryToRemoveComponent<NpcTargetComponent>(prefabEntity);
            EntityManager.TryToRemoveComponent<NpcNavAgentComponent>(prefabEntity);
            EntityManager.TryToRemoveComponent<NavAgentSteeringComponent>(prefabEntity);
            EntityManager.TryToRemoveComponent<NavAgentComponent>(prefabEntity);
        }

        public override Entity Spawn(Transform npc)
        {
            var entity = EntityManager.Instantiate(prefabEntity);

            EntityManager.SetComponentData(entity, new HealthComponent(999));

            var factionType = FactionType.Player;
            EntityManager.SetComponentData(entity, new FactionTypeComponent { Value = factionType });
            EntityManager.SetComponentData(entity, new NpcTypeComponent { Type = NpcType.Player });

            npc.GetComponent<NpcWeaponHolder>().FactionType = factionType;
            BindTransformToEntity(entity, npc);

            return entity;
        }
    }
}