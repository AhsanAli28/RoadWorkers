using Spirit604.Attributes;
using Spirit604.DotsCity.Core.Sound;
using Spirit604.Gameplay.Npc;
using UnityEngine;

namespace Spirit604.Gameplay.Factory.Npc
{
    public class PoliceMonoNpcFactory : NpcMonoFactoryBase, INpcFactory
    {
        [InjectWrapper]
        public override void Construct(
            WeaponFactory weaponFactory,
            IBulletFactory bulletFactory,
            ISoundPlayer soundPlayer)
        {
            base.Construct(weaponFactory, bulletFactory, soundPlayer);
        }

        protected override void Initialize(NpcBehaviourBase npc)
        {
            base.Initialize(npc);
        }

        GameObject INpcFactory.Get(string npcId, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            var npc = base.Get(npcId, spawnPosition, spawnRotation);

            if (npc)
            {
                return npc.gameObject;
            }

            return null;
        }
    }
}