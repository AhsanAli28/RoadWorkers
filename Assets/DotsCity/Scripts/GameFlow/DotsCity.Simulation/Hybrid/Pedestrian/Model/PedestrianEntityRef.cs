using Spirit604.DotsCity.Hybrid.Core;
using Spirit604.Extensions;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Pedestrian
{
    public class PedestrianEntityRef : NpcHybridEntityRef
    {
        [SerializeField] private Transform cachedTransform;
        [SerializeField] private Animator animator;

        private INpcHitReaction npcHitReaction;

        public Transform Transform => cachedTransform;
        public Animator Animator => animator;

        private void Awake()
        {
            npcHitReaction = GetComponent<INpcHitReaction>();

            if (npcHitReaction != null)
                npcHitReaction.OnDeathEffectFinished += NpcHitReaction_OnDeathEffectFinished;
        }

        public void InitReferences()
        {
            cachedTransform = GetComponent<Transform>();
            animator = GetComponentInChildren<Animator>();
            EditorSaver.SetObjectDirty(this);
        }

        public void Reset()
        {
            InitReferences();
        }

        private void NpcHitReaction_OnDeathEffectFinished()
        {
            DestroyEntity();
        }
    }
}