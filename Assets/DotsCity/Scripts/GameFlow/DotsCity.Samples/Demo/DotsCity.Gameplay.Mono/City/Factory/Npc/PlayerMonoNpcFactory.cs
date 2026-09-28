using Spirit604.Attributes;
using Spirit604.DotsCity.Core.Sound;
using Spirit604.Gameplay.InputService;
using Spirit604.Gameplay.Npc;
using Spirit604.Gameplay.Player;
using UnityEngine;

namespace Spirit604.Gameplay.Factory.Npc
{
    public class PlayerMonoNpcFactory : NpcMonoFactoryBase, IPlayerNpcFactory
    {
        private IMotionInput motionInput;
        private IShootTargetProvider shootTargetProvider;

        [InjectWrapper]
        public void Construct(
            IMotionInput motionInput,
            IShootTargetProvider shootTargetProvider,
            WeaponFactory weaponFactory,
            IBulletFactory bulletFactory,
            ISoundPlayer soundPlayer)
        {
            base.Construct(weaponFactory, bulletFactory, soundPlayer);

            this.motionInput = motionInput;
            this.shootTargetProvider = shootTargetProvider;
        }

        protected override void Initialize(NpcBehaviourBase npc)
        {
            base.Initialize(npc);
            var motion = npc.GetComponent<PlayerNpcInputBehaviour>();
            motion.Initialize(motionInput, shootTargetProvider);
        }

        GameObject INpcFactory.Get(string npcId, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            var npc = base.Get(npcId, spawnPosition, spawnRotation);

            if (npc)
            {
                npc.WeaponHolder.FactionType = DotsCity.Core.FactionType.Player;
                return npc.gameObject;
            }

            return null;
        }
    }
}