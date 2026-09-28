using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Simulation.Common;
using Spirit604.Gameplay;
using Spirit604.Gameplay.Npc;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Npc.Authoring
{
    internal class PoliceEntityNpcInitilization : EntityNpcInitilizationBase
    {
        private Entity prefabEntity;

        public PoliceEntityNpcInitilization(EntityManager entityManager) : base(entityManager)
        {
            var npcPrefabQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NpcPrefabComponent>(), ComponentType.ReadOnly<PoliceNpcPrefabTag>());
            prefabEntity = npcPrefabQuery.GetSingleton<NpcPrefabComponent>().PrefabEntity;
        }

        public override Entity Spawn(Transform npc)
        {
            var factionType = FactionType.City;

            var entity = EntityManager.Instantiate(prefabEntity);
            EntityManager.SetComponentData(entity, new FactionTypeComponent { Value = factionType });
            EntityManager.SetComponentData(entity, new HealthComponent(npc.GetComponent<HealthBase>().CurrentHealth));
            npc.GetComponent<NpcWeaponHolder>().FactionType = factionType;
            BindTransformToEntity(entity, npc);

            return entity;
        }
    }
}