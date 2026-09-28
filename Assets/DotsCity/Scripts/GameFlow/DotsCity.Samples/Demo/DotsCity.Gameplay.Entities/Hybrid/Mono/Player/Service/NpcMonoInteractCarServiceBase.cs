using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.Gameplay.Car;
using Spirit604.Gameplay.Factory;
using Spirit604.Gameplay.Npc;
using System;
using System.Collections;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Factory.Player
{
    public class NpcMonoInteractCarServiceBase : INpcInteractCarService
    {
        protected virtual bool Immediate { get; } = true;

        public virtual INpcInCar Enter(CarSlot sourceSlot, string npcId, GameObject enteredNpc = null, bool driver = false, Action<CarSlot> onEnter = null)
        {
            if (!sourceSlot)
                return null;

            INpcInCar newNpc = ModifyNpcAndEnter(sourceSlot, enteredNpc, onEnter);

            return newNpc;
        }

        public virtual GameObject Exit(CarSlot sourceSlot, string npcId, Vector3 spawnPosition, Quaternion spawnRotation, bool isDriver, Action<GameObject> onExit)
        {
            var exitingNpc = sourceSlot.EnteredSourceNpc;
            DestroyComponents(sourceSlot, exitingNpc);
            exitingNpc.transform.SetParent(sourceSlot.EnteredSourceNpcParent);

            if (!Immediate)
            {
                sourceSlot.StartCoroutine(ExitingRoutine(sourceSlot, exitingNpc, spawnPosition, spawnRotation, onExit));
            }
            else
            {
                StartExit(sourceSlot, exitingNpc);
                NpcExited(sourceSlot, exitingNpc, spawnPosition, spawnRotation, onExit);
            }

            return exitingNpc;
        }

        protected virtual INpcInCar ModifyNpcAndEnter(CarSlot sourceSlot, GameObject newNpcGo, Action<CarSlot> onEnter)
        {
            SwitchComponents(sourceSlot.CarParent.gameObject, newNpcGo, false);

            INpcInCar newNpc = AddComponents(newNpcGo);

            sourceSlot.NpcInCar = newNpc;
            sourceSlot.EnteredSourceNpc = newNpcGo;

            if (!Immediate)
            {
                sourceSlot.StartCoroutine(EnteringRoutine(sourceSlot, newNpc, onEnter));
            }
            else
            {
                StartEnter(sourceSlot, newNpc);
                NpcEntered(sourceSlot, newNpc, onEnter);
            }

            return newNpc;
        }

        // Launch animation & disable input
        protected virtual void StartEnter(CarSlot sourceSlot, INpcInCar newNpc)
        {
            newNpc.AutoDisposeWeapon = false;
        }

        protected virtual INpcInCar AddComponents(GameObject newNpcGo)
        {
            newNpcGo.AddComponent<NpcInCarAnimator>();
            var newNpc = newNpcGo.AddComponent<NpcInCar>();
            return newNpc;
        }

        protected virtual void DestroyComponents(CarSlot sourceSlot, GameObject exitingNpc)
        {
            GameObject.Destroy(exitingNpc.GetComponent<NpcInCar>());
            GameObject.Destroy(exitingNpc.GetComponent<NpcInCarAnimator>());
        }

        // Launch animation & disable input
        protected virtual void StartExit(CarSlot sourceSlot, GameObject newNpc)
        {
            newNpc.gameObject.SetActive(true);
        }

        protected virtual void NpcExitedInternal(CarSlot sourceSlot, GameObject exitingNpc, Vector3 spawnPosition, Quaternion spawnRotation, Action<GameObject> onExit)
        {
            exitingNpc.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            SwitchComponents(sourceSlot.CarParent.gameObject, exitingNpc, true);
        }

        // Wait for animation complete
        protected virtual IEnumerator EnteringAnimationRoutine(CarSlot sourceSlot, INpcInCar newNpc)
        {
            yield return null;
        }

        // Wait for animation complete
        protected virtual IEnumerator ExitingAnimationRoutine(CarSlot sourceSlot, GameObject newNpc, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            yield return null;
        }

        protected virtual void NpcEnteredInternal(CarSlot sourceSlot, INpcInCar newNpc)
        {
            newNpc.Actor.transform.SetParent(sourceSlot.transform);
            newNpc.Actor.transform.localPosition = Vector3.zero;
            newNpc.Actor.transform.rotation = Quaternion.identity;

            newNpc.Initialize(sourceSlot);
            newNpc.SnapHide();
        }

        protected virtual void SwitchComponents(GameObject vehicle, GameObject npc, bool isEnabled)
        {
            if (npc.TryGetComponent<HybridEntityRuntimeAuthoring>(out var hybridEntityRuntimeAuthoring))
            {
                hybridEntityRuntimeAuthoring.enabled = isEnabled;
            }

            if (npc.TryGetComponent<CharacterController>(out var characterController))
            {
                characterController.enabled = isEnabled;
            }

            if (npc.TryGetComponent<NpcMotionBehaviourBase>(out var motion))
            {
                motion.enabled = isEnabled;
            }

            if (npc.TryGetComponent<RagdollBase>(out var ragdoll))
            {
                ragdoll.AutoSwitchState = isEnabled;
            }

            if (npc.TryGetComponent<NpcBehaviourBase>(out var behaviour))
            {
                behaviour.enabled = isEnabled;
            }

            if (npc.TryGetComponent<ICustomSlotBehaviour>(out var customSlotBehaviour))
            {
                customSlotBehaviour.SwitchEnteredState(vehicle, !isEnabled);
            }
        }

        private IEnumerator EnteringRoutine(CarSlot sourceSlot, INpcInCar newNpc, Action<CarSlot> onEnter)
        {
            StartEnter(sourceSlot, newNpc);

            yield return sourceSlot.StartCoroutine(EnteringAnimationRoutine(sourceSlot, newNpc));

            NpcEntered(sourceSlot, newNpc, onEnter);
            yield return null;
        }

        private IEnumerator ExitingRoutine(CarSlot sourceSlot, GameObject exitingNpc, Vector3 spawnPosition, Quaternion spawnRotation, Action<GameObject> onExit)
        {
            StartExit(sourceSlot, exitingNpc);

            yield return sourceSlot.StartCoroutine(ExitingAnimationRoutine(sourceSlot, exitingNpc, spawnPosition, spawnRotation));

            NpcExited(sourceSlot, exitingNpc, spawnPosition, spawnRotation, onExit);
            yield return null;
        }

        private void NpcExited(CarSlot sourceSlot, GameObject exitingNpc, Vector3 spawnPosition, Quaternion spawnRotation, Action<GameObject> onExit)
        {
            NpcExitedInternal(sourceSlot, exitingNpc, spawnPosition, spawnRotation, onExit);
            onExit?.Invoke(exitingNpc);
        }

        private void NpcEntered(CarSlot sourceSlot, INpcInCar newNpc, Action<CarSlot> onEnter)
        {
            NpcEnteredInternal(sourceSlot, newNpc);
            onEnter?.Invoke(sourceSlot);
        }
    }
}