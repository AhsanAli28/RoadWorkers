using Spirit604.DotsCity.Simulation.Binding;
using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Events;

namespace Spirit604.DotsCity.Events
{
    public class TriggerNodeHybridListener : MonoBehaviour, IEntityListener, ITriggerNodeListener
    {
        [SerializeField] private EntityWeakRef entityNodeRef;
        [SerializeField] private UnityEvent onTriggerEnter;

        private Entity entity;

        public event Action<Entity, Entity, TriggerNodeType> OnTriggerEnter = delegate { };

        private void OnEnable()
        {
            entityNodeRef.SubscribeBinding();
            entityNodeRef.Subscribe(this);
        }

        private void OnDisable()
        {
            entityNodeRef.UnsubscribeBinding();
            entityNodeRef.Unsubscribe(this);
            Unsubscribe();
            entity = default;
        }

        void IEntityListener.OnInitialized(EntityWeakRef entityWeakRef)
        {
            entity = entityWeakRef.Entity;
            TriggerNodeService.Instance.AddListener(entity, this);
        }

        void IEntityListener.OnUnload(EntityWeakRef entityWeakRef)
        {
            Unsubscribe();
        }

        void ITriggerNodeListener.RaiseEvent(Entity entity, Entity nodeEntity, TriggerNodeType triggerNodeType)
        {
            OnTriggerEnter(entity, nodeEntity, triggerNodeType);
            onTriggerEnter?.Invoke();
        }

        private void Unsubscribe()
        {
            if (entity == Entity.Null)
                return;

            if (TriggerNodeService.Instance)
                TriggerNodeService.Instance.RemoveListener(entity, this);

            entity = default;
        }
    }
}
