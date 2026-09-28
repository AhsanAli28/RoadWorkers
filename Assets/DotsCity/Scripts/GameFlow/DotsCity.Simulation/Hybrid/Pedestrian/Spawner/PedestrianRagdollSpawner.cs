using Spirit604.Attributes;
using Spirit604.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public class PedestrianRagdollSpawner : MonoBehaviour
    {
        public enum FinishType
        {
            Destroy,
            DisablePhysics
        }

        [SerializeField][Range(1f, 20f)] private float ragdollEnabledDuration = 5;
        [SerializeField] private FinishType finishType;

        [Tooltip("Raise OnActivate ragdoll event")]
        [SerializeField] private bool raiseEvent;

        [Tooltip("The current animation of the pedestrian after death will be synced to the spawned ragdoll. Make sure Ragdoll has an Animator with appropriate animations")]
        [SerializeField] private bool syncAnimation;

        private List<PedestrianRagdoll> activatedRagdolls = new();
        private Dictionary<PedestrianRagdoll, float> deactivateTimers = new();
        private Coroutine updateCoroutine = null;

        private IPedestrianRagdollFactory pedestrianRagdollFactory;

        private EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

        public Action<PedestrianRagdoll> OnActivate = delegate { };

        [InjectWrapper]
        public void Construct(IPedestrianRagdollFactory pedestrianRagdollFactory)
        {
            this.pedestrianRagdollFactory = pedestrianRagdollFactory;
        }

        public void SpawnRagdoll(in ActivateRagdollData activateRagdollData)
        {
            var ragdoll = pedestrianRagdollFactory.SpawnRagdoll(activateRagdollData.SkinIndex);

            if (ragdoll == null)
            {
                Debug.LogError($"PedestrianRagdollSpawner. Ragdoll not found. SkinIndex {activateRagdollData.SkinIndex}");
                return;
            }

            ragdoll.transform.position = activateRagdollData.Position;
            ragdoll.transform.rotation = activateRagdollData.Rotation;
            ragdoll.RagdollEnabledDuration = ragdollEnabledDuration;
            ragdoll.OnLifeCycleExpired += PedestrianRagdoll_OnLifeCycleExpired;
            ragdoll.DamageDealerID = activateRagdollData.DamageDealerID;
            AddRagdoll(ragdoll);

            var forceMultiplier = Mathf.Clamp(activateRagdollData.ForceMultiplier, 1f, 5f);

            ragdoll.SwitchActiveState(true, activateRagdollData.ForceDirection, forceMultiplier);

            SyncAnimation(ragdoll, in activateRagdollData);

            if (raiseEvent)
            {
                OnActivate(ragdoll);
            }

            if (updateCoroutine == null)
            {
                updateCoroutine = StartCoroutine(InternalUpdate());
            }
        }

        private void SyncAnimation(PedestrianRagdoll ragdoll, in ActivateRagdollData activateRagdollData)
        {
            if (!syncAnimation) return;

            var animator = ragdoll.CurrentAnimator;

            if (!animator)
            {
                Debug.LogError($"PedestrianRagdollSpawner. Animator not found for spawned ragdoll. SkinIndex {activateRagdollData.SkinIndex}. Make sure Ragdoll has an Animator with appropriate animations");
                return;
            }

            if (!activateRagdollData.HybridSkin)
            {
                if (activateRagdollData.AnimationHash != 0)
                {
                    ragdoll.SwitchAnimator(true);

                    if (activateRagdollData.ParamType != AnimParamType.None)
                    {
                        switch (activateRagdollData.ParamType)
                        {
                            case AnimParamType.Float:
                                animator.SetFloat(activateRagdollData.ParamHash,
                                    activateRagdollData.ParamValue);
                                break;
                            case AnimParamType.Int:
                                animator.SetInteger(activateRagdollData.ParamHash,
                                    (int)activateRagdollData.ParamValue);
                                break;
                            case AnimParamType.Bool:
                                animator.SetBool(activateRagdollData.ParamHash,
                                    activateRagdollData.ParamValue != 0);
                                break;
                        }
                    }

                    animator.Play(activateRagdollData.AnimationHash, activateRagdollData.StateLayer,
                        activateRagdollData.NormalizedTime);
                    animator.Update(0);

                    // Animator.Update(0f); // doesn't work for some reason, so delay to update animation
                    deactivateTimers.Add(ragdoll, Time.time + 0.05f);
                }
            }
            else
            {
                if (EntityManager.HasComponent<Animator>(activateRagdollData.Entity))
                {
                    var sourceAnimator = EntityManager.GetComponentObject<Animator>(activateRagdollData.Entity);

                    CopyAnimator(sourceAnimator, animator);
                    animator.Update(0);

                    // Animator.Update(0f); // doesn't work for some reason, so delay to update animation
                    deactivateTimers.Add(ragdoll, Time.time + 0.05f);
                }
            }
        }

        private void CopyAnimator(Animator sourceAnimator, Animator targetAnimator)
        {
            int layerIndex = GetDominantLayer(sourceAnimator);

            AnimatorStateInfo stateInfo = sourceAnimator.GetCurrentAnimatorStateInfo(layerIndex);

            float normalizedTime = stateInfo.normalizedTime;

            // Copy all parameters
            for (var index = 0; index < sourceAnimator.parameters.Length; index++)
            {
                var parameter = sourceAnimator.parameters[index];
                if (parameter.type == AnimatorControllerParameterType.Float)
                {
                    targetAnimator.SetFloat(parameter.name, sourceAnimator.GetFloat(parameter.name));
                }
                else if (parameter.type == AnimatorControllerParameterType.Int)
                {
                    targetAnimator.SetInteger(parameter.name, sourceAnimator.GetInteger(parameter.name));
                }
                else if (parameter.type == AnimatorControllerParameterType.Bool)
                {
                    targetAnimator.SetBool(parameter.name, sourceAnimator.GetBool(parameter.name));
                }
                else if (parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    if (sourceAnimator.GetBool(parameter.name))
                    {
                        targetAnimator.SetTrigger(parameter.name);
                    }
                }
            }

            // Play the same state on the target Animator
            targetAnimator.Play(stateInfo.shortNameHash, -1, normalizedTime);
        }

        private static int GetDominantLayer(Animator Animator)
        {
            int dominant_index = 0;
            float dominant_weight = 0f;
            float weight = 0f;
            for (int index = 0; index < Animator.layerCount; index++)
            {
                weight = Animator.GetLayerWeight(index);
                if (weight > dominant_weight)
                {
                    dominant_weight = weight;
                    dominant_index = index;
                }
            }

            return dominant_index;
        }

        private IEnumerator InternalUpdate()
        {
            while (true)
            {
                int index = 0;

                while (index < activatedRagdolls.Count)
                {
                    var ragdoll = activatedRagdolls[index];

                    if (ragdoll.DoUpdate())
                    {
                        if (deactivateTimers.TryGetValue(ragdoll, out var deactivateTime) &&
                            Time.time >= deactivateTime)
                        {
                            ragdoll.SwitchAnimator(false);
                            deactivateTimers.Remove(ragdoll);
                        }

                        index++;
                    }
                    else
                    {
                        if (finishType == FinishType.DisablePhysics)
                            index++;
                    }
                }

                yield return null;
            }
        }

        private void AddRagdoll(PedestrianRagdoll ragdoll)
        {
            activatedRagdolls.TryToAdd(ragdoll);
        }

        private void RemoveRagdoll(PedestrianRagdoll ragdoll)
        {
            activatedRagdolls.TryToRemove(ragdoll);

            if (deactivateTimers.ContainsKey(ragdoll))
            {
                deactivateTimers.Remove(ragdoll);
            }

            if (activatedRagdolls.Count == 0 && updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
                updateCoroutine = null;
            }
        }

        #region Event handlers

        private void PedestrianRagdoll_OnLifeCycleExpired(PedestrianRagdoll ragdoll)
        {
            ragdoll.OnLifeCycleExpired -= PedestrianRagdoll_OnLifeCycleExpired;

            switch (finishType)
            {
                case FinishType.Destroy:
                    RemoveRagdoll(ragdoll);
                    ragdoll.gameObject.ReturnToPool();
                    break;
                case FinishType.DisablePhysics:
                    ragdoll.AutoResetPosition = false;
                    ragdoll.SwitchActiveState(false, default);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion
    }
}