using Spirit604.DotsCity.Core;
using System;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Hybrid.Core
{
    public class NpcHybridEntityRef : MonoBehaviour, INpcEntity
    {
        private bool isInitialized;
        private Entity relatedEntity;
        private EntityManager entityManager;

        public bool HasEntity => isInitialized && entityManager.Exists(relatedEntity);

        public Entity RelatedEntity => relatedEntity;

        public event Action<Entity> OnEntityInitialized = delegate { };
        public event Action<INpcEntity> OnDisableCallback = delegate { };

        protected virtual void OnDisable()
        {
            OnDisableCallback(this);
            isInitialized = false;
            relatedEntity = default;
        }

        public virtual void Initialize(Entity entity, EntityManager entityManager)
        {
            this.isInitialized = true;
            this.relatedEntity = entity;
            this.entityManager = entityManager;

            OnEntityInitialized(entity);
        }

        public virtual void DestroyEntity()
        {
            if (isInitialized)
            {
                isInitialized = false;

                if (entityManager.Exists(RelatedEntity))
                {
                    PoolEntityUtils.DestroyEntity(ref entityManager, RelatedEntity);
                }
            }
        }
    }
}