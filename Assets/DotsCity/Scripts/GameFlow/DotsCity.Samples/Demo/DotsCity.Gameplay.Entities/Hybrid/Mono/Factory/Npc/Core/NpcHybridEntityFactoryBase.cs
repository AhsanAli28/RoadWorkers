using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Core.Sound;
using Spirit604.DotsCity.Gameplay.Npc.Authoring;
using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.Gameplay.Factory;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Npc.Factory
{
    public class NpcHybridEntityFactoryBase : SimpleStringKeyFactoryBase<NpcBehaviourEntity>, INpcFactory
    {
        private HashSet<INpcEntity> npcEntities = new HashSet<INpcEntity>();

        private NpcEntityFactory npcEntityFactory;
        private WeaponFactory weaponFactory;
        private IBulletFactory bulletEntityFactory;
        private ISoundPlayer soundPlayer;
        private IEntityWorldService entityWorldService;

        [InjectWrapper]
        public void Construct(IEntityWorldService entityWorldService, NpcEntityFactory npcEntityFactory, WeaponFactory weaponFactory, IBulletFactory bulletEntityFactory, ISoundPlayer soundPlayer)
        {
            this.npcEntityFactory = npcEntityFactory;
            this.weaponFactory = weaponFactory;
            this.bulletEntityFactory = bulletEntityFactory;
            this.soundPlayer = soundPlayer;
            this.entityWorldService = entityWorldService;

            entityWorldService.OnEntitySceneUnload += EntityWorldService_OnEntitySceneUnload;
        }

        public virtual GameObject Get(string npcId, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            var npc = base.Get(npcId);
            var npcEntity = npc.GetComponent<NpcBehaviourEntity>();

            var relatedEntity = npcEntityFactory.Spawn(npcEntity.NpcShapeType, npcEntity.transform, spawnPosition, spawnRotation);
            npcEntity.RelatedEntity = relatedEntity;
            npcEntity.WeaponHolder.Initialize(weaponFactory, bulletEntityFactory, soundPlayer);
            npcEntity.OnDisableCallback += NpcEntity_OnDisable;
            npcEntities.Add(npcEntity);

            return npc.gameObject;
        }

        private void NpcEntity_OnDisable(INpcEntity npcEntity)
        {
            npcEntity.OnDisableCallback -= NpcEntity_OnDisable;

            if (npcEntities.Contains(npcEntity))
            {
                npcEntities.Remove(npcEntity);
            }
        }

        private void EntityWorldService_OnEntitySceneUnload()
        {
            foreach (var npcEntity in npcEntities)
            {
                npcEntity.DestroyEntity();
            }

            npcEntities.Clear();
        }
    }
}

