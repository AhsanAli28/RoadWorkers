using Spirit604.Gameplay.Car;
using Spirit604.Gameplay.Factory;
using Spirit604.Gameplay.Factory.Npc;
using Spirit604.Gameplay.Npc;
using System;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Factory.Player
{
    public abstract class NpcInteractDotsCarServiceBase : INpcInteractCarService
    {
        protected readonly INpcInCarFactory npcInCarFactory;
        protected readonly INpcFactory npcFactory;

        public NpcInteractDotsCarServiceBase(INpcInCarFactory npcInCarFactory, INpcFactory npcFactory)
        {
            this.npcInCarFactory = npcInCarFactory;
            this.npcFactory = npcFactory;
        }

        public virtual INpcInCar Enter(CarSlot sourceSlot, string npcId, GameObject enteredNpc = null, bool driver = false, Action<CarSlot> onEnter = null)
        {
            if (!sourceSlot)
                return null;

            INpcInCar newNpc = GetNewNpc(sourceSlot, npcId);

            if (newNpc != null)
            {
                ProcessEnteredNpc(sourceSlot, enteredNpc, onEnter);
            }

            return newNpc;
        }

        public virtual GameObject Exit(CarSlot sourceSlot, string npcId, Vector3 spawnPosition, Quaternion spawnRotation, bool isDriver, Action<GameObject> onExit = null)
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

        protected virtual INpcInCar GetNewNpc(CarSlot sourceSlot, string npcId)
        {
            var newNpc = npcInCarFactory.GetNpc(npcId);

            if (newNpc != null)
            {
                newNpc.Actor.transform.SetParent(sourceSlot.transform);
                newNpc.Actor.transform.localPosition = Vector3.zero;
                newNpc.Actor.transform.rotation = Quaternion.identity;
                newNpc.Initialize(sourceSlot);
                newNpc.SnapHide();
                sourceSlot.NpcInCar = newNpc;
            }

            return newNpc;
        }

        protected virtual void ProcessEnteredNpc(CarSlot sourceSlot, GameObject enteredNpc, Action<CarSlot> onEnter)
        {
            if (enteredNpc != null)
            {
                sourceSlot.EnteredSourceNpc = enteredNpc;
                enteredNpc.transform.SetParent(sourceSlot.transform);
                enteredNpc.gameObject.SetActive(false);

                onEnter?.Invoke(sourceSlot);
            }
        }
    }
}