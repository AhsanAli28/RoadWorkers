using Spirit604.Attributes;
using System;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Core
{
    public abstract class RuntimeEntityConfigBase : SyncConfigBase, IDisposable
    {
        public bool IsInitialized { get; private set; }
        protected Entity ConfigEntity { get; private set; }
        protected bool ConfigUpdated { get; set; }
        protected virtual bool UpdateAvailableByDefault => true;
        protected virtual bool HasCustomEntityArchetype => false;
        protected virtual bool Updatable => true;
        protected virtual bool IgnoreExist => false;
        protected bool UpdateAvailable => Application.isPlaying && (UpdateAvailableByDefault || ConfigUpdated);
        protected EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;
        private bool NotSubScene => gameObject.scene == null || !gameObject.scene.isSubScene;
        private bool ShowConfigButton => Updatable && NotSubScene;

        protected virtual void OnDestroy()
        {
            Dispose();
        }

        public void Create()
        {
            var entity = CreateEntity(EntityManager);
            Convert(entity, EntityManager);
            IsInitialized = true;
        }

        [ShowIf(nameof(ShowConfigButton))]
        [EnableIf(nameof(UpdateAvailable))]
        [Button]
        public void UpdateConfig()
        {
            if (!Application.isPlaying)
                return;

            UpdateConfigRoutine();
        }

        public virtual void Dispose() { }

        protected abstract void ConvertInternal(Entity entity, EntityManager dstManager);

        protected virtual EntityArchetype GetEntityArchetype() { return default; }

        protected virtual void OnConfigUpdatedInternal() { }

        protected void Convert(Entity entity, EntityManager dstManager)
        {
            ConfigUpdated = false;

            if (dstManager.Exists(ConfigEntity))
            {
                dstManager.DestroyEntity(ConfigEntity);
            }

            Dispose();

            SetEntity(entity);

            ConvertInternal(entity, dstManager);
        }

        protected Entity CreateEntity(EntityManager entityManager)
        {
            if (!HasCustomEntityArchetype)
            {
                return entityManager.CreateEntity();
            }
            else
            {
                return entityManager.CreateEntity(GetEntityArchetype());
            }
        }

        protected void OnInspectorValueUpdated()
        {
            ConfigUpdated = true;
            Sync();
        }

        protected void SetEntity(Entity entity)
        {
            ConfigEntity = entity;
        }

        private void UpdateConfigRoutine()
        {
            Create();
            OnConfigUpdatedInternal();
        }
    }
}
