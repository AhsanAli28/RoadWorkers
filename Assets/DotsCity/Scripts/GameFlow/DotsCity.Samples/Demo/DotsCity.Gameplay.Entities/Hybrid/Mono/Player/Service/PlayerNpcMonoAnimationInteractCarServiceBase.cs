using Spirit604.DotsCity.Core;
using Spirit604.DotsCity.Gameplay.Player;
using Spirit604.Gameplay.Car;
using Spirit604.Gameplay.Npc;
using System;
using System.Collections;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Factory.Player
{
    public class PlayerNpcMonoAnimationInteractCarServiceBase : NpcMonoInteractCarServiceBase
    {
        private const float Distance = 0.1f;
        private const float EnteringSpeed = 2f;

        protected override bool Immediate => false;

        private readonly PlayerActorTracker playerActorTracker;

        public PlayerNpcMonoAnimationInteractCarServiceBase(PlayerActorTracker playerActorTracker)
        {
            this.playerActorTracker = playerActorTracker;
        }

        protected override IEnumerator EnteringAnimationRoutine(CarSlot sourceSlot, INpcInCar newNpc)
        {
            var targetPos = sourceSlot.transform.position;

            while (true)
            {
                var newPos = Vector3.Lerp(newNpc.Actor.transform.position, targetPos, Time.deltaTime * EnteringSpeed);

                newNpc.Actor.transform.position = newPos;

                var distance = Vector3.Distance(newPos, targetPos);

                if (distance < Distance)
                    yield break;
                else
                    yield return null;
            }
        }

        protected override IEnumerator ExitingAnimationRoutine(CarSlot sourceSlot, GameObject newNpc, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            while (true)
            {
                var newPos = Vector3.Lerp(newNpc.transform.position, spawnPosition, Time.deltaTime * EnteringSpeed);

                newNpc.transform.position = newPos;

                var distance = Vector3.Distance(newPos, spawnPosition);

                if (distance < Distance)
                    yield break;
                else
                    yield return null;
            }
        }

        protected override void StartEnter(CarSlot sourceSlot, INpcInCar newNpc)
        {
            base.StartEnter(sourceSlot, newNpc);

            if (sourceSlot.Driver)
            {
                if (sourceSlot.CarParent.TryGetComponent<PlayerHybridInputControl>(out var vehicleInput))
                {
                    vehicleInput.SwitchEnabledState(false);
                }
            }
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
            var exitedNpc = base.Exit(sourceSlot, npcId, spawnPosition, spawnRotation, isDriver, onExit);

            if (isDriver)
            {
                playerActorTracker.Actor = exitedNpc;
            }

            return exitedNpc;
        }
    }
}