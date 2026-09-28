using Spirit604.DotsCity.Core;
using System;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Hybrid.Core
{
    public class HybridEntityAdapter : MonoBehaviour
    {
        private const float UpdateFrequency = 0.2f;

        private float lastUpdateTime;

        public Entity RelatedEntity { get; private set; }

        public CullState CullState { get; set; } = CullState.Uninitialized;

        public event Action<Entity> OnEntityInitialized = delegate { };
        public event Action<CullState> OnCullStateChanged = delegate { };

        public bool HasEntity => RelatedEntity != Entity.Null && EntityManager.Exists(RelatedEntity);
        public bool HasCulling { get; set; } = true;

        protected EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;
        private float Time => UnityEngine.Time.time;

        protected virtual void OnDisable()
        {
            CullState = CullState.Uninitialized;
            DestroyEntity();
        }

        public virtual void Initialize(Entity entity)
        {
            RelatedEntity = entity;
            OnEntityInitialized(entity);

            if (HasCulling)
                CheckCullState();
        }

        public virtual bool CheckCullState(CullState newCullState)
        {
            if (CullState != newCullState)
            {
                if (newCullState != CullState.InViewOfCamera)
                {
                    if (lastUpdateTime + UpdateFrequency > Time)
                        return false;
                }

                lastUpdateTime = Time;
                CullState = newCullState;
                OnCullStateChanged(newCullState);
                return true;
            }

            return false;
        }

        public bool CheckCullState()
        {
            var cullStateComponent = EntityManager.GetComponentData<CullStateComponent>(RelatedEntity);

            return CheckCullState(cullStateComponent.State);
        }

        public virtual void DestroyEntity()
        {
            try
            {
                if (HasEntity && EntityManager.HasComponent<PooledEventTag>(RelatedEntity) && !EntityManager.IsComponentEnabled<PooledEventTag>(RelatedEntity))
                {
                    EntityManager.SetComponentEnabled<PooledEventTag>(RelatedEntity, true);
                }
            }
            catch { }

            RelatedEntity = default;
        }

        public virtual void DestroyEntityImmediate()
        {
            try
            {
                if (HasEntity && EntityManager.HasComponent<PooledEventTag>(RelatedEntity))
                {
                    EntityManager.DestroyEntity(RelatedEntity);
                }
            }
            catch { }

            RelatedEntity = default;
        }
    }
}
