using Spirit604.DotsCity.Core.Sound;
using Spirit604.DotsCity.Hybrid.VFX;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.VFX;
using Spirit604.Gameplay;
using Spirit604.Gameplay.Car;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Car
{
    public class CarVfxBehaviour : MonoBehaviour, IHitReaction
    {
        [Header("Physics Settings")]

        [SerializeField] private float applyForceOffset = -1.6f;
        [SerializeField] private float initialYForce = 8;
        [SerializeField] private float initialForwardForce = 5;

        [Header("VFX")]

        [SerializeField] private bool addSmoke = true;
        [SerializeField] private float smokeOffset = 2;

        [Header("Sound")]

        [SerializeField] private SoundData hitSound;
        [SerializeField] private SoundData explodeSound;

        private Rigidbody rb;
        private HealthBaseWithDelay healthBaseWithDelay;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            healthBaseWithDelay = GetComponent<HealthBaseWithDelay>();
        }

        public void HandleHitReaction(Vector3 hitPosition, Vector3 hitDirection)
        {
            SoundManager.Instance.PlayOneShot(hitSound, transform.position);
            BulletHitReactionUtils.CreateBulletVfx(VFXFactory.Instance, transform.position, hitDirection);
        }

        public void ActivateDeathVFX()
        {
            SoundManager.Instance.PlayOneShot(explodeSound, transform.position);
            AddForce();
            AddVfx();
        }

        private void AddForce()
        {
            if (rb == null)
                return;

            var forward = transform.forward;
            var offset = forward * applyForceOffset;

            var explosionPosition = transform.position + offset;

            var impulse = rb.mass * new Vector3(0, initialYForce, 0);

            var impulse2 = rb.mass * (forward * initialForwardForce);

            rb.AddForceAtPosition(impulse, explosionPosition, ForceMode.Impulse);
            rb.AddForceAtPosition(impulse2, transform.position, ForceMode.Impulse);
        }

        private void AddVfx()
        {
            if (!addSmoke || !VFXFactory.Instance)
                return;

            var explosionVFX = VFXFactory.Instance.GetVFX(VFXType.DefaultCarExplosion);
            var smokeVFX = VFXFactory.Instance.GetVFX(VFXType.DefaultCarSmoke);

            explosionVFX.GetComponent<VFXBehaviour>().Play(transform.position);
            smokeVFX.GetComponent<VFXBehaviour>().Play(transform, healthBaseWithDelay.HideTime - 0.1f, new Vector3(0, smokeOffset, 0));
        }
    }
}