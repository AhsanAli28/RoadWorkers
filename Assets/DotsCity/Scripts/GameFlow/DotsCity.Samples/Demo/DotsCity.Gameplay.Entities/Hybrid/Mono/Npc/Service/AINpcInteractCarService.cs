using Spirit604.DotsCity.Gameplay.Factory.Player;
using Spirit604.Gameplay.Car;
using Spirit604.Gameplay.Factory;
using Spirit604.Gameplay.Factory.Npc;
using Spirit604.Gameplay.Npc;
using System;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Factory
{
    public class AINpcInteractCarService : NpcInteractCarServiceBase
    {
        private const int DefaultNpcHealth = 10;

        public AINpcInteractCarService(INpcInCarFactory npcInCarFactory, INpcFactory npcFactory) : base(npcInCarFactory, npcFactory)
        {
        }

        protected override void ProcessEnteredNpc(CarSlot sourceSlot, GameObject enteredNpc)
        {
            var newNpcBehaviour = sourceSlot.NpcInCarActor.GetComponent<NpcBehaviourBase>();
            bool cloned = false;

            if (enteredNpc)
            {
                base.ProcessEnteredNpc(sourceSlot, enteredNpc);

                if (newNpcBehaviour)
                {
                    newNpcBehaviour.Clone(enteredNpc.GetComponent<NpcBehaviourBase>());
                    cloned = true;
                }
            }

            if (newNpcBehaviour)
            {
                if (!cloned)
                {
                    newNpcBehaviour.SetHealth(DefaultNpcHealth);
                }

                newNpcBehaviour.WeaponHolder.FactionType = Core.FactionType.City;
            }
        }

        public override GameObject Exit(CarSlot sourceSlot, string npcId, Vector3 spawnPosition, Quaternion spawnRotation, bool isDriver, Action<GameObject> onExit = null)
        {
            var exitingNpc = base.Exit(sourceSlot, npcId, spawnPosition, spawnRotation, isDriver, onExit);

            var newNpcBehaviour = exitingNpc.GetComponent<NpcBehaviourBase>();

            if (newNpcBehaviour)
            {
                newNpcBehaviour.Clone(sourceSlot.NpcInCarActor.GetComponent<NpcBehaviourBase>());
                newNpcBehaviour.WeaponHolder.FactionType = Core.FactionType.City;
            }

            return exitingNpc;
        }
    }
}