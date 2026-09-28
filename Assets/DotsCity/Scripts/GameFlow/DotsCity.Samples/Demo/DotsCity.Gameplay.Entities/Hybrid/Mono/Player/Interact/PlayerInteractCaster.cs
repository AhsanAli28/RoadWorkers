using System;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay
{
    public class PlayerInteractCasterBase : MonoBehaviour, IPlayerInteractCaster
    {
        [SerializeField] private LayerMask raycastLayer;
        [SerializeField] private float castDistance = 1f;
        [SerializeField] private float castFrequncy = 0.1f;

        private float nextCastTime;

        public GameObject Interactable { get; private set; }

        public event Action<GameObject> OnCastStateChanged = delegate { };

        protected virtual void FixedUpdate()
        {
            CastRay();
        }

        public void SwitchEnabledState(bool enabled)
        {
            this.enabled = enabled;
            Interactable = null;
        }

        /// <summary>
        /// The method for checking the raycasted object is an interactable & available.
        /// </summary>
        protected virtual bool IsAvailable(GameObject obj)
        {
            return obj != null;
        }

        protected virtual GameObject GetRootFromCollider(GameObject colliderObj)
        {
            return colliderObj;
        }

        protected virtual Vector3 GetCastOrigin() => transform.position + new Vector3(0, 0.5f);

        protected virtual Vector3 GetCastDirection() => transform.forward;

        private void CastRay()
        {
            if (Time.time < nextCastTime) return;

            nextCastTime = Time.time + castFrequncy;
            Vector3 origin = GetCastOrigin();
            bool resetTarget = true;

            if (Physics.Raycast(origin, GetCastDirection(), out var hit, castDistance, raycastLayer, QueryTriggerInteraction.Ignore))
            {
                if (IsAvailable(hit.collider.gameObject))
                {
                    var newObject = GetRootFromCollider(hit.collider.gameObject);

                    if (newObject != null && newObject != Interactable)
                    {
                        resetTarget = false;
                        Interactable = newObject;
                        OnCastStateChanged(Interactable);
                    }
                    else if (newObject == Interactable)
                    {
                        resetTarget = false;
                    }
                }
            }

            if (resetTarget && Interactable != null)
            {
                Interactable = null;
                OnCastStateChanged(Interactable);
            }
        }
    }
}