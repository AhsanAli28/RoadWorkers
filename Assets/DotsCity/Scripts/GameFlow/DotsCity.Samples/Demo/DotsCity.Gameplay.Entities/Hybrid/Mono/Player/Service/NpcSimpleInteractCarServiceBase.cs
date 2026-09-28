using Spirit604.Gameplay;
using Spirit604.Gameplay.Car;
using Spirit604.Gameplay.Factory;
using Spirit604.Gameplay.Npc;
using System;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Factory.Player
{
    public abstract class NpcSimpleInteractCarServiceBase : INpcInteractCarService
    {
        protected readonly INpcFactory npcFactory;

        public NpcSimpleInteractCarServiceBase(INpcFactory npcFactory)
        {
            this.npcFactory = npcFactory;
        }

        public virtual INpcInCar Enter(CarSlot sourceSlot, string npcId, GameObject enteredNpc = null, bool driver = false, Action<CarSlot> onEnter = null)
        {
            if (!sourceSlot)
                return null;

            INpcInCar newNpc = GetNewNpc(sourceSlot, enteredNpc, npcId, onEnter);

            if (newNpc != null)
            {
                ProcessEnteredNpc(sourceSlot, enteredNpc);
            }

            return newNpc;
        }

        public virtual GameObject Exit(CarSlot sourceSlot, string npcId, Vector3 spawnPosition, Quaternion spawnRotation, bool isDriver, Action<GameObject> onExit)
        {
            return Exit(npcFactory, sourceSlot, npcId, spawnPosition, spawnRotation, onExit);
        }

        protected virtual GameObject Exit(INpcFactory npcFactory, CarSlot sourceSlot, string npcId, Vector3 spawnPosition, Quaternion spawnRotation, Action<GameObject> onExit)
        {
            var exitingNpc = sourceSlot.EnteredSourceNpc;

            if (exitingNpc == null)
            {
                exitingNpc = npcFactory.Get(npcId, spawnPosition, spawnRotation);
            }
            else
            {
                exitingNpc.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
                exitingNpc.transform.SetParent(sourceSlot.EnteredSourceNpcParent);
                exitingNpc.gameObject.SetActive(true);
            }

            onExit?.Invoke(exitingNpc);
            return exitingNpc;
        }

        protected virtual INpcInCar GetNewNpc(CarSlot sourceSlot, GameObject enteredNpc, string npcId, Action<CarSlot> onEnter)
        {
            enteredNpc.AddComponent<NpcInCarAnimator>();
            var newNpc = enteredNpc.AddComponent<NpcInCar>();

            EnterSlot(sourceSlot, newNpc, onEnter);

            return newNpc;
        }

        protected virtual void EnterSlot(CarSlot sourceSlot, INpcInCar newNpc, Action<CarSlot> onEnter)
        {
            if (newNpc == null) return;

            newNpc.Actor.transform.SetParent(sourceSlot.transform);
            newNpc.Actor.transform.localPosition = Vector3.zero;
            newNpc.Actor.transform.rotation = Quaternion.identity;
            newNpc.Initialize(sourceSlot);
            newNpc.SnapHide();
            sourceSlot.NpcInCar = newNpc;
            onEnter?.Invoke(sourceSlot);
        }

        protected virtual void ProcessEnteredNpc(CarSlot sourceSlot, GameObject enteredNpc)
        {
            if (enteredNpc != null)
            {
                sourceSlot.EnteredSourceNpc = enteredNpc;
                enteredNpc.transform.SetParent(sourceSlot.transform);
                enteredNpc.gameObject.SetActive(false);
            }
        }
    }
}