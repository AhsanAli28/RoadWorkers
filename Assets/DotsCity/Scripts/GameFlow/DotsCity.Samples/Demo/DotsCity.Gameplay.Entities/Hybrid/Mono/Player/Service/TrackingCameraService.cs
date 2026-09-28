using Spirit604.DotsCity.Gameplay.Factory.Player;
using Spirit604.DotsCity.Hybrid.Core;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player
{
    public class TrackingCameraService : MonoBehaviour
    {
        [SerializeField] private TrackingPointFactory trackingPointFactory;

        private Transform trackingPoint;
        private Entity targetEntity;

        private EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

        public Transform TrackingPoint
        {
            get
            {
                if (trackingPoint == null)
                {
                    Initialize();
                }

                return trackingPoint;
            }
        }

        public void Initialize()
        {
            if (trackingPoint == null)
            {
                trackingPoint = trackingPointFactory.Spawn(default, default);
            }
        }

        public void SetPoint(Vector3 pos)
        {
            TrackingPoint.transform.position = pos;
        }

        public void SetEntity(Entity entity)
        {
            if (targetEntity != Entity.Null && EntityManager.HasComponent<CopyTransformFromGameObject>(targetEntity))
            {
                EntityManager.RemoveComponent<Transform>(targetEntity);
                EntityManager.RemoveComponent<CopyTransformToGameObject>(targetEntity);
            }

            targetEntity = entity;

            EntityManager.AddComponentObject(targetEntity, TrackingPoint.transform);
            EntityManager.AddComponent<CopyTransformToGameObject>(targetEntity);
        }
    }
}
