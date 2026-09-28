using Spirit604.Extensions;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Hybrid.VFX
{
    public class VFXBehaviour : MonoBehaviour
    {
        public enum TrackType { Entity, Transform }

        private ParticleSystem vfx;

        private EntityManager entityManager;
        private Entity EntityToTrack;
        private float destroyTime;
        private Vector3 relativeOffset;
        private TrackType trackType;
        private bool hasEntity;
        private Transform transformToTrack;

        private bool OutOfTime { get => destroyTime != 0 && Time.time > destroyTime; }

        private void Awake()
        {
            vfx = GetComponent<ParticleSystem>();
        }

        private void OnEnable()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        private void OnDisable()
        {
            if (vfx != null)
            {
                vfx.Stop();
                destroyTime = 0;
                hasEntity = false;
            }

            transformToTrack = null;
        }

        private void LateUpdate()
        {
            if (OutOfTime)
            {
                Destroy();
            }

            if (hasEntity)
            {
                if (!TrackEntity())
                {
                    Destroy();
                }
            }
        }

        public void Play(Vector3 vfxPosition)
        {
            vfx.Play();
            destroyTime = Time.time + vfx.main.duration;

            transform.position = vfxPosition;
        }

        public void PlayOneShot(Vector3 vfxPosition)
        {
            Play(vfxPosition);

            destroyTime = Time.time + vfx.main.startLifetime.constantMax;
        }

        public void Play(float duration, Vector3 vfxPosition)
        {
            vfx.Play();
            destroyTime = Time.time + duration;
            transform.position = vfxPosition;
        }

        public void Play(Entity entityToTrack, float duration, Vector3 offset)
        {
            vfx.Play();
            destroyTime = Time.time + duration;
            relativeOffset = offset;
            this.EntityToTrack = entityToTrack;
            hasEntity = true;
            trackType = TrackType.Entity;
        }

        public void Play(Transform transformToTrack, float duration, Vector3 offset)
        {
            this.transformToTrack = transformToTrack;
            vfx.Play();
            destroyTime = Time.time + duration;
            relativeOffset = offset;
            transform.SetParent(transformToTrack.transform);
            transform.SetLocalPositionAndRotation(offset, Quaternion.identity);
            hasEntity = true;
            trackType = TrackType.Transform;
        }

        private bool TrackEntity()
        {
            if (trackType == TrackType.Entity)
            {
                if (entityManager.HasComponent(EntityToTrack, typeof(LocalToWorld)))
                {
                    var entityTransform = entityManager.GetComponentData<LocalToWorld>(EntityToTrack);
                    transform.position = (Vector3)(entityTransform.Position + entityTransform.Forward * relativeOffset);
                    return true;
                }

                return false;
            }
            else if (transformToTrack == null || !transformToTrack.gameObject.activeSelf)
            {
                return false;
            }

            return true;
        }

        private void Destroy()
        {
            gameObject.ReturnToPool();
        }
    }
}
