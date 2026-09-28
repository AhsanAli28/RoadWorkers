using Spirit604.Extensions;
using UnityEngine;

namespace Spirit604.Gameplay.Weapons
{
    public abstract class BulletBehaviourBase : MonoBehaviour
    {
        [SerializeField][Range(0.01f, 50f)] private float speed = 16;
        [SerializeField][Range(0.01f, 30f)] private float lifetime = 2f;
        [SerializeField][Range(1, 100)] private int damage = 1;
        [SerializeField] protected LayerMask castMask;

        private float disableTime;
        private float damageMultiplier;

        public GameObject Owner { get; private set; }

        public int Damage { get => Mathf.RoundToInt(damage * damageMultiplier); set => damage = value; }

        public float DamageMultiplier { get => damageMultiplier; set => damageMultiplier = value; }

        private void FixedUpdate()
        {
            var dist = speed * Time.fixedDeltaTime;
            var dt = transform.forward * dist;

            if (Physics.Raycast(transform.position, transform.forward, out var hit, dist, castMask.value, QueryTriggerInteraction.Ignore))
            {
                if (ProcessHit(hit.collider, hit.point, hit.normal))
                {
                    Destroy();
                }
            }

            Move(dt);
            CheckLifetime();
        }

        public virtual void Initialize(GameObject owner, Vector3 heading, Vector3 spawnPosition, float damageMultiplier = 1f)
        {
            this.Owner = owner;
            this.disableTime = Time.time + lifetime;
            this.damageMultiplier = damageMultiplier;
            transform.SetPositionAndRotation(spawnPosition, Quaternion.LookRotation(heading));
        }

        protected abstract bool ProcessHit(Collider hit, Vector3 point, Vector3 normal);

        protected virtual void Destroy()
        {
            gameObject.ReturnToPool();
        }

        private void Move(Vector3 dt)
        {
            transform.position += dt;
        }

        private void CheckLifetime()
        {
            if (Time.time >= disableTime)
            {
                Destroy();
            }
        }
    }
}