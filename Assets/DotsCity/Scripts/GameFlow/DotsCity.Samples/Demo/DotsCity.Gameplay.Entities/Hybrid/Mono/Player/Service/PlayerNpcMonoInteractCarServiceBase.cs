using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Gameplay.Player;
using Spirit604.Gameplay.Car;
using Spirit604.Gameplay.Npc;
using System;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Factory.Player
{
    public class PlayerNpcMonoInteractCarServiceBase : NpcMonoInteractCarServiceBase
    {
        private readonly PlayerActorTracker playerActorTracker;

        public PlayerNpcMonoInteractCarServiceBase(PlayerActorTracker playerActorTracker)
        {
            this.playerActorTracker = playerActorTracker;
        }

        public override INpcInCar Enter(CarSlot sourceSlot, string npcId, GameObject enteredNpc = null, bool driver = false, Action<CarSlot> onEnter = null)
        {
            if (driver)
            {
                playerActorTracker.Actor = sourceSlot.CarParent.gameObject;
            }

            var npc = base.Enter(sourceSlot, npcId, enteredNpc, driver, onEnter);

            return npc;
        }

        public override GameObject Exit(CarSlot sourceSlot, string npcId, Vector3 spawnPosition, Quaternion spawnRotation, bool isDriver, Action<GameObject> onExit = null)
        {
            var exitNpc = base.Exit(sourceSlot, npcId, spawnPosition, spawnRotation, isDriver, onExit);

            if (isDriver)
            {
                playerActorTracker.Actor = exitNpc;
            }

            return exitNpc;
        }
    }
}