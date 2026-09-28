using Spirit604.Attributes;
using System;
using System.Collections;
using UnityEngine;

namespace Spirit604.Gameplay
{
    public abstract class HealthBaseWithDelay : HealthBase
    {
        [SerializeField] private bool autoHide = true;

        [ShowIf(nameof(autoHide))]
        [SerializeField][Range(0f, 100f)] private float hideTime = 3f;

        public float HideTime { get => hideTime; protected set => hideTime = value; }

        protected override void ActivateDeathVFX()
        {
            if (autoHide)
                StartCoroutine(DeathDelay());
        }

        protected override void PostDeathAction()
        {
            Death();
            ActivateDeathVFX();
        }

        private IEnumerator DeathDelay()
        {
            yield return new WaitForSeconds(hideTime);
            DeathVfxFinished();
        }
    }
}