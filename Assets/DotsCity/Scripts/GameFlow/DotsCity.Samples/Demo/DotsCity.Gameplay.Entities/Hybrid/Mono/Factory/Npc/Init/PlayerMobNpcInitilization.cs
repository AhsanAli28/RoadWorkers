using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Common;
using Spirit604.Gameplay.Npc;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Npc.Authoring
{
    internal class PlayerMobNpcInitilization : EntityNpcInitilizationBase
    {
        private Entity prefabEntity;

        public PlayerMobNpcInitilization(EntityManager entityManager) : base(entityManager)
        {
            var npcPrefabQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NpcPrefabComponent>(), ComponentType.ReadOnly<PlayerMobNpcPrefabTag>());
            prefabEntity = npcPrefabQuery.GetSingleton<NpcPrefabComponent>().PrefabEntity;
        }

        public override Entity Spawn(Transform npc)
        {
            var factionType = FactionType.Player;

            var entity = EntityManager.Instantiate(prefabEntity);
            EntityManager.SetComponentData(entity, new FactionTypeComponent { Value = factionType });

            npc.GetComponent<NpcWeaponHolder>().FactionType = factionType;
            BindTransformToEntity(entity, npc);

            return entity;
        }
    }
}