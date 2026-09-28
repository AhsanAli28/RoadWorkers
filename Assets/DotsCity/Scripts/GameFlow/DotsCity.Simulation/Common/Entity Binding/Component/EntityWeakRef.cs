using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Binding
{
    /// <summary>
    /// Class to link Monobehaviour script & Unity.Entities.Entity.
    /// </summary>
    [Serializable]
    public class EntityWeakRef : IEquatable<EntityWeakRef>
    {
        /// <summary>
        /// Unique ID of the entity that stored in the EntityID component.
        /// </summary>
        public int Id;

        private List<IEntityListener> listeners = new List<IEntityListener>();

        public Entity Entity { get; private set; }

        public bool IsInitialized { get; private set; }

        public int EntityIndex => Entity.Index;

        private EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

        public override int GetHashCode() => this.Id;

        public bool Equals(EntityWeakRef other) => this.Id == other.Id && this.Entity == other.Entity;

        public void Subscribe(IEntityListener listener)
        {
            listeners.Add(listener);

            if (IsInitialized)
            {
                listener.OnInitialized(this);
            }
        }

        public void Unsubscribe(IEntityListener listener)
        {
            if (listeners.Contains(listener))
                listeners.Remove(listener);
        }

        public void SubscribeBinding(IEntityListener listener)
        {
            Subscribe(listener);
            SubscribeBinding();
        }

        public void UnsubscribeBinding(IEntityListener listener)
        {
            Unsubscribe(listener);
            UnsubscribeBinding();
        }

        public void SubscribeBinding()
        {
            EntityBindingService.Instance.Subscribe(this);
        }

        public void UnsubscribeBinding()
        {
            if (EntityBindingService.Instance)
                EntityBindingService.Instance.Unsubscribe(this);
        }

        public bool GetTransform(out RigidTransform transform)
        {
            return GetTransform(EntityManager, out transform);
        }

        public bool GetTransform(EntityManager entityManager, out RigidTransform transform)
        {
            if (entityManager.HasComponent<LocalTransform>(Entity))
            {
                var localTransform = entityManager.GetComponentData<LocalTransform>(Entity);
                transform = new RigidTransform(localTransform.Rotation, localTransform.Position);
                return true;
            }

            transform = default;
            return false;
        }

        public Vector3 GetPosition()
        {
            if (GetTransform(out var transform))
            {
                return transform.pos;
            }

            return default;
        }

        internal void SetEntity(Entity entity)
        {
            this.Entity = entity;
            this.IsInitialized = true;

            for (int i = 0; i < listeners.Count; i++)
            {
                listeners[i].OnInitialized(this);
            }
        }

        internal void UnloadEntity()
        {
            this.Entity = Entity.Null;
            this.IsInitialized = false;

            for (int i = 0; i < listeners.Count; i++)
            {
                listeners[i].OnUnload(this);
            }
        }
    }
}