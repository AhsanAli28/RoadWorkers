using Spirit604.Attributes;
using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Core.Sound;
using Spirit604.Gameplay.Npc;
using UnityEngine;

namespace Spirit604.Gameplay.Factory.Npc
{
    public abstract class NpcMonoFactoryBase : NpcMonoFactoryBase<NpcBehaviourBase> { }

    public abstract class NpcMonoFactoryBase<T> : SimpleStringKeyFactoryBase<T> where T : NpcBehaviourBase
    {
        private WeaponFactory weaponFactory;
        private IBulletFactory bulletFactory;
        private ISoundPlayer soundPlayer;

        [InjectWrapper]
        public virtual void Construct(WeaponFactory weaponFactory, IBulletFactory bulletFactory, ISoundPlayer soundPlayer)
        {
            this.weaponFactory = weaponFactory;
            this.bulletFactory = bulletFactory;
            this.soundPlayer = soundPlayer;
        }

        public virtual T Get(string key, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            var npc = this.Get(key);

            if (npc)
            {
                npc.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            }

            return npc;
        }

        public override T Get(string key)
        {
            var npc = base.Get(key);

            if (npc)
            {
                Initialize(npc);
            }
            else
            {
                var factoryName = name;
                Debug.Log($"{factoryName}. Npc ID '{key}' not found. Make sure you have added the NPC to the {factoryName}.");
            }

            return npc;
        }

        protected virtual void Initialize(T npc)
        {
            npc.WeaponHolder.Initialize(weaponFactory, bulletFactory, soundPlayer);
        }
    }
}